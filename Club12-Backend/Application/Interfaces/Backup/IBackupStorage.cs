using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// Persists, lists, and deletes backup dumps in durable off-host storage, whether cloud object storage or a local or mounted directory.
/// </summary>
public interface IBackupStorage
{
    Task StoreAsync(string name, Stream content, CancellationToken ct = default);

    Task<IReadOnlyList<BackupFile>> ListAsync(CancellationToken ct = default);

    Task DeleteAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Opens a readable stream over the stored backup named name, for the restore flow to copy into a local temp file before invoking IDatabaseRestoreService.
    /// </summary>
    Task<Stream> OpenReadAsync(string name, CancellationToken ct = default);
}
