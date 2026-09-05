using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// The single shared write path for backup operations, serialized by BackupOperationLock so a concurrent attempt returns Busy instead of running alongside another.
/// </summary>
public interface IBackupOperationsService
{
    Task<IReadOnlyList<BackupRecord>> ListNewestFirstAsync(CancellationToken ct = default);

    Task<BackupOperationResult> CreateBackupAsync(BackupOrigin origin, CancellationToken ct = default);

    Task<BackupOperationResult> DeleteBackupAsync(Guid id, CancellationToken ct = default);

    Task<BackupOperationResult> RestoreBackupAsync(Guid id, CancellationToken ct = default);
}
