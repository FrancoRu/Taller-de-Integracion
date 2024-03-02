using AutoMapper;
using Club12.Entities.UserEntity;
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
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class.
    /// </summary>
    /// <param name="userService">The user service.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public UserController(
        IUserService userService,
        IMapper mapper
    )
    {
        _userService = userService;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="userRequest">The user request.</param>
    /// <returns>
    /// Returns 200 (Ok) if the user was created successfully.
    /// Returns 400 (BadRequest) if the username already exists or if the requester is not a SuperAdmin.
    /// </returns>
    [HttpPost("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CreateUser(UserRequest userRequest)
    {
        User userEntity = _mapper.Map<User>(userRequest);

        if (!_userService.IsSuperAdmin(userRequest.UserRequestId))
        {
            return Forbid("Only SuperAdmin can create users.");
        }

        if (_userService.GetUserByUserName(userRequest.UserName) is not null)
        {
            return BadRequest($"Username already exists.");
        }

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
}