using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// Restore orchestration (HU-93): restores the database from a chosen stored
/// backup, always creating a safety backup first so data can be recovered if
/// the restore fails. Runs under the app-wide maintenance lock (HU-92).
/// </summary>
public interface IBackupRestoreService
{
    /// <summary>
    /// Restores the database from the stored backup named
    /// <paramref name="backupName"/>. The sequence is:
    /// <list type="number">
    /// <item>create and store a safety backup of the current state;</item>
    /// <item>retrieve the chosen backup and replay it;</item>
    /// <item>on success, delete the safety backup and the transient restore
    /// copy;</item>
    /// <item>on failure, keep the safety backup and surface a
    /// BackupExecutionException.</item>
    /// </list>
    /// Throws MaintenanceInProgressException if a backup/restore is already
    /// running.
    /// </summary>
    Task<RestoreResult> RestoreAsync(string backupName, CancellationToken ct = default);
}
