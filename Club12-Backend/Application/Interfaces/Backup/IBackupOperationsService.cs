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
/// attempt. RestoreBackupAsync additionally enters maintenance mode,
/// takes an automatic pre-restore safety backup (Origin = Job,
/// applyRetention: false — spec database-restore#Automatic-Pre-Restore-Safety-Backup),
/// runs the restore, and always exits maintenance mode in a
/// finally block, whether the restore succeeded or failed (spec
/// database-restore#Restore-Failure-Is-Logged-and-Isolated).
/// </summary>
public interface IBackupOperationsService
{
    Task<BackupOperationResult> CreateBackupAsync(BackupOrigin origin, CancellationToken ct = default);

    Task<BackupOperationResult> DeleteBackupAsync(Guid id, CancellationToken ct = default);

    Task<BackupOperationResult> RestoreBackupAsync(Guid id, CancellationToken ct = default);
}
