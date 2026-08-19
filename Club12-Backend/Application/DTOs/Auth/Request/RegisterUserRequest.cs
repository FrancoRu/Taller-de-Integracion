using Application.Utils.Constants.Validation;

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth.Request;

/// <summary>
/// Request to register a new user. Password is generated automatically by the system.
/// </summary>
public sealed class RegisterUserRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public required string Email { get; set; }

    /// <summary>
    /// Unique display name for the user. Stored in AspNetUsers.UserName.
    /// </summary>
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(UserFieldLengths.UsernameMaxLength, MinimumLength = UserFieldLengths.UsernameMinLength,
        ErrorMessage = "Username must be between 3 and 50 characters.")]
    public required string Username { get; set; }

    /// <summary>
    /// Optional contact phone number.
    /// </summary>
    [Phone(ErrorMessage = "Invalid phone number format.")]
    public string? Phone { get; set; }

    /// <summary>
    /// Target role. Accepted values: ADMIN, OWNER, TOURNAMENT_MANAGER, TEAM_MANAGER.
    /// </summary>
    [Required(ErrorMessage = "Role is required.")]
    public required string Role { get; set; }
}