using API.Utils.Helpers;
using Application.DTOs.Abstract.Response;
using Application.DTOs.User.Request;
using Application.DTOs.User.Response;
using Application.Interfaces.Services;
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
    IUserManagementService userManagementService) : ControllerBase
{
    // ─────────────────────────────────────────────────────────────
    // GET /api/users
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK,        Type = typeof(PaginatedResponse<UserResponse>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<UserResponse>>> GetAll(
        [FromQuery] UserFilteredRequest filter, CancellationToken ct)
    {
        var (role, id) = User.GetCallerClaims();  
        return Ok(await userManagementService.GetAllAsync(role, id, filter, ct));
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/users/{userId}
    // ─────────────────────────────────────────────────────────────

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK,        Type = typeof(UserResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(Guid userId, CancellationToken ct)
    {
        var (role, id) = User.GetCallerClaims();  
        return Ok(await userManagementService.GetByIdAsync(role, id, userId, ct));
    }

    // ─────────────────────────────────────────────────────────────
    // PUT /api/users/{userId}   — profile fields only
    // ─────────────────────────────────────────────────────────────

    [HttpPut("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK,        Type = typeof(UserResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> Update(
        Guid userId, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var (role, id) = User.GetCallerClaims();  
        return Ok(await userManagementService.UpdateAsync(role, id, userId, request, ct));
    }

    // ─────────────────────────────────────────────────────────────
    // PUT /api/users/{userId}/password   — password change (known current)
    // ─────────────────────────────────────────────────────────────

    [HttpPut("{userId:guid}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        Guid userId, [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var (role, id) = User.GetCallerClaims();  
        await userManagementService.ChangePasswordAsync(role, id, userId, request, ct);
        return NoContent();
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/users/{userId}/password/reset   — forced reset (Admin / Owner only)
    // ─────────────────────────────────────────────────────────────

    [HttpPost("{userId:guid}/password/reset")]
    [ProducesResponseType(StatusCodes.Status200OK,        Type = typeof(ResetPasswordResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(
        Guid userId, CancellationToken ct)
    {
        var (role, id) = User.GetCallerClaims();  
        return Ok(await userManagementService.ResetPasswordAsync(role, id, userId, ct));
    }

    // ─────────────────────────────────────────────────────────────
    // DELETE /api/users/{userId}
    // ─────────────────────────────────────────────────────────────

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken ct)
    {
        var (role, id) = User.GetCallerClaims();
        await userManagementService.DeleteAsync(role, id, userId, ct);
        return NoContent();
    }

    // ─────────────────────────────────────────────────────────────
    // PUT /api/users/{userId}/active   — activate / deactivate account
    // ─────────────────────────────────────────────────────────────

    [HttpPut("{userId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status200OK,        Type = typeof(UserResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> SetActive(
        Guid userId, [FromBody] SetUserActiveRequest request, CancellationToken ct)
    {
        var (role, id) = User.GetCallerClaims();
        return Ok(await userManagementService.SetActiveAsync(role, id, userId, request.IsActive, ct));
    }
}
