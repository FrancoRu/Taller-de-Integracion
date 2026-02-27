using Application.DTOs.Abstract.Response;
using Application.DTOs.Player.Response;
using System;
using System.Collections.Generic;

namespace Application.DTOs.Team.Response;

/// <summary>
/// Represents a response for a team, inheriting from the base response.
/// </summary>
public class TeamResponse : BaseEntityResponse
{
    /// <summary>
    /// The name of the team.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The three-letter code of the team.
    /// </summary>
    public required string ThreeLetterCode { get; set; }

    /// <summary>
    /// The color of the team's shirt.
    /// </summary>
    public required string ShirtColor { get; set; }

    /// <summary>
    /// The URL of the team's logo.
    /// </summary>
    public required string LogoUrl { get; set; }

    public Guid? TournamentId { get; set; }

    /// <summary>
    /// The list of players in the team.
    /// </summary>
    public required List<PublicPlayerResponse> Players { get; set; } = [];
}
