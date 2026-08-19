using System;

namespace Domain.Entities.Models;

/// <summary>
/// Represents detailed statistics for a scorer in a match.
/// </summary>
public class Scorer : EntityBase
{
    /// <summary>
    /// Gets or sets the unique identifier of the player.
    /// </summary>
    public required Guid PlayerId { get; set; }

    public Player? Player { get; set; }

    /// <summary>
    /// Gets or sets the number of points scored by the player in the match.
    /// </summary>
    public required int Points { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the match.
    /// </summary>
    public required Guid MatchId { get; set; }

    public Match? Match { get; set; }
}
