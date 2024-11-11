using Club12.Entities.TeamEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.PlayerEntity;

/// <summary>
/// Represents a player in the Club12 application.
/// </summary>
[Table("Players", Schema = "Club12")]
public class Player : EntityBase
{
    /// <summary>
    /// The name of the player.
    /// </summary>
    [Required]
    public required string Name { get; set; }

    /// <summary>
    /// The last name of the player.
    /// </summary>
    [Required]
    public required string LastName { get; set; }

    /// <summary>
    /// The height of the player.
    /// </summary>
    [Required]
    public required double Height { get; set; }

    /// <summary>
    /// The weight of the player.
    /// </summary>
    [Required]
    public required double Weight { get; set; }

    /// <summary>
    /// The team the player belongs to.
    /// </summary>
    [Column("TeamId")]
    [Required]
    public required Team Team { get; set; }

    /// <summary>
    /// The Id of the team the player belongs to.
    /// </summary>
    public Guid TeamId { get; set; }
}
