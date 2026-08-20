using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// Restores the database from a local plain-SQL dump file (design.md's "Keep
/// plain-SQL dumps; restore with psql, not pg_restore" decision — the dump
/// produced by IDatabaseBackupService is plain SQL text, not a
/// binary custom/tar archive). Adapters are implemented in a later work unit
/// alongside the restore endpoint; this port is defined here so
/// BackupOperationsService.RestoreBackupAsync can be designed
/// against its real shape.
/// </summary>
public interface IDatabaseRestoreService
{
    /// <summary>
    /// Restores from the plain-SQL dump at <paramref name="dumpFilePath"/>.
    /// Throws BackupExecutionException on failure (non-zero exit,
    /// missing binary) — callers are expected to catch it, log it, and keep
    /// the host running (see the "Restore Failure Is Logged and Isolated"
    /// spec requirement).
    /// </summary>
    Task RestoreAsync(string dumpFilePath, CancellationToken ct = default);
}
