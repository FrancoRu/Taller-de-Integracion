using Application.DTOs.DataMaintenance.Response;
using Application.Interfaces.Services;

using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Admin-only tools for resetting tournament-domain data to a clean,
/// realistic sample state. See
/// docs/superpowers/specs/2026-08-18-admin-test-data-tools-design.md.
/// Identity (users, roles) is never touched by either endpoint.
/// </summary>
[Route("api/data-maintenance")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
public class DataMaintenanceController(IDataMaintenanceService dataMaintenanceService) : ControllerBase
{
    /// <summary>
    /// Deletes every tournament-domain row. Identity is untouched.
    /// </summary>
    [HttpPost("wipe")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataWipeResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DataWipeResult>> Wipe(CancellationToken ct)
    {
        DataWipeResult result = await dataMaintenanceService.WipeSampleDataAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Seeds 2 complete sample tournaments. Returns 409 if the database
    /// already has tournament data — wipe first.
    /// </summary>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataSeedResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DataSeedResult>> Seed(CancellationToken ct)
    {
        DataSeedResult result = await dataMaintenanceService.SeedSampleDataAsync(ct);
        return Ok(result);
    }
}
