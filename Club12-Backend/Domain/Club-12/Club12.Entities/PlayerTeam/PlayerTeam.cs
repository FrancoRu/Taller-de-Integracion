using Club12.Entities.PlayerEntity;
using Club12.Entities.TeamEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.PlayersTeamsEntity;

/// <summary>
/// Represents a N:N relationship between a player and a team.
/// </summary>
[Table("PlayerTeams", Schema = "Club12")]
public class PlayerTeam : EntityBase
{
    /// <summary>
    /// The jersey number of the player in that team at that time.
    /// </summary>
    [Required]
    public required int JerseyNumber { get; set; } = 0;

    /// <summary>
    /// Date since the player belongs to that team.
    /// </summary>
    [Required]
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Represents the player associated with the team.
    /// </summary>
    [Required]
    [Column("PlayerId")]
    public required Player Player { get; set; }

    /// <summary>
    /// Represents the ID of the player associated with the team.
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// Represents the team associated with the player.
    /// </summary>
    [Required]
    [Column("TeamId")]
    public required Team Team { get; set; }

    /// <summary>
    /// Represents the ID of the team associated with the player.
    /// </summary>
    public Guid TeamId { get; set; }
}
