using API.Tests.Backup.Fakes;

using Application.Interfaces.Backup;
using Application.Interfaces.Maintenance;
using Application.Maintenance;

using Infrastructure.Backup;

using Microsoft.Extensions.Logging.Abstractions;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for the HU-93 restore orchestration with the pg process and
/// storage abstracted behind fakes: safety-backup-first, feed the chosen
/// backup to the restore, delete the safety backup on success, and keep it on
/// failure. No real Postgres / psql involved.
/// </summary>
public class BackupRestoreServiceTests
{
    private static BackupRestoreService Build(
        FakeDatabaseBackupService backup,
        FakeDatabaseRestoreService restore,
        FakeBackupStorage storage,
        IMaintenanceState state)
    {
        return new BackupRestoreService(
            backup, restore, storage, state, NullLogger<BackupRestoreService>.Instance);
    }

    [Fact]
    public async Task RestoreAsync_Success_SafetyFirst_ThenRestores_ThenDeletesSafety()
    {
        FakeDatabaseBackupService backup = new();
        FakeBackupStorage storage = new();
        MaintenanceState state = new();

        int storedWhenRestoreRan = -1;
        int deletedWhenRestoreRan = -1;
        FakeDatabaseRestoreService restore = new()
        {
            OnRestore = () =>
            {
                storedWhenRestoreRan = storage.StoredNames.Count;
                deletedWhenRestoreRan = storage.DeletedNames.Count;
                return Task.CompletedTask;
            },
        };

        BackupRestoreService service = Build(backup, restore, storage, state);

        RestoreResult result = await service.RestoreAsync("chosen.sql");

        // Safety backup was created and stored BEFORE the restore ran.
        Assert.Equal(1, storedWhenRestoreRan);
        Assert.Equal(0, deletedWhenRestoreRan);
        Assert.Equal(1, backup.CallCount);

        // The chosen backup was retrieved and replayed.
        Assert.Equal("chosen.sql", Assert.Single(storage.RetrievedNames));
        Assert.Equal(1, restore.CallCount);
        Assert.Equal("chosen.sql", result.RestoredFrom);

        // On success the safety backup is deleted.
        Assert.StartsWith("safety-", result.SafetyBackupName);
        Assert.Equal(result.SafetyBackupName, Assert.Single(storage.StoredNames));
        Assert.Contains(result.SafetyBackupName, storage.DeletedNames);
        Assert.False(state.IsActive, "The lock must be released after a successful restore.");
    }

    [Fact]
    public async Task RestoreAsync_FeedsRetrievedBackupContentToRestore()
    {
        FakeDatabaseBackupService backup = new();
        FakeBackupStorage storage = new() { RetrieveContent = "SELECT 1; -- chosen dump" };
        FakeDatabaseRestoreService restore = new();
        BackupRestoreService service = Build(backup, restore, storage, new MaintenanceState());

        await service.RestoreAsync("chosen.sql");

        Assert.Equal("SELECT 1; -- chosen dump", restore.CapturedContent);
    }

    [Fact]
    public async Task RestoreAsync_RestoreFails_KeepsSafetyBackup_AndReleasesLock()
    {
        FakeDatabaseBackupService backup = new();
        FakeBackupStorage storage = new();
        FakeDatabaseRestoreService restore = new() { ShouldThrow = true };
        MaintenanceState state = new();
        BackupRestoreService service = Build(backup, restore, storage, state);

        await Assert.ThrowsAsync<BackupExecutionException>(() => service.RestoreAsync("chosen.sql"));

        // Safety backup was created and is KEPT (never deleted) so data can be recovered.
        string safety = Assert.Single(storage.StoredNames);
        Assert.StartsWith("safety-", safety);
        Assert.DoesNotContain(safety, storage.DeletedNames);
        Assert.Empty(storage.DeletedNames);
        Assert.False(state.IsActive, "The lock must be released even when the restore fails.");
    }

    [Fact]
    public async Task RestoreAsync_WhenAlreadyLocked_Throws_NoSafetyBackupCreated()
    {
        FakeDatabaseBackupService backup = new();
        FakeBackupStorage storage = new();
        FakeDatabaseRestoreService restore = new();
        MaintenanceState state = new();
        using IDisposable held = state.Enter("backup");
        BackupRestoreService service = Build(backup, restore, storage, state);

        await Assert.ThrowsAsync<MaintenanceInProgressException>(() => service.RestoreAsync("chosen.sql"));

        Assert.Equal(0, backup.CallCount);
        Assert.Equal(0, storage.StoreCallCount);
        Assert.Equal(0, restore.CallCount);
    }
}
