using Application.DTOs.Abstract.Response;
using Application.DTOs.AuditLogs.Request;
using Application.DTOs.AuditLogs.Response;
using Application.Interfaces.Services;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Read-only access to the sensitive-action audit trail (HU-101). Restricted
/// to Admin/Owner: the trail exists to hold those shared accounts accountable.
/// </summary>
[Route("api/audit-logs")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class AuditLogController(IAuditService auditService, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Lists audit entries (newest first) with pagination and optional
    /// actor/action filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<AuditLogResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<AuditLogResponse>>> GetAuditLogs(
        [FromQuery] AuditLogFilteredRequest filter)
    {
        PaginatedResponse<AuditLog> entries = await auditService.GetAuditLogsAsync(filter);
        PaginatedResponse<AuditLogResponse> response = mapper.Map<PaginatedResponse<AuditLogResponse>>(entries);

        return Ok(response);
    }
}
