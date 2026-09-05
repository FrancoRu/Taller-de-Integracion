using System.Collections.Generic;

namespace Application.Interfaces.Backup;

/// <summary>
/// Pure keep-last-N pruning decision over an existing list of backup files; implementations must not perform I/O.
/// </summary>
public interface IBackupRetentionPolicy
{
    IReadOnlyList<BackupFile> SelectForDeletion(IReadOnlyList<BackupFile> existing, int retainCount);
}
