using Domain.Enums;

using System;
using System.ComponentModel.DataAnnotations;


namespace Application.DTOs.Match.Request;

/// <summary>
/// Represents a request to create a match.
/// </summary>
public class CreateMatchRequest
{
    [Required(ErrorMessage = "The MatchDate field is required.")]
    public required DateTime MatchDate { get; set; }

    [AllowedValues(MatchType.Regular, MatchType.Playoff)]
    public MatchType? Type { get; set; } = MatchType.Regular;

    [Required(ErrorMessage = "The HomeTeamId field is required.")]
    public required Guid HomeTeamId { get; set; }

    [Required(ErrorMessage = "The VisitorTeamId field is required.")]
    public required Guid VisitorTeamId { get; set; }

    [Required(ErrorMessage = "The DivisionId field is required.")]
    public required Guid StageId { get; set; }

    public Guid? VenueId { get; set; }
}
