using Application.Utils.Constants.Validation;

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth.Request;

/// <summary>
/// Payload the invited user submits from the activation email link to set their first password.
/// </summary>
public sealed class ActivateAccountRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Activation token received via email, URL-decoded automatically by the framework.
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(UserFieldLengths.PasswordMinLength)]
    public string NewPassword { get; set; } = string.Empty;
}
