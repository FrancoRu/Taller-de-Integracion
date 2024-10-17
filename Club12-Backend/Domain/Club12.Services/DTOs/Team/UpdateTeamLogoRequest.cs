using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Club12.Services.DTOs.Team;

/// <summary>
/// Represents a request to update a team's logo.
/// </summary>
public record UpdateTeamLogoRequest
{
    /// <summary>
    /// The unique identifier of the team whose logo is being updated.
    /// </summary>
    [Required(ErrorMessage = "The TeamId field is required.")]
    public required Guid TeamId { get; init; }

    /// <summary>
    /// The logo image file to upload (must be JPEG or PNG).
    /// </summary>
    [Required(ErrorMessage = "The Logo image is required.")]
    [DataType(DataType.Upload)]
    public required IFormFile LogoFile { get; init; }
}
