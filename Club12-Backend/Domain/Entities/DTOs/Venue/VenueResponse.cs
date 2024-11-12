using Entities.DTOs.Abstract;

namespace Entities.DTOs.Venue;

/// <summary>
/// Response model for returning venue details.
/// </summary>
public class VenueResponse : BaseEntityResponse
{

    /// <summary>
    /// The name of the venue.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The address of the venue.
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// The URL of the venue's photo.
    /// </summary>
    public string? PhotoUrl { get; set; }
}
