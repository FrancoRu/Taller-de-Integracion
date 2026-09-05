using Application.DTOs.Backup.Response;
using Application.Interfaces.Backup;
using Application.Interfaces.Services;
using Application.Utils.Constants;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Backup;

/// <summary>
/// The one shared write path for backup operations.
/// </summary>
public sealed class BackupOperationsService(
    IBackupCatalog catalog,
    IBackupStorage storage,
    IDatabaseBackupService backupService,
    IDatabaseRestoreService restoreService,
    IBackupRetentionPolicy retentionPolicy,
    BackupOptions options,
    BackupOperationLock operationLock,
    IMaintenanceModeState maintenanceModeState,
    IAuditService auditService,
    ILogger<BackupOperationsService> logger) : IBackupOperationsService
{
    public async Task<BackupOperationResult> CreateBackupAsync(BackupOrigin origin, CancellationToken ct = default)
    {
        if (!await operationLock.WaitAsync(TimeSpan.Zero, ct))
        {
            return new BackupOperationResult(BackupOperationOutcome.Busy, null, ErrorMessages.Backup.OperationInProgress);
        }

        try
        {
            return await CreateBackupCoreAsync(origin, applyRetention: true, ct);
        }
        finally
        {
            operationLock.Release();
        }
    }

    public async Task<BackupOperationResult> DeleteBackupAsync(Guid id, CancellationToken ct = default)
    {
        if (!await operationLock.WaitAsync(TimeSpan.Zero, ct))
        {
            return new BackupOperationResult(BackupOperationOutcome.Busy, null, ErrorMessages.Backup.OperationInProgress);
        }

        try
        {
            BackupRecord? record = await catalog.GetByIdAsync(id, ct);
            if (record is null)
            {
                return new BackupOperationResult(BackupOperationOutcome.NotFound, null, null);
            }

            try
            {
                await storage.DeleteAsync(record.StoragePath, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // An already-missing file deleted out-of-band must not block removing the catalog row, so log and continue.
                logger.LogWarning(
                    ex,
                    "Backup file could not be deleted from storage; removing catalog record anyway. StoragePath: {StoragePath}",
                    record.StoragePath);
            }

            await catalog.RemoveAsync(record.Id, ct);

            return new BackupOperationResult(BackupOperationOutcome.Completed, BackupRecordResponse.FromEntity(record), null);
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <summary>
    /// Enters maintenance mode, takes an automatic safety backup, restores the selected one, then exits.
    /// </summary>
    public async Task<BackupOperationResult> RestoreBackupAsync(Guid id, CancellationToken ct = default)
    {
        if (!await operationLock.WaitAsync(TimeSpan.Zero, ct))
        {
            return new BackupOperationResult(BackupOperationOutcome.Busy, null, ErrorMessages.Backup.OperationInProgress);
        }

        try
        {
            BackupRecord? record = await catalog.GetByIdAsync(id, ct);
            if (record is null)
            {
                return new BackupOperationResult(BackupOperationOutcome.NotFound, null, null);
            }

            maintenanceModeState.Enter($"Restoring backup {record.Id}.");
            string? tempFilePath = null;
            try
            {
                BackupOperationResult safetyBackup = await CreateBackupCoreAsync(BackupOrigin.Job, applyRetention: false, ct);
                if (safetyBackup.Outcome != BackupOperationOutcome.Completed)
                {
                    return new BackupOperationResult(BackupOperationOutcome.Failed, null, safetyBackup.Message);
                }

                tempFilePath = Path.Combine(Path.GetTempPath(), $"restore-{Guid.NewGuid():N}.sql");
                await using (Stream source = await storage.OpenReadAsync(record.StoragePath, ct))
                await using (FileStream destination = File.Create(tempFilePath))
                {
                    await source.CopyToAsync(destination, ct);
                }

                await restoreService.RestoreAsync(tempFilePath, ct);

                // Logging the restore is non-throwing by contract via IAuditService.LogAsync, so a logging hiccup never turns a successful restore into a reported failure.
                await auditService.LogAsync(
                    AuditAction.BackupRestore,
                    targetType: nameof(BackupRecord),
                    targetId: record.Id.ToString(),
                    targetName: record.StoragePath,
                    detail: $"Origen: {record.Origin}",
                    ct: ct);

                return new BackupOperationResult(BackupOperationOutcome.Completed, safetyBackup.Record, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Restore failed; maintenance mode will be cleared and the host keeps running.");
                return new BackupOperationResult(BackupOperationOutcome.Failed, null, ex.Message);
            }
            finally
            {
                if (tempFilePath is not null && File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

                maintenanceModeState.Exit();
            }
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <summary>
    /// Assumes the lock is already held by the caller, so a restore's safety backup avoids self-deadlock.
    /// </summary>
    private async Task<BackupOperationResult> CreateBackupCoreAsync(BackupOrigin origin, bool applyRetention, CancellationToken ct)
    {
        Stream dump;
        try
        {
            dump = await backupService.CreateDumpAsync(ct);
        }
        catch (BackupExecutionException ex)
        {
            logger.LogError(ex, "Backup dump failed; no catalog record written.");
            return new BackupOperationResult(BackupOperationOutcome.Failed, null, ex.Message);
        }

        await using (dump)
        {
            string name = $"backup-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.sql";
            long sizeBytes = dump.Length;

            try
            {
                await storage.StoreAsync(name, dump, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to store backup dump; no catalog record written.");
                return new BackupOperationResult(BackupOperationOutcome.Failed, null, ex.Message);
            }

            BackupRecord record = new()
            {
                CreatedBy = AuditConstants.SystemUser,
                StoragePath = name,
                SizeBytes = sizeBytes,
                Origin = origin,
            };
            BackupRecord added = await catalog.AddAsync(record, ct);

            if (applyRetention)
            {
                await ApplyRetentionAsync(ct);
            }

            return new BackupOperationResult(BackupOperationOutcome.Completed, BackupRecordResponse.FromEntity(added), null);
        }
    }

    private async Task ApplyRetentionAsync(CancellationToken ct)
    {
        IReadOnlyList<BackupRecord> all = await catalog.ListNewestFirstAsync(ct);
        IReadOnlyList<BackupFile> files = all
            .Select(r => new BackupFile(r.StoragePath, new DateTimeOffset(DateTime.SpecifyKind(r.DateCreated, DateTimeKind.Utc))))
            .ToList();

        IReadOnlyList<BackupFile> toDelete = retentionPolicy.SelectForDeletion(files, options.RetentionCount);
        if (toDelete.Count == 0)
        {
            return;
        }

        Dictionary<string, BackupRecord> byStoragePath = all.ToDictionary(r => r.StoragePath, StringComparer.Ordinal);

#pragma warning disable S3267
        foreach (BackupFile stale in toDelete)
        {
            if (!byStoragePath.TryGetValue(stale.Name, out BackupRecord? record))
            {
                continue;
            }

            try
            {
                await storage.DeleteAsync(record.StoragePath, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to delete stale backup file {StoragePath} during retention pruning.", record.StoragePath);
            }

            await catalog.RemoveAsync(record.Id, ct);
        }
#pragma warning restore S3267
    }
}
