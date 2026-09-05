using API.Utils.Helpers;

using Application.DTOs.Auth.Request;
using Application.DTOs.Auth.Response;
using Application.Interfaces.Services;
using Application.Utils.Constants;

using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Authentication and account-lifecycle endpoints; login, magic links, guest sessions, and password reset are anonymous, while registering or inviting a user requires Admin or Owner.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    /// <summary>
    /// Creates a fully activated account with a caller-set password; requires Admin or Owner rather than being a public self-registration endpoint.
    /// </summary>
    [Authorize(Roles = Roles.AdminOrOwner)]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(RegisterUserResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterUserResponse>> Register(
        [FromBody] RegisterUserRequest request, CancellationToken ct)
    {
        string callerRole = User.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new UnauthorizedAccessException(ErrorMessages.Auth.RoleClaimMissing);

        Guid callerId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException(ErrorMessages.Auth.IdClaimMissing));

        RegisterUserResponse response =
            await authenticationService.RegisterAsync(request, callerRole, callerId, ct);

        return CreatedAtAction(nameof(Register), new { userId = response.UserId }, response);
    }

    /// <summary>
    /// Creates a user by email only, with no password, and emails a magic activation link; requires Admin or Owner.
    /// </summary>
    [Authorize(Roles = Roles.AdminOrOwner)]
    [HttpPost("invite")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(InviteUserResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InviteUserResponse>> Invite(
        [FromBody] InviteUserRequest request, CancellationToken ct)
    {
        string callerRole = User.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new UnauthorizedAccessException(ErrorMessages.Auth.RoleClaimMissing);

        Guid callerId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException(ErrorMessages.Auth.IdClaimMissing));

        InviteUserResponse response =
            await authenticationService.InviteUserAsync(request, callerRole, callerId, ct);

        return CreatedAtAction(nameof(Invite), new { userId = response.UserId }, response);
    }

    /// <summary>
    /// Consumes the activation token from the invitation email, sets the user's first password, and returns a ready-to-use JWT.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("activate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Activate(
        [FromBody] ActivateAccountRequest request, CancellationToken ct)
    {
        return Ok(await authenticationService.ActivateAccountAsync(request, ct));
    }

    /// <summary>
    /// Emails a password-reset magic link for the given email and always returns 200 so it never reveals whether the email has an account.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("password-reset/request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPasswordReset(
        [FromBody] RequestPasswordResetRequest request, CancellationToken ct)
    {
        await authenticationService.RequestPasswordResetAsync(request, ct);
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(
        [FromBody] LogInUserRequest request, CancellationToken ct)
    {
        return Ok(await authenticationService.LoginAsync(request, ct));
    }

    [AllowAnonymous]
    [HttpPost("magic-link/request")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MagicLinkResponse))]
    public async Task<ActionResult<MagicLinkResponse>> RequestMagicLink(
        [FromBody] MagicLinkRequest request, CancellationToken ct)
    {
        return Ok(await authenticationService.RequestMagicLinkAsync(request, ct));
    }

    [AllowAnonymous]
    [HttpPost("magic-link/login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> MagicLinkLogin(
        [FromBody] MagicLinkLoginRequest request, CancellationToken ct)
    {
        return Ok(await authenticationService.MagicLinkLoginAsync(request, ct));
    }

    [AllowAnonymous]
    [HttpPost("guest")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    public async Task<ActionResult<TokenResponse>> Guest(CancellationToken ct)
    {
        return Ok(await authenticationService.GuestAsync(ct));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Refresh(
        [FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        return Ok(await authenticationService.RefreshAsync(request, ct));
    }

    /// <summary>
    /// Verifies the password-reset token from the email link, sets the new password, and returns a ready-to-use JWT, logging the user in automatically.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("password-reset/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> ConfirmPasswordReset(
        [FromBody] PasswordResetConfirmRequest request, CancellationToken ct)
    {
        return Ok(await authenticationService.ConfirmPasswordResetAsync(request, ct));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        (string _, Guid id) = User.GetCallerClaims();
        await authenticationService.LogoutAsync(id, ct);
        return NoContent();
    }
}