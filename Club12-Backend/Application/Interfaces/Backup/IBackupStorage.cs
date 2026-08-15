using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// Persists, lists, and deletes backup dumps in durable off-host storage
/// (e.g. Supabase Storage or a local/mounted directory). Adapters are
/// implemented and wired in a later work unit; this port is defined here so
/// the pure retention policy can be designed against its real shape.
/// </summary>
public interface IBackupStorage
{
    Task StoreAsync(string name, Stream content, CancellationToken ct = default);

    Task<IReadOnlyList<BackupFile>> ListAsync(CancellationToken ct = default);

    Task DeleteAsync(string name, CancellationToken ct = default);
}
