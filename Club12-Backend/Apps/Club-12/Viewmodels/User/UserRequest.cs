using System.ComponentModel.DataAnnotations;

namespace Club12.Viewmodels.User;

/// <summary>
/// Represents a request object for creating or updating a user.
/// </summary>
public class UserRequest
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

    /// <summary>
    /// The role of the user.
    /// </summary>
    [Required(ErrorMessage = "The Role field is required.")]
    public required string Role { get; set; }
}
