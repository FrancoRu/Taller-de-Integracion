using System.Collections.Generic;

namespace Application.Interfaces.Backup;

/// <summary>
/// Pure keep-last-N pruning decision over an existing list of backup files.
/// Implementations must not perform I/O — selection only; deletion is the
/// storage adapter's responsibility.
/// </summary>
public interface IBackupRetentionPolicy
{
    IReadOnlyList<BackupFile> SelectForDeletion(IReadOnlyList<BackupFile> existing, int retainCount);
}
