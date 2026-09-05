using Domain.Enums;

using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Match : EntityBase
{
    public DateTime MatchDate { get; set; }

    /// <summary>
    /// The matchday (jornada) this match belongs to within its stage, 1-based
    /// (HU-63/HU-65). This — not the calendar date — is the canonical fixture
    /// grouping key: "Fecha 1", "Fecha 2", … Every team plays at most once per
    /// round, and with an odd number of teams exactly one team is idle ("libre")
    /// each round. The round order is fixed (HU-67): editing a match's calendar
    /// date (HU-68) never changes its round. Null for matches that have no
    /// round-robin matchday (e.g. knockout/elimination stages) and for legacy
    /// rows created before this concept existed.
    /// </summary>
    public int? Round { get; set; }

    public required MatchType Type { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public match links.
    /// Generated once from the home/visitor team names and match date at
    /// creation time and never changed afterward, so shared links keep
    /// working even if a team is renamed.
    /// </summary>
    public required string Slug { get; set; }

    public Team? HomeTeam { get; set; }

    public Guid? HomeTeamId { get; set; }

    public Team? VisitorTeam { get; set; }

    public Guid? VisitorTeamId { get; set; }

    /// <summary>Null until the match result is loaded.</summary>
    public int? HomeScore { get; set; }

    /// <summary>Null until the match result is loaded.</summary>
    public int? VisitorScore { get; set; }

    public required bool IsFinished { get; set; }

    /// <summary>
    /// Whether the match was decided in overtime (basketball rule: a tied
    /// game plays extra time rather than ending in a draw). Purely
    /// informational — it does not affect scoring or standings, which are
    /// already derived from <see cref="HomeScore"/>/<see cref="VisitorScore"/>.
    /// </summary>
    public bool WentToOvertime { get; set; }

    /// <summary>
    /// The match's result lifecycle state (HU-69): Scheduled (no result yet),
    /// Played (decisive result loaded), Suspended, or WalkOver. Kept alongside
    /// <see cref="IsFinished"/> for backward compatibility: a match is finished
    /// when its status is <see cref="MatchStatus.Played"/> or
    /// <see cref="MatchStatus.WalkOver"/>.
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
