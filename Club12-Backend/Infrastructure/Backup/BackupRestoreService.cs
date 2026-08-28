using Application.Interfaces.Backup;
using Application.Interfaces.Maintenance;

using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Backup;

/// <summary>
/// Restore orchestration (HU-93). Runs under the app-wide maintenance lock
/// (HU-92) and always creates a safety backup <em>before</em> touching the
/// database, so a failed restore never leaves the operator without a recovery
/// path:
/// <list type="number">
/// <item>dump the current state and store it as a safety backup;</item>
/// <item>retrieve the chosen backup and replay it via
/// IDatabaseRestoreService (which also cleans up the transient restore
/// copy);</item>
/// <item>on success, delete the safety backup — the database is now the
/// restored one, so the safeguard is no longer needed;</item>
/// <item>on failure, keep the safety backup and rethrow, so the operator can
/// recover from it.</item>
/// </list>
/// The pg process and the storage are behind ports here, so this
/// safety-first / cleanup-on-success / keep-on-failure sequence is fully
/// unit-testable without a real Postgres.
/// </summary>
public sealed class BackupRestoreService(
    IDatabaseBackupService backupService,
    IDatabaseRestoreService restoreService,
    IBackupStorage backupStorage,
    IMaintenanceState maintenanceState,
    ILogger<BackupRestoreService> logger) : IBackupRestoreService
{
    public async Task<RestoreResult> RestoreAsync(string backupName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupName);

        using IDisposable lease = maintenanceState.Enter("restore");

        string safetyName = $"safety-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.sql";
        await using (Stream safetyDump = await backupService.CreateDumpAsync(ct))
        {
            await backupStorage.StoreAsync(safetyName, safetyDump, ct);
        }

        logger.LogInformation("Restore safety backup stored as {SafetyName}.", safetyName);

        try
        {
            await using Stream chosen = await backupStorage.RetrieveAsync(backupName, ct);
            await restoreService.RestoreAsync(chosen, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Restore from {BackupName} failed; safety backup {SafetyName} kept for recovery.",
                backupName, safetyName);
            throw;
        }

        // Success: the database is now the restored one, so the safeguard is no
        // longer needed. The transient restore copy was already deleted inside
        // IDatabaseRestoreService.RestoreAsync.
        await backupStorage.DeleteAsync(safetyName, ct);
        logger.LogInformation(
            "Restore from {BackupName} succeeded; safety backup {SafetyName} deleted.",
            backupName, safetyName);

        return new RestoreResult(backupName, safetyName);
    }
}
