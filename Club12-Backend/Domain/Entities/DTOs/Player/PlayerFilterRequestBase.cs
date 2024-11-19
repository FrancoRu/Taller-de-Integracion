using Entities.DTOs.Abstract;

namespace Entities.DTOs.Player;

/// <summary>
/// Base class for filtering players with common properties.
/// </summary>
public class PlayerFilterRequestBase : PaginatedFilterRequest
{
    /// <summary>
    /// The name of the player to filter by.
    /// </summary>
    public string? Names { get; set; }

    /// <summary>
    /// The last name of the player to filter by.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// The team the player belongs to, to filter by.
    /// </summary>
    public Guid? TeamId { get; set; }
}

