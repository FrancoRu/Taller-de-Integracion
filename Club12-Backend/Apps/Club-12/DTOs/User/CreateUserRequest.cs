using System.ComponentModel.DataAnnotations;

namespace Club12.Viewmodels.User;

/// <summary>
/// Represents a request object for creating or updating a user.
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// The username of the user.
    /// </summary>
    [Required(ErrorMessage = "The UserName field is required.")]
    public required string UserName { get; set; }

    /// <summary>
    /// The password of the user.
    /// </summary>
    [Required(ErrorMessage = "The Password field is required.")]
    public required string Password { get; set; }
}
