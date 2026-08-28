using Application.DTOs.Backup.Request;
using Application.DTOs.Backup.Response;
using Application.Interfaces.Backup;
using Application.Interfaces.Maintenance;
using Application.Interfaces.Services;

using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Admin/Owner backup management (HU-91/92/93): trigger a manual backup, list
/// available backups, restore from a chosen backup (safety-backup-first), and
/// read the maintenance-lock status. The create and restore operations run
/// under the app-wide maintenance lock; a request that arrives while one is
/// already running is rejected with 503.
/// </summary>
[Route("api/backups")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class BackupController(
    IManualBackupService manualBackupService,
    IBackupRestoreService backupRestoreService,
    IMaintenanceState maintenanceState,
    IAuditService auditService) : ControllerBase
{
    /// <summary>
    /// Triggers a backup on demand and returns the created backup's metadata.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BackupFile))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<BackupFile>> CreateBackup(CancellationToken ct)
    {
        try
        {
            BackupFile created = await manualBackupService.CreateBackupAsync(ct);
            return Ok(created);
        }
        catch (MaintenanceInProgressException ex)
        {
            return MaintenanceInProgress(ex);
        }
        catch (BackupExecutionException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError,
                title: "Backup failed");
        }
    }

    /// <summary>
    /// Lists the backups currently available in storage, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<BackupFile>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BackupFile>>> ListBackups(CancellationToken ct)
    {
        IReadOnlyList<BackupFile> backups = await manualBackupService.ListBackupsAsync(ct);
        return Ok(backups);
    }

    /// <summary>
    /// Restores the database from a chosen backup. A safety backup is created
    /// first; on success it is deleted, on failure it is kept so data can be
    /// recovered.
    /// </summary>
    [HttpPost("restore")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RestoreResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RestoreResult>> Restore([FromBody] RestoreRequest request, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.BackupName))
        {
            return BadRequest("A backup name is required.");
        }

        try
        {
            RestoreResult result = await backupRestoreService.RestoreAsync(request.BackupName, ct);

            // HU-101: record the sensitive restore for traceability.
            await auditService.LogAsync(
                AuditAction.BackupRestore,
                targetType: "Backup",
                targetId: request.BackupName,
                detail: $"Restored from '{request.BackupName}'; safety backup '{result.SafetyBackupName}'.",
                ct: ct);

            return Ok(result);
        }
        catch (MaintenanceInProgressException ex)
        {
            return MaintenanceInProgress(ex);
        }
        catch (BackupExecutionException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError,
                title: "Restore failed (safety backup retained)");
        }
    }

    /// <summary>
    /// Reports the current maintenance-lock state so the UI can show or clear
    /// the "operation in progress" banner. Reachable even while locked.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaintenanceStatusResponse))]
    public ActionResult<MaintenanceStatusResponse> Status()
    {
        MaintenanceStatus? current = maintenanceState.Current;
        return Ok(new MaintenanceStatusResponse(
            maintenanceState.IsActive, current?.Operation, current?.StartedAt));
    }

    private ObjectResult MaintenanceInProgress(MaintenanceInProgressException ex)
    {
        Response.Headers.RetryAfter = "5";
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            error = ex.Message,
            operation = ex.Status?.Operation,
            startedAt = ex.Status?.StartedAt,
        });
    }
}
