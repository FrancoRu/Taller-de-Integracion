using Entities.DTOs.User;
using Entities.Models.UserEntity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Auth;
using Services.Services.UserService;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing users.
/// </summary>
/// <param name="_userService">The user service.</param>
/// <param name="_authService">The auth service.</param>
//[Authorize(Roles = "SuperAdmin")]
[ApiController]
[Route("api/users/")]
public class UserController(IUserService _userService, IAuthService _authService) : ControllerBase
{
    /// <summary>
    /// Logs in a user and generates a JWT token.
    /// </summary>
    /// <param name="userLoginRequest">The user login request containing username and password.</param>
    /// <returns>
    /// A 200 OK response with the generated JWT token if login is successful.
    /// A 401 Unauthorized response if the credentials are invalid.
    /// </returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LogInUserRequest userLoginRequest)
    {
        User? user = await _userService.GetUserByUserNameAsync(userLoginRequest.Username);
        if (user is null || !await _userService.ValidateCredentialsAsync(user, userLoginRequest.Password))
        {
            return Unauthorized("Invalid credentials");
        }

        TokenResponse token = await _authService.GenerateJwtTokenAsync(user);

        user.RefreshToken = token.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.Add(token.ExpiresIn);
        await _userService.UpdateUserAsync(user);

        return Ok(token);
    }

    /// <summary>
    /// Refreshes the JWT token using a valid refresh token.
    /// </summary>
    /// <param name="refreshTokenRequest">The request containing the refresh token.</param>
    /// <returns>
    /// A 200 OK response with a new token pair (access and refresh tokens) if successful.
    /// A 401 Unauthorized response if the refresh token is invalid.
    /// </returns>
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest refreshTokenRequest)
    {
        User? user = await _userService.GetUserByRefreshTokenAsync(refreshTokenRequest.RefreshToken);

        if (user is null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            return Unauthorized("Invalid or expired refresh token");
        }

        TokenResponse token = await _authService.GenerateJwtTokenAsync(user);

        user.RefreshToken = token.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.Add(token.ExpiresIn);
        await _userService.UpdateUserAsync(user);

        return Ok(token);
    }
}
