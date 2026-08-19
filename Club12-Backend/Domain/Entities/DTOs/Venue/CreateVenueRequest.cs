using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Venue;

/// <summary>
/// Request model for creating a new venue.
/// </summary>
public class CreateVenueRequest
{
    /// <summary>
    /// The name of the venue.
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    [MaxLength(50)]
    public required string Name { get; set; }

    /// <summary>
    /// The address of the venue.
    /// </summary>
    [Required(ErrorMessage = "The Address field is required.")]
    [MaxLength(200)]
    public required string Address { get; set; }

    /// <summary>
    /// The URL of the venue's photo.
    /// </summary>
    [Url]
    public string? PhotoUrl { get; set; }
}