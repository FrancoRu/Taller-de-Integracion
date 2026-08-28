using Application.Utils.Constants.Validation;

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth.Request;

/// <summary>
/// HU-09: payload the invited user submits from the activation email link to
/// set their first password and enable login. The token is the magic
/// activation token issued by <c>InviteUserAsync</c>.
/// </summary>
public sealed class ActivateAccountRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Activation token received via email (URL-decoded by the framework automatically).</summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(UserFieldLengths.PasswordMinLength)]
    public string NewPassword { get; set; } = string.Empty;
}
