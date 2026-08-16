using Microsoft.AspNetCore.Http;

using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Team.Request;

/// <summary>
/// Represents a request to update a team's logo.
/// </summary>
public class UpdateTeamLogoRequest
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
