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

    /// <summary>Primary shirt color (#rrggbb), for rendering the kit.</summary>
    public string? ShirtColor { get; set; }

    /// <summary>Jersey kit pattern (e.g. "solid", "stripes").</summary>
    public string? JerseyStyle { get; set; }

    /// <summary>Secondary shirt color (#rrggbb), for the kit trim/pattern.</summary>
    public string? ShirtSecondaryColor { get; set; }

    /// <summary>Third shirt color (#rrggbb), used only by tri-color kit templates.</summary>
    public string? ShirtTertiaryColor { get; set; }

    public int Score { get; set; }

    public List<ScorerByPlayerResponse> Scorers { get; set; } = [];

    public List<PublicPlayerResponse> Players { get; set; } = [];
}
