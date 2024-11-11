using Entities.DTOs.Abstract;

namespace Entities.DTOs.PlayerSanction;

/// <summary>
/// Represents a request to get filtered player sanctions.
/// </summary>
public class GetPlayerSanctionsFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// The unique identifier of the player to filter sanctions by.
    /// </summary>
    public Guid? PlayerId { get; set; }

    /// <summary>
    /// The date the sanction was issued to filter by.
    /// </summary>
    public DateTime? IssuedDate { get; set; }

    /// <summary>
    /// The duration of the sanction to filter by.
    /// </summary>
    public int? Duration { get; set; }
}
