namespace Domain.Entities.Models;

public class Venue : EntityBase
{
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public venue links.
    /// Generated once from the name at creation time and never changed
    /// afterward, so shared links keep working even if the venue is renamed.
    /// </summary>
    public required string Slug { get; set; }

    public required string Address { get; set; }
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Optional geographic latitude of the venue, used to build a public map
    /// link. Null when the venue has no coordinates yet.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Optional geographic longitude of the venue, used to build a public map
    /// link. Null when the venue has no coordinates yet.
    /// </summary>
    public double? Longitude { get; set; }
}