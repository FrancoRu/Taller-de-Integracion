using Application.Utils.Constants.Validation;
using Application.Utils.Extensions;

using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Venue.Request;

/// <summary>
/// Request model for updating an existing venue.
/// </summary>
public class UpdateVenueRequest
{
    [Required(ErrorMessage = "The Name field is required.")]
    [MaxLength(VenueFieldLengths.NameMaxLength)]
    public required string Name { get; set; }

    [Required(ErrorMessage = "The Address field is required.")]
    [MaxLength(VenueFieldLengths.AddressMaxLength)]
    public required string Address { get; set; }

    [ImageReference]
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Optional geographic latitude of the venue.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Optional geographic longitude of the venue.
    /// </summary>
    public double? Longitude { get; set; }
}