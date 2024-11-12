using Entities.DTOs.Abstract;

namespace Entities.DTOs.Tournament;

/// <summary>
/// Represents a request to get filtered tournaments.
/// </summary>
public class GetTournamentsFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// The name of the tournament to filter by.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The description of the tournament to filter by.
    /// </summary>
    public string? Description { get; set; }
}
