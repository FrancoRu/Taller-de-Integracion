using Entities.Models.DivisionEntity;
using Entities.Models.TeamEntity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.MatchEntity;

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
    /// Represents the type of the match (regular or playoff).
    /// </summary>
    [Required]
    public required MatchType Type { get; set; }

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
    /// Represents the home team's score.
    /// </summary>
    public int? HomeScore { get; set; }

    /// <summary>
    /// Represents the visitor team's score.
    /// </summary>
    public int? VisitorScore { get; set; }

    /// <summary>
    /// Indicates whether the match has finished.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public required bool IsFinished { get; set; }

    /// <summary>
    /// Represents the winning team in the match.
    /// </summary>
    [Column("WinningTeamId")]
    public Team? WinningTeam { get; set; }

    /// <summary>
    /// Represents the ID of the winning team.
    /// </summary>
    public Guid? WinningTeamId { get; set; }

    /// <summary>
    /// Represents the winning team in the match.
    /// </summary>
    [Column("DivisionId")]
    public required Division Division { get; set; }

    /// <summary>
    /// Division Id the match belongs to.
    /// </summary>
    public Guid DivisionId { get; set; }
}
