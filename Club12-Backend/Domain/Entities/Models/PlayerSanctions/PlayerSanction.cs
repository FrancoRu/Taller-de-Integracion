using Entities.Models.Matches;
using Entities.Models.Players;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.PlayerSanctions;

/// <summary>
/// Represents a player sanction in the Club12 application.
/// </summary>
[Table("PlayerSanctions", Schema = "Club12")]
public class PlayerSanction : EntityBase
{
    /// <summary>
    /// The duration of the sanction in fixtures.
    /// </summary>
    [Required]
    public required int Duration { get; set; }

    /// <summary>
    /// The date when the sanction was issued.
    /// </summary>
    [Required]
    public required DateTime IssuedDate { get; set; }

    /// <summary>
    /// A description detailing the sanction.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public required string Description { get; set; }

    /// <summary>
    /// The player who has received the sanction.
    /// </summary>
    [Required]
    [Column("PlayerId")]
    [ForeignKey(nameof(PlayerId))]
    public required Player Player { get; set; }

    /// <summary>
    /// The unique identifier of the player associated with the sanction.
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// The match associated with the sanction.
    /// </summary>
    [Required]
    [Column("MatchId")]
    [ForeignKey(nameof(MatchId))]
    public required Match Match { get; set; }

    /// <summary>
    /// The unique identifier of the match associated with the sanction.
    /// </summary>
    public Guid MatchId { get; set; }
}
