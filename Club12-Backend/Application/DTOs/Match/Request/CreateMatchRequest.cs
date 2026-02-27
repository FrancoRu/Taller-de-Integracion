using Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;


namespace Application.DTOs.Match.Request;

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
    [AllowedValues(MatchType.Regular, MatchType.Playoff)]
    public MatchType? Type { get; set; } = MatchType.Regular;

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
