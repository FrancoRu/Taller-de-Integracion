using API.Utils.Helpers;

using Application.DTOs.Abstract.Response;
using Application.DTOs.User.Request;
using Application.DTOs.User.Response;
using Application.Interfaces.Services;

using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// User management endpoints (CRUD + logout).
/// Access rules are enforced in IUserManagementService.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UserController(
    IUserManagementService userManagementService,
    IAuditService auditService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<UserResponse>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<UserResponse>>> GetAll(
        [FromQuery] UserFilteredRequest filter, CancellationToken ct)
    {
        (string? role, Guid id) = User.GetCallerClaims();
        return Ok(await userManagementService.GetAllAsync(role, id, filter, ct));
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(Guid userId, CancellationToken ct)
    {
        (string? role, Guid id) = User.GetCallerClaims();
        return Ok(await userManagementService.GetByIdAsync(role, id, userId, ct));
    }

    [HttpPut("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> Update(
        Guid userId, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        (string? role, Guid id) = User.GetCallerClaims();
        return Ok(await userManagementService.UpdateAsync(role, id, userId, request, ct));
    }

    [HttpPut("{userId:guid}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        Guid userId, [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        (string? role, Guid id) = User.GetCallerClaims();
        await userManagementService.ChangePasswordAsync(role, id, userId, request, ct);
        return NoContent();
    }

    [HttpPost("{userId:guid}/password/reset")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResetPasswordResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(
        Guid userId, CancellationToken ct)
    {
        (string? role, Guid id) = User.GetCallerClaims();
        ResetPasswordResponse response = await userManagementService.ResetPasswordAsync(role, id, userId, ct);

        // HU-101: record the password reset / blanqueo for traceability. The
        // target's email is looked up separately since ResetPasswordResponse
        // only carries the id — worth the extra read so the audit trail shows
        // who, not just an opaque guid.
        UserResponse resetUser = await userManagementService.GetByIdAsync(role, id, userId, ct);
        await auditService.LogAsync(
            AuditAction.PasswordReset,
            targetType: "User",
            targetId: userId.ToString(),
            targetName: resetUser.Email,
            ct: ct);

        return Ok(response);
    }

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken ct)
    {
        (string? role, Guid id) = User.GetCallerClaims();
        await userManagementService.DeleteAsync(role, id, userId, ct);
        return NoContent();
    }

    [HttpPut("{userId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> SetActive(
        Guid userId, [FromBody] SetUserActiveRequest request, CancellationToken ct)
    {
        (string? role, Guid id) = User.GetCallerClaims();
        return Ok(await userManagementService.SetActiveAsync(role, id, userId, request.IsActive, ct));
    }
}
