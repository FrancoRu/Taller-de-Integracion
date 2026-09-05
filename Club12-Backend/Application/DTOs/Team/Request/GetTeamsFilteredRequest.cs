using Application.DTOs.Abstract.Request;

using System;

namespace Application.DTOs.Team.Request;

/// <summary>
/// Represents a request to get filtered teams.
/// </summary>
public class GetTeamsFilteredRequest : PaginatedFilterRequest
{
    public string? Name { get; set; }

    public string? ThreeLetterCode { get; set; }

    public string? ShirtColor { get; set; }

    public Guid? StageId { get; set; }
    public Guid? TournamentId { get; set; }
}
