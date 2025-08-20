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
    /// The unique identifier of the match to filter sanctions by.
    /// </summary>
    public Guid? MatchId { get; set; }

    /// <summary>
    /// The description text to filter sanctions by.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The date the sanction was issued, used to filter sanctions.
    /// </summary>
    public DateTime? IssuedDate { get; set; }

    /// <summary>
    /// The duration of the sanction, used to filter sanctions.
    /// </summary>
    public int? Duration { get; set; }
}
