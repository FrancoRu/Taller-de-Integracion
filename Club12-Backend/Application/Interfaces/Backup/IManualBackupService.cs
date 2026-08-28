using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// On-demand (HU-91) backup operations for the admin panel: trigger a backup
/// now and list the backups currently available in storage. Reuses the same
/// ports the scheduled job uses (IDatabaseBackupService dump +
/// IBackupStorage + IBackupRetentionPolicy pruning), and runs the create
/// under the app-wide maintenance lock (HU-92) so no data is mutated
/// mid-dump.
/// </summary>
public interface IManualBackupService
{
    /// <summary>
    /// Creates a backup now: dumps the database, stores it, prunes stale
    /// backups per the configured retention count, and returns the created
    /// backup's metadata. Throws MaintenanceInProgressException if a
    /// backup/restore is already running, and BackupExecutionException if the
    /// dump/store fails.
    /// </summary>
    Task<BackupFile> CreateBackupAsync(CancellationToken ct = default);

    /// <summary>
    /// Lists the backups currently available in storage, newest first.
    /// </summary>
    Task<IReadOnlyList<BackupFile>> ListBackupsAsync(CancellationToken ct = default);
}
