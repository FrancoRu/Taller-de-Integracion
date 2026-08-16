using Application.Utils.Constants.Validation;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Venue.Request;

/// <summary>
/// Request model for creating a new venue.
/// </summary>
public class CreateVenueRequest
{
    /// <summary>
    /// The name of the venue.
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    [MaxLength(VenueFieldLengths.NameMaxLength)]
    public required string Name { get; set; }

    /// <summary>
    /// The address of the venue.
    /// </summary>
    [Required(ErrorMessage = "The Address field is required.")]
    [MaxLength(VenueFieldLengths.AddressMaxLength)]
    public required string Address { get; set; }

    /// <summary>
    /// The logo image file to upload (must be JPEG or PNG).
    /// </summary>
    [Required(ErrorMessage = "The image file image is required.")]
    [DataType(DataType.Upload)]
    public required IFormFile ImageFile { get; init; }
}