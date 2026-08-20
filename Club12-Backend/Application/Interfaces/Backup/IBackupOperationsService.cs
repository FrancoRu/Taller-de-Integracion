using Domain.Enums;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// The single shared write path for backup operations, used by both the
/// manual Admin endpoint (BackupController) and the scheduled job
/// (DatabaseBackupHostedService, via a DI scope). Every call is
/// serialized by BackupOperationLock, so a concurrent attempt from
/// either caller returns Busy instead of running alongside another
/// attempt. RestoreBackupAsync is added in a later work unit (PR4) —
/// this PR only exposes create and delete.
/// </summary>
public interface IBackupOperationsService
{
    Task<BackupOperationResult> CreateBackupAsync(BackupOrigin origin, CancellationToken ct = default);

    Task<BackupOperationResult> DeleteBackupAsync(Guid id, CancellationToken ct = default);
}
