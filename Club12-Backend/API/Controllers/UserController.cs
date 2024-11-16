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
/// <remarks>
/// Initializes a new instance of the <see cref="UserController"/> class.
/// </remarks>
/// <param name="_userService">The user service.</param>
/// <param name="_authService">The auth service.</param>
//[Authorize(Roles = "SuperAdmin")]
[ApiController]
[Route("api/users/")]
public class UserController(
    IUserService _userService,
    IAuthService _authService
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
        User? user = _userService.GetUserByUserNameAsync(userLoginRequest.Username);

        if (user == null || !_userService.ValidateCredentials(user, userLoginRequest.Password))
        {
            return Unauthorized("Invalid credentials");
        }

        TokenResponse token = _authService.GenerateJwtToken(user);

        return Ok(token);
    }
}