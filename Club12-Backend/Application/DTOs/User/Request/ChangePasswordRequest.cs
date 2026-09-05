using Application.Utils.Constants.Validation;

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.User.Request;

/// <summary>
/// Request to change a user's password; self-service changes require CurrentPassword.
/// </summary>
public sealed class ChangePasswordRequest
{
    [Required]
    [MinLength(UserFieldLengths.PasswordMinLength)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Required only for self-service password changes.
    /// </summary>
    public string? CurrentPassword { get; set; }
}