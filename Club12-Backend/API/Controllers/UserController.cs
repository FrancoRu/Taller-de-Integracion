using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Controller for identity-user management operations.
/// Authentication (login / refresh / guest / magic-link) is handled by <see cref="AuthController"/>.
/// </summary>
[ApiController]
[Route("api/users")]
public class UserController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    /// <summary>
    /// Logs out the current authenticated user by invalidating their refresh token.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        string? email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
            return Unauthorized("User is not authenticated.");

        ApplicationUser? user = await userManager.FindByEmailAsync(email);

        if (user is null)
            return BadRequest("Something went wrong.");

        user.RefreshToken           = null;
        user.RefreshTokenExpiryTime = null;

        await userManager.UpdateAsync(user);

        return Ok();
    }
}
