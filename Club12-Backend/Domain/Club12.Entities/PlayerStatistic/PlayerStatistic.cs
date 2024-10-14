using Club12.Entities.MatchEntity;
using Club12.Entities.PlayerEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.PlayersStatisticEntity;

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
    public required double Value { get; set; }

    /// <summary>
    /// Represents the match associated with the player statistic.
    /// </summary>
    [Required]
    [Column("MatchId")]
    public required Match Match { get; set; }

    /// <summary>
    /// Represents the ID of the match associated with the player statistic.
    /// </summary>
    public Guid MatchId { get; set; }

    /// <summary>
    /// Represents the player associated with the player statistic.
    /// </summary>
    [Required]
    [Column("PlayerId")]
    public required Player Player { get; set; }

    /// <summary>
    /// Represents the ID of the player associated with the player statistic.
    /// </summary>
    public Guid PlayerId { get; set; }
}
