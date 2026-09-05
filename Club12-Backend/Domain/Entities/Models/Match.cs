using Domain.Enums;

using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Match : EntityBase
{
    public DateTime MatchDate { get; set; }

    /// <summary>
    /// The 1-based matchday this match belongs to within its stage — the canonical fixture grouping key, not the calendar date.
    /// </summary>
    public int? Round { get; set; }

    public required MatchType Type { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public match links, generated once from the team names and match date and never changed afterward.
    /// </summary>
    public required string Slug { get; set; }

    public Team? HomeTeam { get; set; }

    public Guid? HomeTeamId { get; set; }

    public Team? VisitorTeam { get; set; }

    public Guid? VisitorTeamId { get; set; }

    /// <summary>
    /// Null until the match result is loaded.
    /// </summary>
    public int? HomeScore { get; set; }

    /// <summary>
    /// Null until the match result is loaded.
    /// </summary>
    public int? VisitorScore { get; set; }

    public required bool IsFinished { get; set; }

    /// <summary>
    /// Whether the match was decided in overtime, purely informational and with no effect on scoring or standings.
    /// </summary>
    public bool WentToOvertime { get; set; }

    /// <summary>
    /// The match's result lifecycle state: Scheduled, Played, Suspended, or WalkOver.
    /// </summary>
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

    public Team? WinningTeam { get; set; }

    public Guid? WinningTeamId { get; set; }

    public Stage Stage { get; set; } = default!;

    public Guid? VenueId { get; set; }

    public Venue? Venue { get; set; }

    public Guid StageId { get; set; }

    public virtual ICollection<PlayerStatistic> PlayerStatistics { get; set; } = [];

    public virtual ICollection<Scorer> Scorers { get; set; } = [];

    /// <summary>
    /// The best-of-N series this game belongs to, when the stage's BestOf is greater than 1, null for single-game rounds.
    /// </summary>
    public Guid? SeriesId { get; set; }
    public MatchSeries? Series { get; set; }

    /// <summary>
    /// The 1-based game number within its series, null when the match does not belong to a series.
    /// </summary>
    public int? GameNumber { get; set; }
}
