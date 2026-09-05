using Domain.Enums;

using System;

namespace Domain.Entities.Models;

/// <summary>
/// Represents the position of a team in a division, including its match statistics, used in the service layer.
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
    /// Basketball score totaled across all matches, distinct from the standings score in Points.
    /// </summary>
    public required int PointsFor { get; set; }

    /// <summary>
    /// Basketball score conceded, totaled across all matches, the counterpart to PointsFor.
    /// </summary>
    public required int PointsAgainst { get; set; }

    /// <summary>
    /// The point difference, PointsFor minus PointsAgainst, for the team.
    /// </summary>
    public required int PointsDifference { get; set; }

    /// <summary>
    /// The total table points accumulated by the team, calculated from the division's points-per-win and points-per-loss configuration.
    /// </summary>
    public required int Points { get; set; }

    /// <summary>
    /// The tiebreaker criterion that separated this team from the team ranked immediately above it.
    /// </summary>
    public TiebreakerCriterion? ResolvedBy { get; set; }

    /// <summary>
    /// The disciplinary point deduction applied to this team, null when the team has no deduction.
    /// </summary>
    public AppliedPointDeduction? PointDeduction { get; set; }
}
