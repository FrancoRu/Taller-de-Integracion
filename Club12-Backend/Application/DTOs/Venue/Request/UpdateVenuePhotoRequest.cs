using Microsoft.AspNetCore.Http;

using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Venue.Request;

/// <summary>
/// Represents a request to update a venue's photo.
/// </summary>
public class UpdateVenuePhotoRequest
{
    /// <summary>
    /// The unique identifier of the venue whose photo is being updated.
    /// </summary>
    [Required(ErrorMessage = "The VenueId field is required.")]
    public required Guid VenueId { get; init; }

    /// <summary>
    /// The photo image file to upload (must be JPEG or PNG).
    /// </summary>
    [Required(ErrorMessage = "The image file is required.")]
    [DataType(DataType.Upload)]
    public required IFormFile ImageFile { get; init; }
}
