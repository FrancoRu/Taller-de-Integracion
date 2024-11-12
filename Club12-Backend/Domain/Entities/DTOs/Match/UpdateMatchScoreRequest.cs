using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Match;

/// <summary>
/// Represents a request to update the score of a match.
/// </summary>
public class UpdateMatchScoreRequest
{
    /// <summary>
    /// The score of the home team.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "The HomeScore must be a non-negative number.")]
    public int? HomeScore { get; set; }

    /// <summary>
    /// The score of the visitor team.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "The VisitorScore must be a non-negative number.")]
    public int? VisitorScore { get; set; }
}
