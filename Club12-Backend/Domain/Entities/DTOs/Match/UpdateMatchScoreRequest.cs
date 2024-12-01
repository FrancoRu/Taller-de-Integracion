using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Match;

/// <summary>
/// Represents a request to update the score of a match.
/// </summary>
public class UpdateMatchScoreRequest
{
    /// <summary>
    /// Home team score
    /// </summary>
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "The HomeScore must be a non-negative number.")]
    public required int HomeScore { get; set; }

    /// <summary>
    /// Visitor team score
    /// </summary>
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "The VisitorScore must be a non-negative number.")]
    public required int VisitorScore { get; set; }

    /// <summary>
    /// Home team players and their corresponding scores
    /// </summary>
    [Required]
    public required List<PlayerScoreRequest> HomeTeamPlayerScores { get; set; }

    /// <summary>
    /// Visitor team players and their corresponding scores
    /// </summary>
    [Required]
    public required List<PlayerScoreRequest> VisitorTeamPlayerScores { get; set; }
}
