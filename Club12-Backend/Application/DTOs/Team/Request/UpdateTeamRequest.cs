using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Team.Request;

/// <summary>
/// Represents a request to update a team's details.
/// </summary>
public class UpdateTeamRequest
{
    /// <summary>
    /// The name of the team.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The three-letter code of the team.
    /// </summary>
    [MaxLength(3)]
    public string? ThreeLetterCode { get; init; }

    /// <summary>
    /// The color of the team's shirt.
    /// </summary>
    public string? ShirtColor { get; set; }

    /// <summary>
    /// The jersey kit pattern applied over the primary shirt color. Left
    /// unchanged when not supplied.
    /// </summary>
    [MaxLength(20)]
    public string? JerseyStyle { get; set; }

    /// <summary>
    /// Optional secondary #rrggbb hex color used for the jersey pattern/trim.
    /// </summary>
    [MaxLength(9)]
    public string? ShirtSecondaryColor { get; set; }
}
