using System;

namespace Application.DTOs.Champions.Response;

/// <summary>
/// A lightweight reference to a team occupying a podium place, carrying only what a team badge needs.
/// </summary>
public class PodiumTeamResponse
{
    /// <summary>
    /// The unique identifier of the team.
    /// </summary>
    public required Guid TeamId { get; set; }

    /// <summary>
    /// The display name of the team.
    /// </summary>
    public required string TeamName { get; set; }

    /// <summary>
    /// The URL of the team's logo.
    /// </summary>
    public required string LogoUrl { get; set; }
}
