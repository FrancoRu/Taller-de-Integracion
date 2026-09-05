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
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public team links.
    /// </summary>
    public required string Slug { get; set; }

    public required string ThreeLetterCode { get; set; }

    public required string ShirtColor { get; set; }

    /// <summary>
    /// The jersey kit pattern applied over the primary shirt color; see the frontend's JERSEY_STYLES list.
    /// </summary>
    public string JerseyStyle { get; set; } = "solid";

    /// <summary>
    /// Optional secondary #rrggbb hex color used for the jersey pattern or trim; null means no accent color.
    /// </summary>
    public string? ShirtSecondaryColor { get; set; }

    /// <summary>
    /// Optional third #rrggbb hex color, used only by tri-color kit templates; null when unused.
    /// </summary>
    public string? ShirtTertiaryColor { get; set; }

    public required string LogoUrl { get; set; }

    public Guid? TournamentId { get; set; }

    /// <summary>
    /// The team's current tournament name, disambiguating same-named teams across seasons; null when TournamentId is null.
    /// </summary>
    public string? TournamentName { get; set; }

    /// <summary>
    /// The club this team belongs to, letting the frontend link to it; null when not associated with a club.
    /// </summary>
    public Guid? ClubId { get; set; }

    public required List<PublicPlayerResponse> Players { get; set; } = [];
}
