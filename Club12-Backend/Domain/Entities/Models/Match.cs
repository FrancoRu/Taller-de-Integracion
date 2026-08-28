using Domain.Enums;

using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

/// <summary>
/// Represents a match in the Club12 application.
/// </summary>
public class Match : EntityBase
{
    /// <summary>
    /// Represents the date of the match.
    /// </summary>
    public DateTime MatchDate { get; set; }

    /// <summary>
    /// Represents the type of the match (regular or playoff).
    /// </summary>
    public required MatchType Type { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public match links.
    /// Generated once from the home/visitor team names and match date at
    /// creation time and never changed afterward, so shared links keep
    /// working even if a team is renamed.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// Represents the home team in the match.
    /// </summary>
    public Team? HomeTeam { get; set; }

    /// <summary>
    /// Represents the ID of the home team.
    /// </summary>
    public Guid? HomeTeamId { get; set; }

    /// <summary>
    /// Represents the visitor team in the match.
    /// </summary>
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
    public required bool IsFinished { get; set; }

    /// <summary>
    /// The match's result lifecycle state (HU-69): Scheduled (no result yet),
    /// Played (decisive result loaded), Suspended, or WalkOver. Kept alongside
    /// <see cref="IsFinished"/> for backward compatibility: a match is finished
    /// when its status is <see cref="MatchStatus.Played"/> or
    /// <see cref="MatchStatus.WalkOver"/>.
    /// </summary>
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

    /// <summary>
    /// Represents the winning team in the match.
    /// </summary>
    public Team? WinningTeam { get; set; }

    /// <summary>
    /// Represents the ID of the winning team.
    /// </summary>
    public Guid? WinningTeamId { get; set; }

    /// <summary>
    /// Represents the division the match belongs to.
    /// </summary>
    public Stage Stage { get; set; } = default!;

    /// <summary>
    /// Represents the ID of the venue the match belongs to.
    /// </summary>
    public Guid? VenueId { get; set; }

    /// <summary>
    /// Represents the venue the match belongs to.
    /// </summary>
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
    /// Represents the collection of scorer statistics associated with the match.
    /// </summary>
    public virtual ICollection<Scorer> Scorers { get; set; } = [];

    /// <summary>
    /// The best-of-N series this game belongs to, when the stage's BestOf
    /// is greater than 1. Null for single-game rounds (BestOf == 1).
    /// </summary>
    public Guid? SeriesId { get; set; }
    public MatchSeries? Series { get; set; }

    /// <summary>
    /// The game number within its series (1-based). Null when the match
    /// does not belong to a series.
    /// </summary>
    public int? GameNumber { get; set; }
}
