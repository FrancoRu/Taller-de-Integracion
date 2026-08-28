using Application.Backup;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for BackupOperationLock: the process-wide single-flight
/// guard shared by manual (BackupController) and scheduled
/// (DatabaseBackupHostedService) backup/restore attempts. Covers
/// backup-catalog#Single-Shared-Write-Path.
/// </summary>
public class BackupOperationLockTests
{
    [Fact]
    public async Task WaitAsync_SecondCallWhileHeld_ReturnsFalse()
    {
        BackupOperationLock sut = new();

        bool first = await sut.WaitAsync(TimeSpan.Zero);
        bool second = await sut.WaitAsync(TimeSpan.Zero);

        Assert.True(first, "First acquisition of an unheld lock must succeed.");
        Assert.False(second, "A second acquisition attempt while the lock is held must fail immediately.");

        sut.Release();
    }

    [Fact]
    public async Task WaitAsync_AfterRelease_AllowsAcquisitionAgain()
    {
        BackupOperationLock sut = new();
        bool first = await sut.WaitAsync(TimeSpan.Zero);
        sut.Release();

        bool acquiredAgain = await sut.WaitAsync(TimeSpan.Zero);

        Assert.True(first);
        Assert.True(acquiredAgain, "Once released, the lock must be acquirable again.");

        sut.Release();
    }
}
