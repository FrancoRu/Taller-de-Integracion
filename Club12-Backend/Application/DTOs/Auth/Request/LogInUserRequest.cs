using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Auth.Request;

/// <summary>
/// Represents a request object for user login.
/// </summary>
public class LogInUserRequest
{
    [Required(ErrorMessage = "The Email field is required.")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "The Password field is required.")]
    public required string Password { get; set; }
}
