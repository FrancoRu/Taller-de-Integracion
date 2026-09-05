using System;
namespace Application.DTOs.Scorer.Response;

/// <summary>
/// Response model representing a scorer's performance in a match.
/// </summary>
public class ScorerByPlayerResponse : ScorerBaseResponse
{
    public required Guid PlayerId { get; set; }

    public required string FullName { get; set; }

    /// <summary>
    /// The player's jersey number (dorsal), when known — for rendering the kit
    /// on the match scoreboard. Null when the player has no season roster number.
    /// </summary>
    public int? JerseyNumber { get; set; }

    /// <summary>The player's current team.</summary>
    public Guid TeamId { get; set; }

    /// <summary>The player's current team's name.</summary>
    public string TeamName { get; set; } = string.Empty;

    /// <summary>The player's current team's crest, for the ranking row.</summary>
    public string TeamLogoUrl { get; set; } = string.Empty;

    /// <summary>The player's current team's primary shirt color — for rendering the kit alongside the dorsal.</summary>
    public string TeamShirtColor { get; set; } = string.Empty;

    /// <summary>The player's current team's kit pattern. See <c>Team.JerseyStyle</c>.</summary>
    public string TeamJerseyStyle { get; set; } = "solid";

    /// <summary>The player's current team's secondary shirt color, when set.</summary>
    public string? TeamShirtSecondaryColor { get; set; }

    /// <summary>The player's current team's tertiary shirt color, when set.</summary>
    public string? TeamShirtTertiaryColor { get; set; }
}
