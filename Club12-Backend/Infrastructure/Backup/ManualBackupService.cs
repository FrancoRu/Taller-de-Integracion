using Application.Interfaces.Backup;
using Application.Interfaces.Maintenance;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Backup;

/// <summary>
/// On-demand backup orchestration (HU-91). Reuses the exact same ports the
/// scheduled DatabaseBackupHostedService uses — IDatabaseBackupService to
/// dump, IBackupStorage to persist, IBackupRetentionPolicy to prune — but is
/// driven by an admin/owner request instead of a timer, and runs under the
/// app-wide maintenance lock (HU-92) so no data is mutated mid-dump.
/// </summary>
public sealed class ManualBackupService(
    IDatabaseBackupService backupService,
    IBackupStorage backupStorage,
    IBackupRetentionPolicy retentionPolicy,
    BackupOptions options,
    IMaintenanceState maintenanceState,
    ILogger<ManualBackupService> logger) : IManualBackupService
{
    public async Task<BackupFile> CreateBackupAsync(CancellationToken ct = default)
    {
        using IDisposable lease = maintenanceState.Enter("backup");

        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        string name = $"backup-{createdAt:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.sql";

        await using (Stream dump = await backupService.CreateDumpAsync(ct))
        {
            await backupStorage.StoreAsync(name, dump, ct);
        }

        await PruneAsync(ct);

        logger.LogInformation("Manual backup completed: stored {Name}.", name);
        return new BackupFile(name, createdAt);
    }

    public async Task<IReadOnlyList<BackupFile>> ListBackupsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<BackupFile> files = await backupStorage.ListAsync(ct);
        return files.OrderByDescending(f => f.Timestamp).ToList();
    }

    private async Task PruneAsync(CancellationToken ct)
    {
        IReadOnlyList<BackupFile> existing = await backupStorage.ListAsync(ct);
        IReadOnlyList<BackupFile> toDelete = retentionPolicy.SelectForDeletion(existing, options.RetentionCount);

#pragma warning disable S3267
        foreach (BackupFile stale in toDelete)
        {
            try
            {
                await backupStorage.DeleteAsync(stale.Name, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete stale backup {Name} during retention pruning.", stale.Name);
            }
        }
#pragma warning restore S3267
    }
}
