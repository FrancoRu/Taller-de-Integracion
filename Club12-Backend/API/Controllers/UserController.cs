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
/// User management endpoints for CRUD and logout; access rules are enforced in IUserManagementService.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UserController(
    IUserManagementService userManagementService,
    IAuditService auditService) : ControllerBase
{
    /// <summary>
    /// Lists users paginated and filtered; Admins see every user, Owners only their subordinates, other roles get 403.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<UserResponse>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<UserResponse>>> GetAll(
        [FromQuery] UserFilteredRequest filter, CancellationToken ct)
    {
        (string? role, Guid id) = User.GetCallerClaims();
        return Ok(await userManagementService.GetAllAsync(role, id, filter, ct));
    }

    /// <summary>
    /// Retrieves a single user by id; Admins may view anyone, Owners themselves or their subordinates, other roles only themselves.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(Guid userId, CancellationToken ct)
    {
        (string? role, Guid id) = User.GetCallerClaims();
        return Ok(await userManagementService.GetByIdAsync(role, id, userId, ct));
    }

    /// <summary>
    /// Updates a user's profile fields and, if Role is set, reassigns the target's role; only Admins and Owners may reassign, and nobody may change their own.
    /// </summary>
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

    /// <summary>
    /// Changes a user's password; CurrentPassword is required for a self-service change, optional when an Admin or Owner changes another's.
    /// </summary>
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

    /// <summary>
    /// Forces a password reset, generating a new temporary password server-side and recording an audit log entry.
    /// </summary>
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

    /// <summary>
    /// Deletes a user. Admins may delete any user; Owners only their own subordinates.
    /// </summary>
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

    /// <summary>
    /// Activates or deactivates a user account via Identity lockout; a deactivated account cannot log in.
    /// </summary>
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
