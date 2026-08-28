using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// Low-level restore boundary: replays a single plain-SQL dump
/// (as produced by <see cref="IDatabaseBackupService.CreateDumpAsync"/>) back
/// into the configured database. The pg-specific adapter shells out to
/// <c>psql</c> via <see cref="IProcessRunner"/>, mirroring how
/// PgDumpBackupService shells out to <c>pg_dump</c>. Implementations must
/// surface a failed restore (non-zero exit, missing binary, ...) as a
/// <see cref="BackupExecutionException"/> rather than an unhandled exception,
/// so the restore orchestrator can keep the safety backup and report cleanly.
/// </summary>
public interface IDatabaseRestoreService
{
    /// <summary>
    /// Applies the SQL in <paramref name="dumpContent"/> to the database.
    /// The caller owns the stream's lifetime.
    /// </summary>
    Task RestoreAsync(Stream dumpContent, CancellationToken ct = default);
}
