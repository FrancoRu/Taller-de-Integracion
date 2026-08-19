using Application.DTOs.Abstract.Request;

using System;

namespace Application.DTOs.Team.Request;

/// <summary>
/// Represents a request to get filtered teams.
/// </summary>
public class GetTeamsFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// The name of the team to filter by.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The three-letter code of the team to filter by.
    /// </summary>
    public string? ThreeLetterCode { get; set; }

    /// <summary>
    /// The color of the team's shirt.
    /// </summary>
    public string? ShirtColor { get; set; }

    public Guid? StageId { get; set; }
    public Guid? TournamentId { get; set; }
}
