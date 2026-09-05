using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Team.Request;

/// <summary>
/// Represents a request to update a team's details.
/// </summary>
public class UpdateTeamRequest
{
    public string? Name { get; init; }

    [MaxLength(3)]
    public string? ThreeLetterCode { get; init; }

    public string? ShirtColor { get; set; }

    /// <summary>
    /// The jersey kit pattern applied over the primary shirt color; left unchanged when not supplied.
    /// </summary>
    [MaxLength(20)]
    public string? JerseyStyle { get; set; }

    /// <summary>
    /// Optional secondary #rrggbb hex color used for the jersey pattern or trim.
    /// </summary>
    [MaxLength(9)]
    public string? ShirtSecondaryColor { get; set; }

    /// <summary>
    /// Optional third #rrggbb hex color, used only by tri-color kit templates as a second accent.
    /// </summary>
    [MaxLength(9)]
    public string? ShirtTertiaryColor { get; set; }
}
