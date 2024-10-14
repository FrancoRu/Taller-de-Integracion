using Club12.DTOs.User;
using Club12.Entities.TokenResponse;
using Club12.Entities.UserEntity;
using Club12.Services.Auth;
using Club12.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Club12.Controllers;

/// <summary>
/// Controller for managing users.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UserController"/> class.
/// </remarks>
/// <param name="userService">The user service.</param>
/// <param name="authService">The auth service.</param>
[Authorize(Roles = "SuperAdmin")]
[ApiController]
[Route("api/")]
public class UserController(
    IUserService userService,
    IAuthService authService
    ) : ControllerBase
{

    /// <summary>
    /// Logs in a user and generates a JWT token.
    /// </summary>
    /// <param name="userLoginRequest">The user login request.</param>
    /// <returns>
    /// Returns 200 (Ok) with the generated JWT token if the login is successful.
    /// Returns 401 (Unauthorized) if the credentials are invalid.
    /// </returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login(LogInUserRequest userLoginRequest)
    {
        User? user = userService.GetUserByUserName(userLoginRequest.UserName);

        if (user == null || !userService.ValidateCredentials(user, userLoginRequest.Password))
        {
            return Unauthorized("Invalid credentials");
        }

        TokenResponse token = authService.GenerateJwtToken(user);

        return Ok(token);
    }
}