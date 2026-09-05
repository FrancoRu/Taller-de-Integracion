using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Match.Request;

/// <summary>
/// Request to mark a match as a walkover, awarding the present team the regulation default result.
/// </summary>
public class LoadWalkOverRequest
{
    /// <summary>
    /// The walkover winner; must be one of the match's two teams.
    /// </summary>
    [Required]
    public required Guid PresentTeamId { get; set; }

    /// <summary>
    /// Optional override for the present team's awarded score; the regulation default is used when omitted.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "The PresentTeamScore must be a non-negative number.")]
    public int? PresentTeamScore { get; set; }
}
