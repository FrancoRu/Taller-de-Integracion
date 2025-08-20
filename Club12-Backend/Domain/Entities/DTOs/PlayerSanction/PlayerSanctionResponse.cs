using Entities.DTOs.Abstract;

namespace Entities.DTOs.PlayerSanction;

/// <summary>
/// Represents a response for a Player Sanction.
/// </summary>
public class PlayerSanctionResponse : BaseEntityResponse
{
    /// <summary>
    /// The duration in fixtures of the sanction.
    /// </summary>
    public required int Duration { get; set; }

    /// <summary>
    /// Represents the date the sanction was issued.
    /// </summary>
    public required DateTime IssuedDate { get; set; }

    /// <summary>
    /// A description of the sanction.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The unique identifier of the player who has the sanction.
    /// </summary>
    public required Guid PlayerId { get; set; }

    public required string PlayerFullName { get; set; }

    /// <summary>
    /// The unique identifier of the match associated with the sanction.
    /// </summary>
    public required Guid MatchId { get; set; }
}
