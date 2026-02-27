using Microsoft.AspNetCore.Http;

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Team.Request;

/// <summary>
/// Represents a request to create a team.
/// </summary>
public class CreateTeamRequest
{
    /// <summary>
    /// The name of the team.
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; init; }

    /// <summary>
    /// The three-letter code of the team.
    /// </summary>
    [Required(ErrorMessage = "The Three-letter code field is required.")]
    [MaxLength(3)]
    public required string ThreeLetterCode { get; init; }

    /// <summary>
    /// The color of the team's shirt.
    /// </summary>
    [Required(ErrorMessage = "The ShirtColor field is required")]
    public required string ShirtColor { get; set; }

    /// <summary>
    /// The logo image file to upload (must be JPEG or PNG).
    /// </summary>
    [Required(ErrorMessage = "The Logo image is required.")]
    [DataType(DataType.Upload)]
    public required IFormFile LogoFile { get; init; }
}
