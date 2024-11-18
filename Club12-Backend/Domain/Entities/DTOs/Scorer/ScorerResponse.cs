namespace Entities.DTOs.Scorer;

/// <summary>
/// Response model representing a scorer's performance in a match.
/// </summary>
public class ScorerResponse
{
    /// <summary>
    /// Unique identifier of the player.
    /// </summary>
    public required Guid PlayerId { get; set; }

    /// <summary>
    /// Full name of the player, combining first and last names.
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Number of points scored by the player in the match.
    /// </summary>
    public required int Points { get; set; }
}
