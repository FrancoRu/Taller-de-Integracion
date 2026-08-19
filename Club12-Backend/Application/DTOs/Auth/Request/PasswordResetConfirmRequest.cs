using Application.Utils.Constants.Validation;

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth.Request;

/// <summary>
/// Payload for confirming a password reset from the email link.
/// </summary>
public sealed class PasswordResetConfirmRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Token received via email (URL-decoded by the framework automatically).</summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(UserFieldLengths.PasswordMinLength)]
    public string NewPassword { get; set; } = string.Empty;
}
