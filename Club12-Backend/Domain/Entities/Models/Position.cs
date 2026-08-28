using Domain.Enums;

using System;

namespace Domain.Entities.Models;

/// <summary>
/// Represents the position of a team in a division, including various match statistics.
/// This is used in the service layer.
/// </summary>
public class Position
{
    /// <summary>
    /// The unique identifier of the team.
    /// </summary>
    public required Guid TeamId { get; set; }

    /// <summary>
    /// The name of the team.
    /// </summary>
    public required string TeamName { get; set; }

    /// <summary>
    /// The URL of the team's logo.
    /// </summary>
    public required string LogoUrl { get; set; }

    /// <summary>
    /// The total number of matches the team has played.
    /// </summary>
    public required int MatchesPlayed { get; set; }

    /// <summary>
    /// The number of matches the team has won.
    /// </summary>
    public required int Wins { get; set; }

    /// <summary>
    /// The number of matches the team has lost.
    /// </summary>
    public required int Losses { get; set; }

    /// <summary>
    /// The total points scored by the team across all matches.
    /// </summary>
    public required int PointsFor { get; set; }

    /// <summary>
    /// The total points scored against the team by opposing teams.
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
}
