using System.ComponentModel.DataAnnotations;

namespace Club12.DTOs.Team;

/// <summary>
/// Represents a request to create a team.
/// </summary>
public record CreateTeamRequest
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
    /// The unique identifier of the division to which the team belongs.
    /// </summary>
    [Required(ErrorMessage = "The DivisionId field is required.")]
    public required Guid DivisionId { get; init; }

    /// <summary>
    /// The logo image file to upload (must be JPEG or PNG).
    /// </summary>
    [Required(ErrorMessage = "The Logo image is required.")]
    [DataType(DataType.Upload)]
    public required IFormFile LogoFile { get; init; }
}
