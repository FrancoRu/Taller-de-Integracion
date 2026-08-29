using Application.DTOs.Backup.Response;
using Application.Interfaces.Backup;

using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Admin-only maintenance-window status and manual escape hatch
/// (database-restore#Maintenance-Mode-Window). GET reports the current
/// IMaintenanceModeState; DELETE force-clears a stuck window — e.g.
/// the process stayed alive but a restore's finally block never ran
/// (design.md's "Stuck maintenance window" rollout note; this path is
/// intentionally allow-listed by MaintenanceModeMiddleware so it
/// stays reachable even while maintenance is active, while
/// [Authorize(Roles = Admin)] here still fully applies —
/// threat-matrix "Escape-hatch abuse"). Mirrors
/// DataMaintenanceController's shape.
/// </summary>
[Route("api/maintenance")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class MaintenanceController(IMaintenanceModeState maintenanceModeState) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaintenanceStatusResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<MaintenanceStatusResponse> GetStatus()
    {
        MaintenanceStatusResponse response = new(
            maintenanceModeState.IsActive, maintenanceModeState.Reason, maintenanceModeState.EnteredAtUtc);
        return Ok(response);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Exit()
    {
        maintenanceModeState.Exit();
        return NoContent();
    }
}
