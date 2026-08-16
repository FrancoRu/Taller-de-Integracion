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

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
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
    /// Verifies the password-reset token from the email link, sets the new password,
    /// and returns a ready-to-use JWT. The user is automatically logged in after reset.
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