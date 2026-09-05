using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.PlayerStatistic.Request;

/// <summary>
/// A single player's points within a team's match sheet.
/// </summary>
public class PlayerScoreEntry
{
    /// <summary>
    /// The player who scored.
    /// </summary>
    [Required]
    public required Guid PlayerId { get; set; }

    /// <summary>
    /// The points the player scored in the match; may be zero.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Points must be a non-negative number.")]
    public required int Points { get; set; }
}
