using Application.Utils.Constants.Validation;

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth.Request;

/// <summary>
/// HU-09: request to create a user by email only. No password is set at
/// creation time — the system emails a magic activation link the user
/// consumes (see <see cref="ActivateAccountRequest"/>) to set their own
/// password.
/// </summary>
public sealed class InviteUserRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public required string Email { get; set; }

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
