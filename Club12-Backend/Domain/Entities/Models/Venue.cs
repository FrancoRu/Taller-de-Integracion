namespace Domain.Entities.Models;

public class Venue : EntityBase
{
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public venue links, generated once from the name and never changed afterward.
    /// </summary>
    public required string Slug { get; set; }

    public required string Address { get; set; }
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Optional geographic latitude of the venue, used to build a public map link, null when the venue has no coordinates yet.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Optional geographic longitude of the venue, used to build a public map link, null when the venue has no coordinates yet.
    /// </summary>
    public double? Longitude { get; set; }
}