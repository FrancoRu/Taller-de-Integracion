using Application.DTOs.Auth.Request;
using Application.DTOs.Auth.Response;
using Application.Interfaces.Services;
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
/// Controller for authentication and authorization.
/// </summary>
/// <remarks>
/// Intentionally thin — all logic is delegated to <see cref="IAuthenticationService"/>.
/// </remarks>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    /// <summary>
    /// Registers a new user. Only ADMIN and OWNER can call this endpoint.
    /// </summary>
    /// <remarks>
    /// Role creation rules:
    /// <list type="bullet">
    ///   <item>ADMIN → ADMIN, OWNER, TOURNAMENT_MANAGER, TEAM_MANAGER</item>
    ///   <item>OWNER → TOURNAMENT_MANAGER</item>
    /// </list>
    /// TEAM_MANAGER registrations do not require a password (magic-link flow).
    /// </remarks>
    [Authorize(Roles = Roles.AdminOrOwner)]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created,    Type = typeof(RegisterUserResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterUserResponse>> Register(
        [FromBody] RegisterUserRequest request, CancellationToken ct)
    {
        string callerRole = User.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new UnauthorizedAccessException("Role claim is missing from the token.");

        RegisterUserResponse response = await authenticationService.RegisterAsync(request, callerRole, ct);
        return CreatedAtAction(nameof(Register), new { userId = response.UserId }, response);
    }

    /// <summary>
    /// Login with email and password. Not available for TEAM_MANAGER.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK,          Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(
        [FromBody] LogInUserRequest request, CancellationToken ct)
        => Ok(await authenticationService.LoginAsync(request, ct));

    /// <summary>
    /// Requests a magic-link for TEAM_MANAGER accounts.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("magic-link/request")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MagicLinkResponse))]
    public async Task<ActionResult<MagicLinkResponse>> RequestMagicLink(
        [FromBody] MagicLinkRequest request, CancellationToken ct)
        => Ok(await authenticationService.RequestMagicLinkAsync(request, ct));

    /// <summary>
    /// Completes TEAM_MANAGER login using the magic-link token.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("magic-link/login")]
    [ProducesResponseType(StatusCodes.Status200OK,          Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> MagicLinkLogin(
        [FromBody] MagicLinkLoginRequest request, CancellationToken ct)
        => Ok(await authenticationService.MagicLinkLoginAsync(request, ct));

    /// <summary>
    /// Issues an anonymous guest JWT. No database interaction.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("guest")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    public async Task<ActionResult<TokenResponse>> Guest(CancellationToken ct)
        => Ok(await authenticationService.GuestAsync(ct));

    /// <summary>
    /// Rotates the access token using a valid refresh token.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK,          Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Refresh(
        [FromBody] RefreshTokenRequest request, CancellationToken ct)
        => Ok(await authenticationService.RefreshAsync(request, ct));
}