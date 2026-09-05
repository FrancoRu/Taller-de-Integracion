using Application.DTOs.Abstract.Request;

using Domain.Enums;
namespace Application.DTOs.Tournament.Request;

/// <summary>
/// Represents a request to get filtered tournaments.
/// </summary>
public class GetTournamentsFilteredRequest : PaginatedFilterRequest
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public TournamentStatus? Status { get; set; }
}
