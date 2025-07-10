using Entities.DTOs.Abstract;

namespace Entities.DTOs.Divisions;

/// <summary>
/// Represents a request to get filtered divisions.
/// </summary>
public class GetDivisionsFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// The name of the division to filter by.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Indicates whether to filter divisions by their finished status.
    /// </summary>
    public bool? IsFinished { get; set; }
}
