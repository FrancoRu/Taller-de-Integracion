using Domain.Enums;

using System;

namespace Domain.Entities.Models;

/// <summary>
/// Represents the position of a team in a division, including various match statistics.
/// This is used in the service layer.
/// </summary>
public class Position
{
    public required Guid TeamId { get; set; }

    public required string TeamName { get; set; }

    public required string LogoUrl { get; set; }

    public required int MatchesPlayed { get; set; }

    public required int Wins { get; set; }

    public required int Losses { get; set; }

    /// <summary>
    /// Basketball score totaled across all matches — distinct from
    /// <see cref="Points"/>, which is the standings/table score derived from
    /// the division's points-per-win/loss configuration.
    /// </summary>
    public required int PointsFor { get; set; }

    /// <summary>
    /// Basketball score conceded, totaled across all matches — the
    /// counterpart to <see cref="PointsFor"/>. Not to be confused with the
    /// table score in <see cref="Points"/>.
    /// </summary>
    public required int PointsAgainst { get; set; }

    /// <summary>
    /// The point difference (PointsFor - PointsAgainst) for the team.
    /// </summary>
    public required int PointsDifference { get; set; }

    /// <summary>
    /// The total points accumulated by the team, calculated from the
    /// division's configured points-per-win and points-per-loss (HU-79;
    /// defaults 2 per win, 1 per loss).
    /// </summary>
    public required int Points { get; set; }

    /// <summary>
    /// The tiebreaker criterion (HU-80) that separated this team from the
    /// team ranked immediately above it. Null for the top team and for teams
    /// that are not tied with the team above them on table points. Lets the
    /// standings UI show why each tie was broken.
    /// </summary>
    public TiebreakerCriterion? ResolvedBy { get; set; }

    /// <summary>
    /// The disciplinary point deduction applied to this team, when any. Null
    /// when the team has no deduction. When present, <see cref="Points"/> has
    /// already had the deduction subtracted; this only carries the amount and
    /// reason so the standings can show a "-N (motivo)" note.
    /// </summary>
    public AppliedPointDeduction? PointDeduction { get; set; }
}
