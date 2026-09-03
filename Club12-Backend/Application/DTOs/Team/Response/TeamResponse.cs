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
    /// The unique, URL-friendly identifier used in public team links.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// The three-letter code of the team.
    /// </summary>
    public required string ThreeLetterCode { get; set; }

    /// <summary>
    /// The color of the team's shirt.
    /// </summary>
    public required string ShirtColor { get; set; }

    /// <summary>
    /// The jersey kit pattern applied over the primary shirt color. See
    /// JERSEY_STYLES (frontend) for the full, current list.
    /// </summary>
    public string JerseyStyle { get; set; } = "solid";

    /// <summary>
    /// Optional secondary #rrggbb hex color used for the jersey pattern/trim.
    /// Null when the kit has no accent color.
    /// </summary>
    public string? ShirtSecondaryColor { get; set; }

    /// <summary>
    /// Optional third #rrggbb hex color, used only by the tri-color kit
    /// templates. Null when the selected template does not use one.
    /// </summary>
    public string? ShirtTertiaryColor { get; set; }

    /// <summary>
    /// The URL of the team's logo.
    /// </summary>
    public required string LogoUrl { get; set; }

    public Guid? TournamentId { get; set; }

    /// <summary>
    /// The name of the team's current tournament (<see cref="TournamentId"/>),
    /// e.g. "Torneo Apertura Masculino 2025" — disambiguates same-named teams
    /// from different seasons in an "existing team" picker. Null when
    /// <see cref="TournamentId"/> is null.
    /// </summary>
    public string? TournamentName { get; set; }

    /// <summary>
    /// The club this team belongs to, letting the frontend link a team to its
    /// club. Null when the team is not associated with a club.
    /// </summary>
    public Guid? ClubId { get; set; }

    /// <summary>
    /// The list of players in the team.
    /// </summary>
    public required List<PublicPlayerResponse> Players { get; set; } = [];
}
