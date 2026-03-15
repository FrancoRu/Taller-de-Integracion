using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth.Request;

/// <summary>
/// Request to register a new user. Password is only required for non-TeamManager roles.
/// TeamManager accounts authenticate via magic-link and do not need a password.
/// </summary>
public sealed class RegisterUserRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public required string Email { get; set; }

    /// <summary>
    /// Required for ADMIN, OWNER, TOURNAMENT_MANAGER.
    /// Leave null when registering a TEAM_MANAGER (magic-link flow).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Target role. Accepted values: ADMIN, OWNER, TOURNAMENT_MANAGER, TEAM_MANAGER.
    /// </summary>
    [Required(ErrorMessage = "Role is required.")]
    public required string Role { get; set; }
}