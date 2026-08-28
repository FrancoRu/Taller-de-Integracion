using Application.Interfaces.Backup;

using Domain.Entities.Models;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// In-memory test double for IBackupCatalog. Lets
/// BackupOperationsServiceTests and BackupControllerTests exercise
/// the shared create/delete/retention logic without a real database.
/// </summary>
public sealed class FakeBackupCatalog : IBackupCatalog
{
    private readonly List<BackupRecord> _records = [];

    public int AddCallCount { get; private set; }
    public int RemoveCallCount { get; private set; }

    public Task<BackupRecord> AddAsync(BackupRecord record, CancellationToken ct = default)
    {
        AddCallCount++;
        if (record.Id == Guid.Empty)
        {
            record.Id = Guid.NewGuid();
        }

        _records.Add(record);
        return Task.FromResult(record);
    }

    public Task<BackupRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(_records.Find(r => r.Id == id));
    }

    public Task<IReadOnlyList<BackupRecord>> ListNewestFirstAsync(CancellationToken ct = default)
    {
        IReadOnlyList<BackupRecord> ordered = _records
            .OrderByDescending(r => r.DateCreated)
            .ToList();
        return Task.FromResult(ordered);
    }

    public Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        RemoveCallCount++;
        _records.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }
}
