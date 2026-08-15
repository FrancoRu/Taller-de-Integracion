using System;
using System.Threading;
using System.Threading.Tasks;
using API.BackgroundServices;
using API.Tests.Backup.Fakes;
using Application.Backup;
using Application.Interfaces.Backup;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for <see cref="DatabaseBackupHostedService"/>: interval-trigger
/// logic, the <c>Backup:Enabled</c> gate, the single-flight guard, and
/// failure isolation. All tests use <see cref="DatabaseBackupHostedService.IntervalOverride"/>
/// (a short, deterministic interval) instead of sleeping for real
/// <c>IntervalHours</c>-scale durations, and poll via <see cref="TestTiming"/>
/// rather than fixed sleeps to avoid flakiness.
/// </summary>
public class DatabaseBackupHostedServiceTests
{
    private static BackupOptions EnabledOptions() => new()
    {
        Enabled = true,
        IntervalHours = 24,
        RetentionCount = 7,
    };

    [Fact]
    public async Task ExecuteAsync_IntervalElapses_TriggersOneBackupAttempt()
    {
        FakeDatabaseBackupService backupService = new();
        FakeBackupStorage storage = new();
        DatabaseBackupHostedService service = new(
            EnabledOptions(), backupService, storage, new KeepLastNRetentionPolicy(),
            NullLogger<DatabaseBackupHostedService>.Instance)
        {
            IntervalOverride = TimeSpan.FromMilliseconds(30),
        };

        await service.StartAsync(CancellationToken.None);
        bool triggered = await TestTiming.WaitUntilAsync(() => backupService.CallCount >= 1, TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        Assert.True(triggered, "Expected at least one backup attempt after the interval elapsed.");
        Assert.True(storage.StoreCallCount >= 1);
    }

    [Fact]
    public async Task ExecuteAsync_IntervalNotYetElapsed_NoBackupAttemptTriggered()
    {
        FakeDatabaseBackupService backupService = new();
        FakeBackupStorage storage = new();
        DatabaseBackupHostedService service = new(
            EnabledOptions(), backupService, storage, new KeepLastNRetentionPolicy(),
            NullLogger<DatabaseBackupHostedService>.Instance)
        {
            IntervalOverride = TimeSpan.FromSeconds(5),
        };

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100); // well short of the 5s interval
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, backupService.CallCount);
        Assert.Equal(0, storage.StoreCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_Disabled_NeverCallsBackupServiceOrStorage()
    {
        BackupOptions options = new() { Enabled = false, IntervalHours = 24, RetentionCount = 7 };
        FakeDatabaseBackupService backupService = new();
        FakeBackupStorage storage = new();
        DatabaseBackupHostedService service = new(
            options, backupService, storage, new KeepLastNRetentionPolicy(),
            NullLogger<DatabaseBackupHostedService>.Instance)
        {
            IntervalOverride = TimeSpan.FromMilliseconds(20),
        };

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150); // several would-be intervals, to prove no scheduling ever starts
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, backupService.CallCount);
        Assert.Equal(0, storage.StoreCallCount);
        Assert.Equal(0, storage.ListCallCount);
        Assert.Equal(0, storage.DeleteCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_SingleFlight_OverlappingTickDoesNotStartConcurrentAttempt()
    {
        FakeDatabaseBackupService backupService = new()
        {
            Gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
        FakeBackupStorage storage = new();
        DatabaseBackupHostedService service = new(
            EnabledOptions(), backupService, storage, new KeepLastNRetentionPolicy(),
            NullLogger<DatabaseBackupHostedService>.Instance)
        {
            IntervalOverride = TimeSpan.FromMilliseconds(20),
        };

        await service.StartAsync(CancellationToken.None);

        // Let several intervals elapse while the first attempt is still gated —
        // the single-flight guard must prevent a second concurrent attempt.
        await Task.Delay(150);
        Assert.Equal(1, backupService.CallCount);

        backupService.Gate.SetResult(true);
        bool completed = await TestTiming.WaitUntilAsync(() => storage.StoreCallCount >= 1, TimeSpan.FromSeconds(2));
        Assert.True(completed, "Expected the gated attempt to complete once released.");

        // After completion the guard must reset — a later tick can start attempt #2.
        bool secondStarted = await TestTiming.WaitUntilAsync(() => backupService.CallCount >= 2, TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        Assert.True(secondStarted, "Expected the guard to reset after completion, allowing a later attempt.");
    }

    [Fact]
    public async Task ExecuteAsync_BackupFails_LoggedAndHostSurvives_NextTickStillRuns()
    {
        FakeDatabaseBackupService backupService = new() { FailFirstCalls = 1 };
        FakeBackupStorage storage = new();
        CapturingLogger<DatabaseBackupHostedService> logger = new();
        DatabaseBackupHostedService service = new(
            EnabledOptions(), backupService, storage, new KeepLastNRetentionPolicy(), logger)
        {
            IntervalOverride = TimeSpan.FromMilliseconds(25),
        };

        await service.StartAsync(CancellationToken.None);
        // Wait for the failure-triggering call PLUS a subsequent successful store —
        // the store count (not just CallCount) is the real proof that ticking
        // survived the failure, and is immune to extra ticks racing the shutdown.
        bool succeededAfterFailure = await TestTiming.WaitUntilAsync(() => storage.StoreCallCount >= 1, TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        Assert.True(backupService.CallCount >= 2, "A failed attempt must not stop the host from ticking again.");
        Assert.True(succeededAfterFailure, "A subsequent tick after the failure must still complete and store a dump.");
        Assert.Contains(
            logger.Entries,
            e => e.Level == LogLevel.Error && e.Message.Contains("backup", StringComparison.OrdinalIgnoreCase));
    }
}
