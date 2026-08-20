using Application.Interfaces.Backup;

using Domain.Enums;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.BackgroundServices;

/// <summary>
/// Drives the scheduled database backup: a PeriodicTimer loop that,
/// once per elapsed interval, resolves the scoped
/// IBackupOperationsService and calls
/// CreateBackupAsync(BackupOrigin.Job) — the same shared write
/// path the manual Admin endpoint uses (spec
/// scheduled-database-backups#Scheduled-Runs-Share-the-Catalog-Write-Path-With-Manual-Runs).
///
/// No-ops entirely when BackupOptions.Enabled is false
/// (spec: "Backup Enabled Gate") — this is checked here, independent of
/// whatever gates Program.cs applies before registering this as an
/// IHostedService, so the gate is honored even if the service
/// is constructed/started directly.
///
/// A tick is fired-and-forgotten from the timer loop (rather than awaited
/// inline) so a slow/blocked attempt never delays the loop's own timing.
/// There is no longer a local single-flight flag: the shared
/// BackupOperationLock inside IBackupOperationsService is the one
/// guard for both scheduled and manual attempts (design.md's "the
/// semaphore subsumes it" decision) — an overlapping tick, or a manual
/// request racing a scheduled attempt, is told Busy by the
/// use case and simply logged here, never started twice. A failed
/// attempt (Failed outcome, or any unexpected exception from
/// resolving/calling the scoped service) is logged and never propagates —
/// it must not crash the host or stop later scheduled attempts (spec:
/// "Backup Failure Isolation").
///
/// A DI scope is created per tick (via IServiceScopeFactory) because
/// IBackupOperationsService is scoped (it depends on the scoped
/// IBackupCatalog/ApplicationDBContext), while this hosted service
/// itself is a singleton.
/// </summary>
public sealed class DatabaseBackupHostedService(
    IServiceScopeFactory scopeFactory,
    BackupOptions options,
    ILogger<DatabaseBackupHostedService> logger) : BackgroundService
{
    /// <summary>
    /// Test-only hook: overrides the interval that would otherwise be
    /// derived from BackupOptions.IntervalHours, letting tests
    /// use a short, deterministic interval instead of sleeping for real
    /// hours-scale durations. Always null in production wiring (the
    /// codebase has no InternalsVisibleTo convention, so this is
    /// public rather than internal — it is not exercised by any production
    /// code path).
    /// </summary>
    public TimeSpan? IntervalOverride { get; init; }

    private Task? _inFlightRun;

    /// <summary>
    /// Ticks on a PeriodicTimer for the lifetime of the host. Cancellation via
    /// stoppingToken during host shutdown is the expected exit path, not an error.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("Backup:Enabled is false — DatabaseBackupHostedService will not run.");
            return;
        }

        TimeSpan interval = IntervalOverride ?? TimeSpan.FromHours(Math.Max(options.IntervalHours, 1));

        using PeriodicTimer timer = new(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _inFlightRun = RunBackupAttemptAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(ex, "Backup hosted service stopping: cancellation requested.");
        }
        finally
        {
            if (_inFlightRun is not null)
            {
                await _inFlightRun.ConfigureAwait(false);
            }
        }
    }

    private async Task RunBackupAttemptAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IBackupOperationsService operations = scope.ServiceProvider.GetRequiredService<IBackupOperationsService>();

            BackupOperationResult result = await operations.CreateBackupAsync(BackupOrigin.Job, ct);

            switch (result.Outcome)
            {
                case BackupOperationOutcome.Busy:
                    logger.LogWarning("Skipping scheduled backup attempt: another backup/restore operation is already in progress.");
                    break;
                case BackupOperationOutcome.Failed:
                    logger.LogError("Scheduled backup attempt failed: {Message}", result.Message);
                    break;
                case BackupOperationOutcome.Completed:
                    logger.LogInformation("Scheduled backup completed: stored {StoragePath}.", result.Record?.StoragePath);
                    break;
                case BackupOperationOutcome.NotFound:
                default:
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected error during scheduled backup attempt.");
        }
    }
}
