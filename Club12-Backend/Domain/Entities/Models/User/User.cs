using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.UserEntity;

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

    /// <summary>
    /// The refresh token of the user.
    /// </summary>
    [MaxLength(256)]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// The expiration date and time of the refresh token.
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }
}
