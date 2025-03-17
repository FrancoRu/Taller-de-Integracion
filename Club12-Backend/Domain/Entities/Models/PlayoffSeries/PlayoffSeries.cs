using Entities.Models.MatchEntity;
using Entities.Models.RoundNameEnum;
using Entities.Models.TeamEntity;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.PlayoffSeriesEntity;

/// <summary>
/// Represents a playoff series in the Club12 application.
/// </summary>
[Table("PlayoffSeries", Schema = "Club12")]
public class PlayoffSeries : EntityBase
{
    /// <summary>
    /// Represents the name of the playoff series (e.g., Quarterfinal, Semifinal, Final).
    /// </summary>
    [Required]
    public required RoundName RoundName { get; set; }

    /// <summary>
    /// Represents the collection of matches in the playoff series.
    /// </summary>
    public virtual ICollection<Match> Matches { get; set; } = [];

    /// <summary>
    /// Represents the ID of the winning team of the series.
    /// </summary>
    public Guid? WinningTeamId { get; set; }

    /// <summary>
    /// Represents the winning team of the series.
    /// </summary>
    [ForeignKey(nameof(WinningTeamId))]
    public virtual Team? WinningTeam { get; set; }

    /// <summary>
    /// Indicates whether the playoff series has finished.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public required bool IsFinished { get; set; }

    /// <summary>
    /// Represents the number of games required to win the series (e.g., best of 3, best of 5).
    /// </summary>
    [Required]
    public required int GamesRequiredToWin { get; set; }

    /// <summary>
    /// Represents the number of wins for the home team in the series.
    /// </summary>
    [Required]
    [DefaultValue(0)]
    public required int HomeTeamWins { get; set; }

    /// <summary>
    /// Represents the number of wins for the visitor team in the series.
    /// </summary>
    [Required]
    [DefaultValue(0)]
    public required int VisitorTeamWins { get; set; }

    /// <summary>
    /// Represents the ID of the next series in the playoff bracket.
    /// </summary>
    public Guid? NextSeriesId { get; set; }

    /// <summary>
    /// Represents the next series in the playoff bracket.
    /// </summary>
    [ForeignKey(nameof(NextSeriesId))]
    public virtual PlayoffSeries? NextSeries { get; set; }
}