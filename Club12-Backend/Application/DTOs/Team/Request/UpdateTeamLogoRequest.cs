using Microsoft.AspNetCore.Http;

using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Team.Request;

/// <summary>
/// Represents a request to update a team's logo.
/// </summary>
public class UpdateTeamLogoRequest
{
    [Required(ErrorMessage = "The TeamId field is required.")]
    public required Guid TeamId { get; init; }

    /// <summary>
    /// The logo image file to upload, which must be JPEG or PNG.
    /// </summary>
    [Required(ErrorMessage = "The Logo image is required.")]
    [DataType(DataType.Upload)]
    public required IFormFile LogoFile { get; init; }
}
