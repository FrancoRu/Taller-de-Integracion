using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth.Request;

/// <summary>
/// Request to login using a magic-link token.
/// </summary>
public sealed class MagicLinkLoginRequest
{
    [Required(ErrorMessage = "The Email field is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "The Token field is required.")]
    public required string Token { get; set; }
}
