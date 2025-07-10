using Entities.Models.Matches;
using Entities.Models.Players;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.PlayerStatistics;

/// <summary>
/// Represents a player statistic in the Club12 application.
/// </summary>
[Table("PlayersStatistics", Schema = "Club12")]
public class PlayerStatistic : EntityBase
{
    /// <summary>
    /// The value of the statistic for the player.
    /// </summary>
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Value must be a non-negative integer.")]
    public required int Value { get; set; }

    /// <summary>
    /// Represents the match associated with the player statistic.
    /// </summary>
    [Required]
    [Column("MatchId")]
    public Match Match { get; set; } = default!;

    /// <summary>
    /// Represents the ID of the match associated with the player statistic.
    /// </summary>
    public Guid MatchId { get; set; }

    /// <summary>
    /// Represents the player associated with the player statistic.
    /// </summary>
    [Required]
    [Column("PlayerId")]
    public Player Player { get; set; } = default!;

    /// <summary>
    /// Represents the ID of the player associated with the player statistic.
    /// </summary>
    public Guid PlayerId { get; set; }
}
