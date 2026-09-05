using Application.Utils.Constants.Validation;

using Microsoft.AspNetCore.Http;

using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Venue.Request;

/// <summary>
/// Request model for creating a new venue.
/// </summary>
public class CreateVenueRequest
{
    [Required(ErrorMessage = "The Name field is required.")]
    [MaxLength(VenueFieldLengths.NameMaxLength)]
    public required string Name { get; set; }

    [Required(ErrorMessage = "The Address field is required.")]
    [MaxLength(VenueFieldLengths.AddressMaxLength)]
    public required string Address { get; set; }

    /// <summary>
    /// Optional photo of the venue (JPEG or PNG). A venue does not require a
    /// photo, so this may be omitted.
    /// </summary>
    [DataType(DataType.Upload)]
    public IFormFile? ImageFile { get; init; }

    /// <summary>
    /// Optional geographic latitude of the venue (e.g. pasted from Google Maps).
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Optional geographic longitude of the venue (e.g. pasted from Google Maps).
    /// </summary>
    public double? Longitude { get; set; }
}