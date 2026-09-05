using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Match.Request;

/// <summary>
/// Represents a request to update the score of a match.
/// </summary>
public class UpdateMatchScoreRequest
{
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "The HomeScore must be a non-negative number.")]
    public required int HomeScore { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "The VisitorScore must be a non-negative number.")]
    public required int VisitorScore { get; set; }
}
