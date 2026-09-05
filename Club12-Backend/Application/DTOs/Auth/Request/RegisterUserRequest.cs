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
    /// Display name for the user; must be unique across all accounts.
    /// </summary>
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(UserFieldLengths.UsernameMaxLength, MinimumLength = UserFieldLengths.UsernameMinLength,
        ErrorMessage = "Username must be between 3 and 50 characters.")]
    public required string Username { get; set; }

    /// <summary>
    /// Optional contact phone number.
    /// </summary>
    [ArgentinePhoneNumber]
    [MaxLength(UserFieldLengths.PhoneMaxLength, ErrorMessage = "Phone number must not exceed 15 characters.")]
    public string? Phone { get; set; }

    /// <summary>
    /// Target role. Accepted values: ADMIN, OWNER.
    /// </summary>
    [Required(ErrorMessage = "Role is required.")]
    public required string Role { get; set; }
}