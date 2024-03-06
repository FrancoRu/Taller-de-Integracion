using AutoMapper;
using Club12.Entities.UserEntity;
using Club12.Services.Auth;
using Club12.Services.Users;
using Club12.Viewmodels.User;
using Microsoft.AspNetCore.Mvc;

namespace Club12.Controllers;

/// <summary>
/// Controller for managing users.
/// </summary>
[ApiController]
[Route("api/")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class.
    /// </summary>
    /// <param name="userService">The user service.</param>
    /// <param name="authService">The auth service.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public UserController(
        IUserService userService,
        IAuthService authService,
        IMapper mapper
    )
    {
        _userService = userService;
        _authService = authService;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="userRequest">The user request.</param>
    /// <returns>
    /// Returns 200 (Ok) if the user was created successfully.
    /// Returns 400 (BadRequest) if the username already exists.
    /// Returns 403 (Forbidden) if the user is not SuperAdmin.
    /// </returns>
    [HttpPost("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CreateUser(UserRequest userRequest)
    {
        if (!_authService.IsTokenValid()) {
            return Forbid("Invalid Token.");
        }

        if (!_authService.IsSuperAdmin())
        {
            return Forbid("Only SuperAdmin can create users.");
        }

        if (_userService.GetUserByUserName(userRequest.UserName) is not null)
        {
            return BadRequest($"Username already exists.");
        }

        User userEntity = _mapper.Map<User>(userRequest);
        _userService.CreateUser(userEntity);

        return Ok();
    }

    /// <summary>
    /// Logs in a user and generates a JWT token.
    /// </summary>
    /// <param name="userLoginRequest">The user login request.</param>
    /// <returns>
    /// Returns 200 (Ok) with the generated JWT token if the login is successful.
    /// Returns 401 (Unauthorized) if the credentials are invalid.
    /// </returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login(UserLoginRequest userLoginRequest)
    {
        User? user = _userService.GetUserByUserName(userLoginRequest.UserName);

        if (user == null || !_userService.ValidateCredentials(user, userLoginRequest.Password))
        {
            return Unauthorized("Invalid credentials");
        }

        string token = _userService.GenerateJwtToken(user);

        return Ok(token);
    }

    /// <summary>
    /// Updates a user by its id.
    /// </summary>
    /// <param name="userId">The id of the user to update.</param>
    /// <param name="userRequest">The user request.</param>
    /// <returns>
    /// Returns 200 (OK) with the updated user response if the update was successful.
    /// Returns 400 (Bad Request) if the user with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not SuperAdmin.
    /// </returns>
    [HttpPut("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateUser(Guid userId, UserRequest userRequest)
    {
        if (!_authService.IsTokenValid())
        {
            return Forbid("Invalid Token.");
        }

        if (!_authService.IsSuperAdmin())
        {
            return Forbid("Only SuperAdmin can update users.");
        }

        User? existingUser = _userService.GetUserById(userId);

        if (existingUser is null)
        {
            return BadRequest($"User with id {userId} not found.");
        }

        _mapper.Map(userRequest, existingUser);
        bool updateResult = await _userService.UpdateUser(existingUser);

        return !updateResult ? BadRequest("Failed to update the user.") : Ok();
    }

    /// <summary>
    /// Deletes a user by its id.
    /// </summary>
    /// <param name="userId">The id of the user to delete.</param>
    /// <returns>
    /// Returns 200 (OK) if the user was successfully deleted.
    /// Returns 400 (Bad Request) if the user with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not SuperAdmin.
    /// </returns>
    [HttpDelete("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult DeleteUserById(Guid userId)
    {
        if (!_authService.IsTokenValid())
        {
            return Forbid("Invalid Token.");
        }

        if (!_authService.IsSuperAdmin())
        {
            return Forbid("Only SuperAdmin can delete users.");
        }

        User? user = _userService.GetUserById(userId);

        if (user is null)
        {
            return BadRequest($"User with id {userId} not found.");
        }

        _userService.DeleteUser(user);
        return Ok();
    }

    ///<summary>
    ///Check if the token sent is correct and belongs to a user in the DB
    /// </summary>
    /// <returns>
    /// Returns 200 (OK) if the token is valid.
    /// Returns 403 (Forbidden) if the user is not SuperAdmin.
    /// </returns>
    [HttpGet("users/verifyToken")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult VerifyToken() {
        if (_authService.IsTokenValid()) return Ok();
        return Forbid("Invalid token");
    }
}