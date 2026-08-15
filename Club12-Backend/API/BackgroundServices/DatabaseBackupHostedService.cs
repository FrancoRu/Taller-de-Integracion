using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Backup;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.BackgroundServices;

/// <summary>
/// Drives the scheduled database backup: a <see cref="PeriodicTimer"/> loop
/// that, once per elapsed interval, creates a dump (<see cref="IDatabaseBackupService"/>),
/// stores it (<see cref="IBackupStorage"/>), and prunes stale backups per the
/// configured retention policy (<see cref="IBackupRetentionPolicy"/>).
///
/// No-ops entirely when <see cref="BackupOptions.Enabled"/> is <c>false</c>
/// (spec: "Backup Enabled Gate") — this is checked here, independent of
/// whatever gates <c>Program.cs</c> applies before registering this as an
/// <see cref="IHostedService"/>, so the gate is honored even if the service
/// is constructed/started directly.
///
/// A tick is fired-and-forgotten from the timer loop (rather than awaited
/// inline) so a slow/blocked dump never delays the loop's own timing; an
/// <see cref="Interlocked"/>-guarded single-flight flag ensures an
/// already-running attempt is never started again by an overlapping tick
/// (spec: proposal's "Long dump blocks/overlaps runs" risk). A failed
/// attempt (<see cref="BackupExecutionException"/>, or any unexpected
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
    /// derived from <see cref="BackupOptions.IntervalHours"/>, letting tests
    /// use a short, deterministic interval instead of sleeping for real
    /// hours-scale durations. Always <c>null</c> in production wiring (the
    /// codebase has no <c>InternalsVisibleTo</c> convention, so this is
    /// public rather than internal — it is not exercised by any production
    /// code path).
    /// </summary>
    public TimeSpan? IntervalOverride { get; init; }

    private int _isRunning; // 0 = idle, 1 = a backup attempt is in flight.
    private Task? _inFlightRun;

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
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown via StopAsync.
        }
        finally
        {
            if (_inFlightRun is not null)
                await _inFlightRun.ConfigureAwait(false);
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
            // Defense-in-depth: any unexpected failure from a port implementation
            // (e.g. an unhandled storage upload error) must not crash the host or
            // stop future scheduled attempts.
            logger.LogError(ex, "Unexpected error during scheduled backup attempt.");
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }
    }
}
