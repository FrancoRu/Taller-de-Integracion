using Application.DTOs.Backup.Response;
using Application.Interfaces.Backup;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Admin-only management of database backups: lists the catalog, triggers a manual backup, deletes a backup, and restores from one, with outcomes mapped explicitly to status codes.
/// </summary>
[Route("api/backups")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class BackupController(IBackupOperationsService operations) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<BackupRecordResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BackupRecordResponse>>> GetAll(CancellationToken ct)
    {
        IReadOnlyList<BackupRecord> records = await operations.ListNewestFirstAsync(ct);
        IReadOnlyList<BackupRecordResponse> response = records.Select(BackupRecordResponse.FromEntity).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Triggers a manual backup, returning 409 if a backup or restore is already in progress and 500 if the backup itself fails.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BackupRecordResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        BackupOperationResult result = await operations.CreateBackupAsync(BackupOrigin.Manual, ct);

        return result.Outcome switch
        {
            BackupOperationOutcome.Completed => Ok(result.Record),
            BackupOperationOutcome.Busy => Conflict(result.Message),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result.Message),
        };
    }

    /// <summary>
    /// Deletes a catalogued backup, returning 404 if no backup matches the id and 409 if a backup or restore is currently in progress.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        BackupOperationResult result = await operations.DeleteBackupAsync(id, ct);

        return result.Outcome switch
        {
            BackupOperationOutcome.Completed => NoContent(),
            BackupOperationOutcome.NotFound => NotFound(),
            BackupOperationOutcome.Busy => Conflict(result.Message),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result.Message),
        };
    }

    /// <summary>
    /// Restores the database from a catalogued backup, returning on success the automatic safety backup record taken just before the restore, not the restored backup itself; returns 404 if no backup matches the id and 409 if a backup or restore is already in progress.
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BackupRecordResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        BackupOperationResult result = await operations.RestoreBackupAsync(id, ct);

        return result.Outcome switch
        {
            BackupOperationOutcome.Completed => Ok(result.Record),
            BackupOperationOutcome.NotFound => NotFound(),
            BackupOperationOutcome.Busy => Conflict(result.Message),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result.Message),
        };
    }
}
