using Application.DTOs.Player.Response;
using Application.DTOs.Scorer.Response;

using System;
using System.Collections.Generic;

namespace Application.DTOs.Team.Response;

/// <summary>
/// Represents the response data for a team in a match.
/// </summary>
public class TeamDetailedMatchResponse
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string LogoUrl { get; set; }

    /// <summary>
    /// Primary shirt color as #rrggbb, for rendering the kit.
    /// </summary>
    public string? ShirtColor { get; set; }

    /// <summary>
    /// Jersey kit pattern.
    /// </summary>
    public string? JerseyStyle { get; set; }

    /// <summary>
    /// Secondary shirt color as #rrggbb, for the kit trim or pattern.
    /// </summary>
    public string? ShirtSecondaryColor { get; set; }

    /// <summary>
    /// Third shirt color as #rrggbb, used only by tri-color kit templates.
    /// </summary>
    public string? ShirtTertiaryColor { get; set; }

    public int Score { get; set; }

    public List<ScorerByPlayerResponse> Scorers { get; set; } = [];

    public List<PublicPlayerResponse> Players { get; set; } = [];
}
