using Application.DTOs.Backup.Response;
using Application.Interfaces.Backup;
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
/// The one shared write path for backup operations (spec
/// backup-catalog#Single-Shared-Write-Path). Both the manual endpoint
/// (BackupController) and the scheduled job
/// (DatabaseBackupHostedService, via a DI scope) call this scoped
/// service, which serializes every call through the singleton
/// BackupOperationLock. A dump/store failure is logged and returns
/// Failed without ever writing a catalog record (spec
/// backup-catalog#Failed-Backups-Are-Not-Catalogued). Retention pruning
/// reads the catalog (not IBackupStorage.ListAsync()), so it
/// naturally spans the combined Manual+Job pool with no per-origin cap
/// (scheduled-database-backups#Keep-Last-N-Retention-Pruning).
/// </summary>
public sealed class BackupOperationsService(
    IBackupCatalog catalog,
    IBackupStorage storage,
    IDatabaseBackupService backupService,
    IBackupRetentionPolicy retentionPolicy,
    BackupOptions options,
    BackupOperationLock operationLock,
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
                // spec backup-catalog#Delete-Removes-Both-Stored-File-and-Catalog-Record:
                // an already-missing file (deleted out-of-band) must not block removing the
                // catalog row — log and continue.
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
    /// Assumes the lock is already held by the caller — this lets a future
    /// restore take its pre-restore safety backup
    /// (applyRetention: false) without self-deadlocking on the
    /// same lock.
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
