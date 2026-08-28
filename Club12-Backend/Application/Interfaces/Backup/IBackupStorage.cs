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

    /// <summary>
    /// Opens a readable stream over the stored backup named
    /// <paramref name="name"/>, for the restore flow to copy into a local
    /// temp file before invoking IDatabaseRestoreService. Adapters
    /// MUST re-validate <paramref name="name"/> with the exact same guard
    /// StoreAsync/DeleteAsync use before any read/download is
    /// attempted, since the caller-supplied name may originate from a
    /// catalog row rather than a trusted server-generated value.
    /// </summary>
    Task<Stream> OpenReadAsync(string name, CancellationToken ct = default);
}
