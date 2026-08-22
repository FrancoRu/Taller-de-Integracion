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
[Authorize(Roles = Roles.Admin)]
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
    /// database-restore#Restore-Executes-Directly-Against-the-Live-Database.
    /// The only input is the route Guid — no request body is bound
    /// (threat-matrix "Restore of foreign/uploaded dumps": there is no
    /// upload endpoint, so a restore can only ever target an existing
    /// catalogued backup by id). Confirmation naming the target's
    /// Fecha before this call is a frontend concern (Phase 5), not
    /// enforced server-side. On success the response carries the automatic
    /// pre-restore safety backup's record (design.md's HTTP contract).
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
