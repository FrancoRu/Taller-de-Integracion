using Application.DTOs.Abstract.Response;

using Domain.Enums;

using System;
namespace Application.DTOs.PlayerSanction.Response;

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

    /// <summary>
    /// The current appeal status of the sanction.
    /// </summary>
    public SanctionAppealStatus AppealStatus { get; set; }

    /// <summary>
    /// The reason submitted when the sanction was appealed.
    /// </summary>
    public string? AppealReason { get; set; }

    /// <summary>
    /// The date the appeal was submitted.
    /// </summary>
    public DateTime? AppealDate { get; set; }

    /// <summary>
    /// The decision notes recorded when the appeal was resolved.
    /// </summary>
    public string? AppealResolution { get; set; }

    /// <summary>
    /// The date the appeal was resolved.
    /// </summary>
    public DateTime? AppealResolvedDate { get; set; }
}
