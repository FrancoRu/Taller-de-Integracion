using API.BackgroundServices;
using API.Tests.Backup.Fakes;

using Application.Interfaces.Backup;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for DatabaseBackupHostedService: interval-trigger
/// logic, the Backup:Enabled gate, and failure isolation. Since the
/// PR2 refactor, the service resolves IBackupOperationsService from a
/// DI scope per tick and no longer owns its own single-flight flag — that
/// guard now lives in BackupOperationLock, shared with the manual
/// endpoint, and is exercised at that layer
/// (BackupOperationsServiceTests). All tests use
/// DatabaseBackupHostedService.IntervalOverride (a short,
/// deterministic interval) instead of sleeping for real
/// IntervalHours-scale durations, and poll via TestTiming
/// rather than fixed sleeps to avoid flakiness.
/// </summary>
public class DatabaseBackupHostedServiceTests
{
    private static BackupOptions EnabledOptions()
    {
        return new()
        {
            Enabled = true,
            IntervalHours = 24,
            RetentionCount = 7,
        };
    }

    private static IServiceScopeFactory ScopeFactoryFor(IBackupOperationsService operations)
    {
        ServiceCollection services = new();
        services.AddSingleton(operations);
        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task ExecuteAsync_IntervalElapses_TriggersOneBackupAttempt()
    {
        FakeBackupOperationsService operations = new();
        DatabaseBackupHostedService service = new(
            ScopeFactoryFor(operations), EnabledOptions(), NullLogger<DatabaseBackupHostedService>.Instance)
        {
            IntervalOverride = TimeSpan.FromMilliseconds(30),
        };

        await service.StartAsync(CancellationToken.None);
        bool triggered = await TestTiming.WaitUntilAsync(() => operations.CreateCallCount >= 1, TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        Assert.True(triggered, "Expected at least one backup attempt after the interval elapsed.");
    }

    /// <summary>
    /// The 100ms delay is well short of the 5s interval used here.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_IntervalNotYetElapsed_NoBackupAttemptTriggered()
    {
        FakeBackupOperationsService operations = new();
        DatabaseBackupHostedService service = new(
            ScopeFactoryFor(operations), EnabledOptions(), NullLogger<DatabaseBackupHostedService>.Instance)
        {
            IntervalOverride = TimeSpan.FromSeconds(5),
        };

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, operations.CreateCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_Disabled_NeverCallsOperationsService()
    {
        BackupOptions options = new() { Enabled = false, IntervalHours = 24, RetentionCount = 7 };
        FakeBackupOperationsService operations = new();
        DatabaseBackupHostedService service = new(
            ScopeFactoryFor(operations), options, NullLogger<DatabaseBackupHostedService>.Instance)
        {
            IntervalOverride = TimeSpan.FromMilliseconds(20),
        };

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150); // several would-be intervals, to prove no scheduling ever starts
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, operations.CreateCallCount);
    }

    /// <summary>
    /// Proves the hosted service no longer guards overlapping ticks itself —
    /// it simply calls CreateBackupAsync every tick and tolerates a
    /// Busy outcome (from the shared BackupOperationLock)
    /// without throwing or stalling later ticks.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_OperationReturnsBusy_LogsAndContinuesTicking_NoThrow()
    {
        FakeBackupOperationsService operations = new()
        {
            NextCreateResult = new BackupOperationResult(BackupOperationOutcome.Busy, null, "busy"),
        };
        CapturingLogger<DatabaseBackupHostedService> logger = new();
        DatabaseBackupHostedService service = new(
            ScopeFactoryFor(operations), EnabledOptions(), logger)
        {
            IntervalOverride = TimeSpan.FromMilliseconds(20),
        };

        await service.StartAsync(CancellationToken.None);
        bool calledTwice = await TestTiming.WaitUntilAsync(() => operations.CreateCallCount >= 2, TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        Assert.True(calledTwice, "A Busy outcome must not stop later ticks from also attempting a backup.");
        Assert.Contains(
            logger.Entries,
            e => e.Level == LogLevel.Warning && e.Message.Contains("progress", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_BackupFails_LoggedAndHostSurvives_NextTickStillRuns()
    {
        FakeBackupOperationsService operations = new() { FailFirstCalls = 1 };
        CapturingLogger<DatabaseBackupHostedService> logger = new();
        DatabaseBackupHostedService service = new(
            ScopeFactoryFor(operations), EnabledOptions(), logger)
        {
            IntervalOverride = TimeSpan.FromMilliseconds(25),
        };

        await service.StartAsync(CancellationToken.None);
        bool secondCallHappened = await TestTiming.WaitUntilAsync(() => operations.CreateCallCount >= 2, TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        Assert.True(secondCallHappened, "A failed attempt must not stop the host from ticking again.");
        Assert.Contains(
            logger.Entries,
            e => e.Level == LogLevel.Error && e.Message.Contains("backup", StringComparison.OrdinalIgnoreCase));
    }
}
