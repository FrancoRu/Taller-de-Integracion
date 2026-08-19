using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Response;

using System;
using System.Collections.Generic;
namespace Application.DTOs.Divisions.Response;

/// <summary>
/// Represents a response for a division, inheriting from the base response.
/// </summary>
public class DetailedDivisionResponse : BaseEntityResponse
{
    /// <summary>
    /// The name of the division.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// If the division is finished.
    /// </summary>
    public required bool IsFinished { get; set; }

    /// <summary>
    /// The positions of teams in the division.
    /// </summary>
    public required IEnumerable<PositionResponse> Positions { get; set; }

    /// <summary>
    /// The Id of the tournament this division belongs to.
    /// </summary>
    public required Guid TournamentId { get; set; }

    /// <summary>
    /// The matches grouped by week in the division.
    /// </summary>
    public required IDictionary<int, IEnumerable<MinimalMatchResponse>> MatchesByWeek { get; set; }
}
