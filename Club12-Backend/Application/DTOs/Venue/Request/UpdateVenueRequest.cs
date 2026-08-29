using Application.Utils.Constants.Validation;

using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Venue.Request;

/// <summary>
/// Request model for updating an existing venue.
/// </summary>
public class UpdateVenueRequest
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
    /// The new URL of the venue's photo.
    /// </summary>
    [Url]
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Optional geographic latitude of the venue (e.g. pasted from Google Maps).
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Optional geographic longitude of the venue (e.g. pasted from Google Maps).
    /// </summary>
    public double? Longitude { get; set; }
}