using Microsoft.AspNetCore.Http;

using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Team;

/// <summary>
/// Represents a batch request to create a team from an Excel file.
/// </summary>
public class BatchCreateTeamRequest
{
    /// <summary>
    /// The unique identifier of the division to which the team belongs.
    /// </summary>
    [Required(ErrorMessage = "The DivisionId field is required.")]
    public required Guid DivisionId { get; init; }

    /// <summary>
    /// The Excel file containing team data.
    /// </summary>
    [Required(ErrorMessage = "The Team Excel file is required.")]
    [DataType(DataType.Upload)]
    public required IFormFile TeamFile { get; init; }


    /// <summary>
    /// The logo image file to upload (must be JPEG or PNG).
    /// </summary>
    [Required(ErrorMessage = "The Logo image is required.")]
    [DataType(DataType.Upload)]
    public required IFormFile LogoFile { get; init; }
}
