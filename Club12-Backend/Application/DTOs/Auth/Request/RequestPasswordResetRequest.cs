using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth.Request;

/// <summary>
/// Self-service request to receive a password-reset magic link by email.
/// </summary>
public sealed class RequestPasswordResetRequest
{
    [Required(ErrorMessage = "The Email field is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public required string Email { get; set; }
}
