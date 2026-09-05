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
    public required string Name { get; set; }

    public required bool IsFinished { get; set; }

    public required IEnumerable<PositionResponse> Positions { get; set; }

    public required Guid TournamentId { get; set; }

    /// <summary>
    /// The matches grouped by week in the division.
    /// </summary>
    public required IDictionary<int, IEnumerable<MinimalMatchResponse>> MatchesByWeek { get; set; }
}
