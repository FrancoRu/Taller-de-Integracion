using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// Durable, queryable per-backup record store that is the source of truth for the admin backup listing.
/// </summary>
public interface IBackupCatalog
{
    Task<BackupRecord> AddAsync(BackupRecord record, CancellationToken ct = default);

    Task<BackupRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<BackupRecord>> ListNewestFirstAsync(CancellationToken ct = default);

    Task RemoveAsync(Guid id, CancellationToken ct = default);
}
