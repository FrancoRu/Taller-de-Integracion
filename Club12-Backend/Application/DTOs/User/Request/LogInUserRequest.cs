using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.User.Request;

/// <summary>
/// Represents a request object for user login.
/// </summary>
public class LogInUserRequest
{
    /// <summary>
    /// The username for login.
    /// </summary>
    [Required(ErrorMessage = "The UserName field is required.")]
    public required string Username { get; set; }

    /// <summary>
    /// The password for login.
    /// </summary>
    [Required(ErrorMessage = "The Password field is required.")]
    public required string Password { get; set; }
}
