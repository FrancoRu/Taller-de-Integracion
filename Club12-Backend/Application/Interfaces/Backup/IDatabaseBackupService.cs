using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// Produces a single database dump. Implementations must never let an
/// underlying failure crash the caller: a failed dump attempt (non-zero exit
/// code, missing binary, ...) is logged and surfaced as a
/// BackupExecutionException, not an unhandled exception.
/// </summary>
public interface IDatabaseBackupService
{
    Task<Stream> CreateDumpAsync(CancellationToken ct = default);
}
