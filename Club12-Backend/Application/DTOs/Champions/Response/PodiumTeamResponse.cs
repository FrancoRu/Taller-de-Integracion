using System;

namespace Application.DTOs.Champions.Response;

/// <summary>
/// A lightweight reference to a team occupying a podium place (champion,
/// runner-up or third place). Carries only what the champions/podium views
/// need to render a team badge, never the full team aggregate.
/// </summary>
public class PodiumTeamResponse
{
    /// <summary>The unique identifier of the team.</summary>
    public required Guid TeamId { get; set; }

    /// <summary>The display name of the team.</summary>
    public required string TeamName { get; set; }

    /// <summary>The URL of the team's logo.</summary>
    public required string LogoUrl { get; set; }
}
