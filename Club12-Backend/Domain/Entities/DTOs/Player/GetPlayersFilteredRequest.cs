using Entities.DTOs.Abstract;

namespace Entities.DTOs.Player;

/// <summary>
/// Represents a request to get filtered players.
/// </summary>
public class GetPlayersFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// The name of the player to filter by.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The last name of the player to filter by.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// The document number of the player to filter by.
    /// </summary>
    public string? DocumentNumber { get; set; }
}
