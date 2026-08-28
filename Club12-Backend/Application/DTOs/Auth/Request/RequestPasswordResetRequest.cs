using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth.Request;

/// <summary>
/// HU-10: self-service request to receive a password-reset magic link by
/// email. Anonymous endpoint — the response never reveals whether the email
/// belongs to a real account (no user enumeration).
/// </summary>
public sealed class RequestPasswordResetRequest
{
    [Required(ErrorMessage = "The Email field is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public required string Email { get; set; }
}
