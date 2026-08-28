using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Divisions.Request;

/// <summary>
/// One position-range → playoff-destination entry the tournament wizard
/// sends for a division (HU-45), e.g. { From: 1, To: 4, Destination: "Copa
/// Oro" }. Ranges within a division must not overlap.
/// </summary>
public class PlayoffMappingRequest
{
    /// <summary>First standings position in the range (1-based, inclusive).</summary>
    [Range(1, int.MaxValue, ErrorMessage = "FromPosition must be 1 or greater.")]
    public required int FromPosition { get; set; }

    /// <summary>Last standings position in the range (1-based, inclusive).</summary>
    [Range(1, int.MaxValue, ErrorMessage = "ToPosition must be 1 or greater.")]
    public required int ToPosition { get; set; }

    /// <summary>
    /// The playoff destination (cup) the teams in this range qualify for,
    /// matching the elimination stages' BracketName (e.g. "Copa Oro").
    /// </summary>
    [Required(ErrorMessage = "The Destination field is required.")]
    public required string Destination { get; set; }
}
