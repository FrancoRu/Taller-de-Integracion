using Entities.Models.Matches;

using System.ComponentModel.DataAnnotations;


namespace Entities.DTOs.Match;

/// <summary>
/// Represents a request to create a match.
/// </summary>
public class CreateMatchRequest
{
    /// <summary>
    /// The date of the match.
    /// </summary>
    [Required(ErrorMessage = "The MatchDate field is required.")]
    public required DateTime MatchDate { get; set; }

    /// <summary>
    /// The type of the match (e.g., regular or playoff).
    /// </summary>
    [AllowedValues(Models.Matches.MatchType.Regular, Models.Matches.MatchType.Playoff)]
    public Models.Matches.MatchType? Type { get; set; } = Models.Matches.MatchType.Regular;

    /// <summary>
    /// The unique identifier of the home team.
    /// </summary>
    [Required(ErrorMessage = "The HomeTeamId field is required.")]
    public required Guid HomeTeamId { get; set; }

    /// <summary>
    /// The unique identifier of the visitor team.
    /// </summary>
    [Required(ErrorMessage = "The VisitorTeamId field is required.")]
    public required Guid VisitorTeamId { get; set; }

    /// <summary>
    /// The unique identifier of the stage to which the match belongs.
    /// </summary>
    [Required(ErrorMessage = "The DivisionId field is required.")]
    public required Guid StageId { get; set; }

    /// <summary>
    /// The unique identifier of the venue where the match will be played.
    /// </summary>    
    public Guid? VenueId { get; set; }
}
