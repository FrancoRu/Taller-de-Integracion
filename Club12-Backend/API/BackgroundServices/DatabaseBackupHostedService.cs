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
/// Drives the scheduled database backup by ticking a PeriodicTimer and calling the same shared IBackupOperationsService write path the manual admin endpoint uses, no-oping entirely when BackupOptions.Enabled is false.
/// </summary>
public sealed class DatabaseBackupHostedService(
    IServiceScopeFactory scopeFactory,
    BackupOptions options,
    ILogger<DatabaseBackupHostedService> logger) : BackgroundService
{
    /// <summary>
    /// Test-only hook that overrides the interval otherwise derived from BackupOptions.IntervalHours, letting tests use a short, deterministic interval instead of sleeping for real hours-scale durations.
    /// </summary>
    public TimeSpan? IntervalOverride { get; init; }

    private Task? _inFlightRun;

    /// <summary>
    /// Ticks on a PeriodicTimer for the lifetime of the host; cancellation via stoppingToken during host shutdown is the expected exit path, not an error.
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
