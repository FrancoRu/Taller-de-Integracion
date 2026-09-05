using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// Produces a single database dump without ever letting an underlying failure crash the caller.
/// </summary>
public interface IDatabaseBackupService
{
    Task<Stream> CreateDumpAsync(CancellationToken ct = default);
}
