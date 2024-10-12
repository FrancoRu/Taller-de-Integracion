using Club12.Entities.PlayerEntity;
using Club12.Entities.SancitonEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.SanctionPlayerEntity;

/// <summary>
/// Represents a N:N relationship between Sanction and Player in the Club12 application.
/// </summary>
[Table("SanctionPlayers", Schema = "Club12")]
public class SanctionPlayer : EntityBase
{
    /// <summary>
    /// The duration in days of the sanction.
    /// </summary>
    [Required]
    public required int Duration { get; set; }

    /// <summary>
    /// Represents the date the sanction was issued.
    /// </summary>
    [Required]
    public required DateTime IssuedDate { get; set; }

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

    /// <summary>
    /// Represents the sanction.
    /// </summary>
    [Required]
    [Column("SanctionId")]
    public required Sanction Sanction { get; set; }

    /// <summary>
    /// Represents the ID of the sanction associated with the player.
    /// </summary>
    public Guid SanctionId { get; set; }
}
