using Entities.Models.Players;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.PlayerSanctions;

/// <summary>
/// Represents a Player Sanction in the Club12 application.
/// </summary>
[Table("PlayerSanctions", Schema = "Club12")]
public class PlayerSanction : EntityBase
{
    /// <summary>
    /// The duration in fixtures of the sanction.
    /// </summary>
    [Required]
    public required int Duration { get; set; }

    /// <summary>
    /// Represents the date the sanction was issued.
    /// </summary>
    [Required]
    public required DateTime IssuedDate { get; set; }

    /// <summary>
    /// A description of the sanction.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public required string Description { get; set; }

    /// <summary>
    /// The player who has a sanction.
    /// </summary>
    [Required]
    [Column("PlayerId")]
    public required Player Player { get; set; }

    /// <summary>
    /// Represents the ID of the player who has a sanction.
    /// </summary>
    public Guid PlayerId { get; set; }
}
