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
/// Admin-only management of database backups: list the catalog, trigger a
/// manual backup, and delete a backup. Create/Delete go through the shared
/// IBackupOperationsService write path (the same one the scheduled
/// job uses); GET reads directly from IBackupCatalog, the source of
/// truth for the listing (not IBackupStorage.ListAsync()).
/// Mirrors DataMaintenanceController's shape: [ApiController],
/// [Authorize(Roles = Roles.Admin)], a CancellationToken
/// parameter, and Ok(result) on success. Outcomes are mapped
/// explicitly to status codes rather than relying on exception-mapped
/// status codes (design.md's "Controllers return an explicit outcome"
/// decision), which keeps this controller's own logic pure and easy to
/// unit test.
/// </summary>
[Route("api/backups")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
#pragma warning disable S6960
public class BackupController(IBackupCatalog catalog, IBackupOperationsService operations) : ControllerBase
#pragma warning restore S6960
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<BackupRecordResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BackupRecordResponse>>> GetAll(CancellationToken ct)
    {
        IReadOnlyList<BackupRecord> records = await catalog.ListNewestFirstAsync(ct);
        IReadOnlyList<BackupRecordResponse> response = records.Select(BackupRecordResponse.FromEntity).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Triggers a manual backup. Returns 409 if a backup or restore is
    /// already in progress (busy); 500 if the backup itself fails.
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
    /// Deletes a catalogued backup. Returns 404 if no backup matches the id,
    /// 409 if a backup or restore is currently in progress (busy).
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
    /// Restores the database from a catalogued backup, executing directly
    /// against the live database. The only input is the route id — there is
    /// no upload endpoint, so a restore can only ever target an existing
    /// catalogued backup. On success the response carries the record of the
    /// automatic safety backup taken just before the restore, not the
    /// restored backup itself. Returns 404 if no backup matches the id, 409
    /// if a backup or restore is already in progress (busy).
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
