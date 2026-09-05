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
    /// The total points accumulated by the team, with any disciplinary deduction already subtracted.
    /// </summary>
    public required int Points { get; set; }

    /// <summary>
    /// The disciplinary point deduction applied to this team, or null when there is none.
    /// </summary>
    public AppliedPointDeductionResponse? PointDeduction { get; set; }
}