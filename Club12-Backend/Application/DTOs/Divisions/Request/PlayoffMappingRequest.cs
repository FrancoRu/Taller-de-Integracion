using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Divisions.Request;

/// <summary>
/// One position-range to playoff-destination entry the wizard sends for a division; ranges must not overlap.
/// </summary>
public class PlayoffMappingRequest
{
    /// <summary>
    /// First standings position in the range, 1-based and inclusive.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "FromPosition must be 1 or greater.")]
    public required int FromPosition { get; set; }

    /// <summary>
    /// Last standings position in the range, 1-based and inclusive.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "ToPosition must be 1 or greater.")]
    public required int ToPosition { get; set; }

    /// <summary>
    /// The playoff cup the teams in this range qualify for, matching an elimination stage's BracketName.
    /// </summary>
    [Required(ErrorMessage = "The Destination field is required.")]
    public required string Destination { get; set; }
}
