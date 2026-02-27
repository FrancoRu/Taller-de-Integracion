using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models;

/// <summary>
/// Represents a match in the Club12 application.
/// </summary>
[Table("Matches", Schema = "Club12")]
public class Match : EntityBase
{
    /// <summary>
    /// Represents the date of the match.
    /// </summary>
    public DateTime MatchDate { get; set; }

    /// <summary>
    /// Represents the type of the match (regular or playoff).
    /// </summary>
    [Required]
    public required MatchType Type { get; set; }

    /// <summary>
    /// Represents the home team in the match.
    /// </summary>
    [ForeignKey(nameof(HomeTeamId))]
    public Team? HomeTeam { get; set; }

    /// <summary>
    /// Represents the ID of the home team.
    /// </summary>
    public Guid? HomeTeamId { get; set; }

    /// <summary>
    /// Represents the visitor team in the match.
    /// </summary>
    [ForeignKey(nameof(VisitorTeamId))]
    public Team? VisitorTeam { get; set; }

    /// <summary>
    /// Represents the ID of the visitor team.
    /// </summary>
    public Guid? VisitorTeamId { get; set; }

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
    [ForeignKey(nameof(WinningTeamId))]
    public Team? WinningTeam { get; set; }

    /// <summary>
    /// Represents the ID of the winning team.
    /// </summary>
    public Guid? WinningTeamId { get; set; }

    /// <summary>
    /// Represents the division the match belongs to.
    /// </summary>
    [ForeignKey(nameof(StageId))]
    public Stage Stage { get; set; } = default!;

    /// <summary>
    /// Represents the ID of the venue the match belongs to.
    /// </summary>
    public Guid? VenueId { get; set; }


    /// <summary>
    /// Represents the venue the match belongs to.
    /// </summary>
    [ForeignKey(nameof(VenueId))]
    public Venue? Venue { get; set; }

    /// <summary>
    /// Represents the ID of the division the match belongs to.
    /// </summary>
    public Guid StageId { get; set; }

    /// <summary>
    /// Represents the collection of player statistics associated with the match.
    /// </summary>
    public virtual ICollection<PlayerStatistic> PlayerStatistics { get; set; } = [];

    /// <summary>
    /// The collection of home team scorers for the match.
    /// </summary>
    [NotMapped]
    public IEnumerable<Scorer> HomeScorers { get; set; } = [];

    /// <summary>
    /// The collection of visitor team scorers for the match.
    /// </summary>
    [NotMapped]
    public IEnumerable<Scorer> VisitorScorers { get; set; } = [];
}
