namespace Entities.Models.ScorerModel;

/// <summary>
/// Represents detailed statistics for a scorer in a match.
/// </summary>
public class Scorer
{
    /// <summary>
    /// Gets or sets the unique identifier of the player.
    /// </summary>
    public required Guid PlayerId { get; set; }

    /// <summary>
    /// Gets or sets the full name of the player.
    /// </summary>
    public required string Names { get; set; }

    /// <summary>
    /// Gets or sets the last name of the player.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Gets or sets the number of points scored by the player in the match.
    /// </summary>
    public required int Points { get; set; }

    /// <summary>
    /// Gets or sets the team ID of the player.
    /// </summary>
    public required Guid TeamId { get; set; }

    /// <summary>
    /// Gets or sets the name of the team the player is a part of.
    /// </summary>
    public required string TeamName { get; set; }
}
