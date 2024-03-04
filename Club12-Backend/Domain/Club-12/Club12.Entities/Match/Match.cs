using Club12.Entities.TeamEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.MatchEntity;

/// <summary>
/// Represents a match in the Club12 application.
/// </summary>
[Table("Matches", Schema = "Club12")]
public class Match : EntityBase
{
    /// <summary>
    /// Represents the date of the match.
    /// </summary>
    [Required]
    public required DateTime MatchDate { get; set; }

    /// <summary>
    /// Represents the home team's score.
    /// </summary>
    [Required]
    public required int HomeScore { get; set; }

    /// <summary>
    /// Represents the visitor team's score.
    /// </summary>
    [Required]
    public required int VisitorScore { get; set; }

    /// <summary>
    /// Represents the home team in the match.
    /// </summary>
    [Required]
    [Column("HomeTeamId")]
    public required Team HomeTeam { get; set; }

    /// <summary>
    /// Represents the ID of the home team.
    /// </summary>
    public Guid HomeTeamId { get; set; }

    /// <summary>
    /// Represents the visitor team in the match.
    /// </summary>
    [Required]
    [Column("VisitorTeamId")]
    public required Team VisitorTeam { get; set; }

    /// <summary>
    /// Represents the ID of the visitor team.
    /// </summary>
    public Guid VisitorTeamId { get; set; }

    /// <summary>
    /// Represents the winning team in the match.
    /// </summary>
    [Required]
    [Column("WinningTeamId")]
    public required Team WinningTeam { get; set; }

    /// <summary>
    /// Represents the ID of the winning team.
    /// </summary>
    public Guid WinningTeamId { get; set; }
}
