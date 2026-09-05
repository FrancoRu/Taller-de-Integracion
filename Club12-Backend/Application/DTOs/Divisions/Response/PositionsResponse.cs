using System;

using Application.DTOs.PointDeductions.Response;

namespace Application.DTOs.Divisions.Response;


/// <summary>
/// Represents the position of a team in a division, including various match statistics.
/// </summary>
public class PositionResponse
{
    public required Guid TeamId { get; set; }

    public required string TeamName { get; set; }

    public required string LogoUrl { get; set; }

    public required int MatchesPlayed { get; set; }

    public required int Wins { get; set; }

    public required int Losses { get; set; }

    public required int PointsFor { get; set; }

    public required int PointsAgainst { get; set; }

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