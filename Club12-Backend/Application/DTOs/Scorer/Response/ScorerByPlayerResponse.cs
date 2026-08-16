using System;
namespace Application.DTOs.Scorer.Response;

/// <summary>
/// Response model representing a scorer's performance in a match.
/// </summary>
public class ScorerByPlayerResponse : ScorerBaseResponse
{
    /// <summary>
    /// Unique identifier of the player.
    /// </summary>
    public required Guid PlayerId { get; set; }

    /// <summary>
    /// Full name of the player, combining first and last names.
    /// </summary>
    public required string FullName { get; set; }
}
