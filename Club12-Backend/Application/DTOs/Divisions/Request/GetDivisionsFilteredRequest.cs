using Application.DTOs.Abstract.Request;

using System;
namespace Application.DTOs.Divisions.Request;

/// <summary>
/// Represents a request to get filtered divisions.
/// </summary>
public class GetDivisionsFilteredRequest : PaginatedFilterRequest
{
    public string? Name { get; set; }

    public bool? IsFinished { get; set; }

    public Guid? TournamentId { get; set; }
}
