using Application.Interfaces.Backup;

using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistance;

/// <summary>
/// IBackupCatalog implementation backed directly by
/// ApplicationDBContext.BackupRecords. Kept as a thin, single-entity
/// wrapper (not routed through GenericRepository/UnitOfWork) to match the
/// lightweight port style already established for the rest of the Backup
/// feature (IBackupStorage, IDatabaseBackupService).
/// </summary>
public sealed class EfBackupCatalog(ApplicationDBContext context) : IBackupCatalog
{
    public async Task<BackupRecord> AddAsync(BackupRecord record, CancellationToken ct = default)
    {
        context.BackupRecords.Add(record);
        await context.SaveChangesAsync(ct);
        return record;
    }

    public async Task<BackupRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.BackupRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<BackupRecord>> ListNewestFirstAsync(CancellationToken ct = default)
    {
        return await context.BackupRecords
            .OrderByDescending(r => r.DateCreated)
            .ToListAsync(ct);
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        BackupRecord? record = await context.BackupRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null)
        {
            return;
        }

        context.BackupRecords.Remove(record);
        await context.SaveChangesAsync(ct);
    }
}
