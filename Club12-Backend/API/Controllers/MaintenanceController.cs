using Application.DTOs.Backup.Response;
using Application.Interfaces.Backup;

using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Reports maintenance-mode status and lets an admin force-clear a stuck maintenance window.
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
