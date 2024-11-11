using Entities.DTOs.Abstract;

namespace Entities.DTOs.Team;

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
}
