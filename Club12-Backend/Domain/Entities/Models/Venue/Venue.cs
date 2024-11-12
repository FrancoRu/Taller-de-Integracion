using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.VenueEntity;

/// <summary>
/// Represents a venue where matches are played in the Club12 application.
/// </summary>
[Table("Venues", Schema = "Club12")]
public class Venue : EntityBase
{
    /// <summary>
    /// The name of the venue.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string Name { get; set; }

    /// <summary>
    /// The address of the venue.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string Address { get; set; }

    /// <summary>
    /// The URL of the venue's photo.
    /// </summary>
    [Url]
    public string? PhotoUrl { get; set; }
}
