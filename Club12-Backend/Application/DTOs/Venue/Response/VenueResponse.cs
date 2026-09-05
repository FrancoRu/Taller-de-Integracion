using Application.DTOs.Abstract.Response;
namespace Application.DTOs.Venue.Response;

/// <summary>
/// Response model for returning venue details.
/// </summary>
public class VenueResponse : BaseEntityResponse
{

    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public venue links.
    /// </summary>
    public required string Slug { get; set; }

    public required string Address { get; set; }

    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Optional geographic latitude of the venue, for the public map link.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Optional geographic longitude of the venue, for the public map link.
    /// </summary>
    public double? Longitude { get; set; }
}
