using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.UserEntity;

/// <summary>
/// Represents a user entity in the system.
/// </summary>
[Table("Users", Schema = "Club12")]
public class User : EntityBase
{
    /// <summary>
    /// The username of the user.
    /// </summary>
    [Required]
    [MaxLength(25)]
    public required string Username { get; set; }

    /// <summary>
    /// The hashed password of the user.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public required string Password { get; set; }

    /// <summary>
    /// The role of the user.
    /// </summary>
    [Required]
    [MaxLength(10)]
    public required string Role { get; set; }
}
