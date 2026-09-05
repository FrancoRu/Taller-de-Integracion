using Microsoft.AspNetCore.Http;

using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Team.Request;

/// <summary>
/// Represents a request to create a team.
/// </summary>
public class CreateTeamRequest
{
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; init; }

    [Required(ErrorMessage = "The Three-letter code field is required.")]
    [MaxLength(3)]
    public required string ThreeLetterCode { get; init; }

    [Required(ErrorMessage = "The ShirtColor field is required")]
    public required string ShirtColor { get; set; }

    /// <summary>
    /// The jersey kit pattern applied over the primary shirt color. Defaults
    /// to "solid" when not supplied.
    /// </summary>
    [MaxLength(20)]
    public string JerseyStyle { get; set; } = "solid";

    /// <summary>
    /// Optional secondary #rrggbb hex color used for the jersey pattern/trim.
    /// </summary>
    [MaxLength(9)]
    public string? ShirtSecondaryColor { get; set; }

    /// <summary>
    /// Optional third #rrggbb hex color, used only by the tri-color kit
    /// templates as a second accent alongside <see cref="ShirtSecondaryColor"/>.
    /// </summary>
    [MaxLength(9)]
    public string? ShirtTertiaryColor { get; set; }

    /// <summary>
    /// The logo image file to upload (must be JPEG or PNG).
    /// </summary>
    [Required(ErrorMessage = "The Logo image is required.")]
    [DataType(DataType.Upload)]
    public required IFormFile LogoFile { get; init; }

    public Guid? TournamentId { get; init; }
}
