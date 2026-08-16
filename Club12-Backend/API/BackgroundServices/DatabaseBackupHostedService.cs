using Application.Interfaces.Backup;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace API.BackgroundServices;

/// <summary>
/// Drives the scheduled database backup: a PeriodicTimer loop
/// that, once per elapsed interval, creates a dump (IDatabaseBackupService),
/// stores it (IBackupStorage), and prunes stale backups per the
/// configured retention policy (IBackupRetentionPolicy).
///
/// No-ops entirely when BackupOptions.Enabled is false
/// (spec: "Backup Enabled Gate") — this is checked here, independent of
/// whatever gates Program.cs applies before registering this as an
/// IHostedService, so the gate is honored even if the service
/// is constructed/started directly.
///
/// A tick is fired-and-forgotten from the timer loop (rather than awaited
/// inline) so a slow/blocked dump never delays the loop's own timing; an
/// Interlocked-guarded single-flight flag ensures an
/// already-running attempt is never started again by an overlapping tick
/// (spec: proposal's "Long dump blocks/overlaps runs" risk). A failed
/// attempt (BackupExecutionException, or any unexpected
/// exception from a port implementation) is logged and never propagates —
/// it must not crash the host or stop later scheduled attempts (spec:
/// "Backup Failure Isolation").
/// </summary>
public sealed class DatabaseBackupHostedService(
    BackupOptions options,
    IDatabaseBackupService backupService,
    IBackupStorage backupStorage,
    IBackupRetentionPolicy retentionPolicy,
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

    /// <summary>
    /// 0 = idle, 1 = a backup attempt is in flight.
    /// </summary>
    private int _isRunning;
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
                _inFlightRun = TryStartBackupAttempt(stoppingToken);
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

    private Task TryStartBackupAttempt(CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
        {
            logger.LogWarning("Skipping scheduled backup attempt: a previous attempt is still running.");
            return Task.CompletedTask;
        }

        return RunBackupAttemptAsync(ct);
    }

    private async Task RunBackupAttemptAsync(CancellationToken ct)
    {
        try
        {
            await using Stream dump = await backupService.CreateDumpAsync(ct);
            string name = $"backup-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.sql";
            await backupStorage.StoreAsync(name, dump, ct);

            IReadOnlyList<BackupFile> existing = await backupStorage.ListAsync(ct);
            IReadOnlyList<BackupFile> toDelete = retentionPolicy.SelectForDeletion(existing, options.RetentionCount);

#pragma warning disable S3267
            foreach (BackupFile stale in toDelete)
            {
                try
                {
                    await backupStorage.DeleteAsync(stale.Name, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete stale backup {Name} during retention pruning.", stale.Name);
                }
            }
#pragma warning restore S3267

            logger.LogInformation(
                "Scheduled backup completed: stored {Name}, pruned {PrunedCount} stale backup(s).",
                name, toDelete.Count);
        }
        catch (BackupExecutionException ex)
        {
            logger.LogError(ex, "Scheduled backup attempt failed.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected error during scheduled backup attempt.");
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }
    }
}
