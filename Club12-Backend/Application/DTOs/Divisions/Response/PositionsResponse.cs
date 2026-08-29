using System;

using Application.DTOs.PointDeductions.Response;

namespace Application.DTOs.Divisions.Response;


/// <summary>
/// Represents the position of a team in a division, including various match statistics.
/// </summary>
public class PositionResponse
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
    /// The total points accumulated by the team, calculated based on wins and losses (2 points per win, 1 point per loss).
    /// Any disciplinary deduction (see <see cref="PointDeduction"/>) has
    /// already been subtracted from this value.
    /// </summary>
    public required int Points { get; set; }

    /// <summary>
    /// The disciplinary point deduction applied to this team, when any. Null
    /// when the team has no deduction. Lets the standings show a "-N (motivo)"
    /// note; the subtraction is already reflected in <see cref="Points"/>.
    /// </summary>
    public AppliedPointDeductionResponse? PointDeduction { get; set; }
}