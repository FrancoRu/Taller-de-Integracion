using Application.Utils.Constants.Validation;

using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.PlayerSanction.Request;

/// <summary>
/// Represents a request to create a Player Sanction.
/// </summary>
public class CreatePlayerSanctionRequest
{
    /// <summary>
    /// The duration in fixtures of the sanction.
    /// </summary>
    [Required(ErrorMessage = "The Duration field is required.")]
    public required int Duration { get; set; }

    /// <summary>
    /// Represents the date the sanction was issued.
    /// </summary>
    [Required(ErrorMessage = "The IssuedDate field is required.")]
    public required DateTime IssuedDate { get; set; }

    /// <summary>
    /// A description of the sanction.
    /// </summary>
    [Required(ErrorMessage = "The Description field is required.")]
    [MaxLength(SanctionFieldLengths.DescriptionMaxLength)]
    public required string Description { get; set; }

    /// <summary>
    /// The unique identifier of the player who has a sanction.
    /// </summary>
    [Required(ErrorMessage = "The PlayerId field is required.")]
    public required Guid PlayerId { get; set; }

    public required Guid MatchId { get; set; }
}
