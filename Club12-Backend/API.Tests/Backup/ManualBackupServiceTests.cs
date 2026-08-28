using API.Tests.Backup.Fakes;

using Application.Backup;
using Application.Interfaces.Backup;
using Application.Interfaces.Maintenance;
using Application.Maintenance;

using Infrastructure.Backup;

using Microsoft.Extensions.Logging.Abstractions;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for the HU-91 manual backup orchestration: it dumps + stores +
/// prunes using the same ports as the scheduled job, returns the created
/// backup's metadata, runs under the maintenance lock and releases it, lists
/// newest-first, and is rejected when a backup/restore is already running.
/// No real pg_dump or storage involved.
/// </summary>
public class ManualBackupServiceTests
{
    private static ManualBackupService Build(
        FakeDatabaseBackupService backup,
        FakeBackupStorage storage,
        IMaintenanceState state,
        int retentionCount = 100)
    {
        BackupOptions options = new() { RetentionCount = retentionCount };
        return new ManualBackupService(
            backup, storage, new KeepLastNRetentionPolicy(), options, state,
            NullLogger<ManualBackupService>.Instance);
    }

    [Fact]
    public async Task CreateBackupAsync_DumpsStoresAndReturnsMetadata_ReleasesLock()
    {
        FakeDatabaseBackupService backup = new();
        FakeBackupStorage storage = new();
        MaintenanceState state = new();
        ManualBackupService service = Build(backup, storage, state);

        BackupFile result = await service.CreateBackupAsync();

        Assert.Equal(1, backup.CallCount);
        Assert.Equal(1, storage.StoreCallCount);
        Assert.Equal(result.Name, Assert.Single(storage.StoredNames));
        Assert.False(state.IsActive, "The lock must be released after the backup completes.");
    }

    [Fact]
    public async Task CreateBackupAsync_PrunesStaleBackupsPerRetentionCount()
    {
        FakeDatabaseBackupService backup = new();
        FakeBackupStorage storage = new()
        {
            FilesToList =
            [
                new BackupFile("backup-1.sql", DateTimeOffset.UtcNow.AddHours(-3)),
                new BackupFile("backup-2.sql", DateTimeOffset.UtcNow.AddHours(-2)),
                new BackupFile("backup-3.sql", DateTimeOffset.UtcNow.AddHours(-1)),
            ],
        };
        MaintenanceState state = new();
        ManualBackupService service = Build(backup, storage, state, retentionCount: 1);

        await service.CreateBackupAsync();

        // Retain 1 of the 3 existing => delete the 2 oldest.
        Assert.Equal(2, storage.DeleteCallCount);
        Assert.Contains("backup-1.sql", storage.DeletedNames);
        Assert.Contains("backup-2.sql", storage.DeletedNames);
    }

    [Fact]
    public async Task CreateBackupAsync_WhenAlreadyLocked_ThrowsAndDoesNotDump()
    {
        FakeDatabaseBackupService backup = new();
        FakeBackupStorage storage = new();
        MaintenanceState state = new();
        using IDisposable held = state.Enter("restore");
        ManualBackupService service = Build(backup, storage, state);

        await Assert.ThrowsAsync<MaintenanceInProgressException>(() => service.CreateBackupAsync());

        Assert.Equal(0, backup.CallCount);
        Assert.Equal(0, storage.StoreCallCount);
    }

    [Fact]
    public async Task ListBackupsAsync_ReturnsNewestFirst()
    {
        FakeDatabaseBackupService backup = new();
        FakeBackupStorage storage = new()
        {
            FilesToList =
            [
                new BackupFile("older.sql", DateTimeOffset.UtcNow.AddHours(-5)),
                new BackupFile("newest.sql", DateTimeOffset.UtcNow),
                new BackupFile("middle.sql", DateTimeOffset.UtcNow.AddHours(-2)),
            ],
        };
        ManualBackupService service = Build(backup, storage, new MaintenanceState());

        IReadOnlyList<BackupFile> result = await service.ListBackupsAsync();

        Assert.Equal(["newest.sql", "middle.sql", "older.sql"], result.Select(f => f.Name));
    }
}
