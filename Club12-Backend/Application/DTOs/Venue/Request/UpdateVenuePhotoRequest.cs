using Microsoft.AspNetCore.Http;

using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Venue.Request;

/// <summary>
/// Represents a request to update a venue's photo.
/// </summary>
public class UpdateVenuePhotoRequest
{
    [Required(ErrorMessage = "The VenueId field is required.")]
    public required Guid VenueId { get; init; }

    /// <summary>
    /// The photo image file to upload, which must be JPEG or PNG.
    /// </summary>
    [Required(ErrorMessage = "The image file is required.")]
    [DataType(DataType.Upload)]
    public required IFormFile ImageFile { get; init; }
}
