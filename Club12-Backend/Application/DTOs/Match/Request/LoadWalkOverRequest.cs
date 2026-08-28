using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Match.Request;

/// <summary>
/// Request to mark a match as a walkover (HU-73). The present team is awarded
/// the regulation default result; the absent team gets zero.
/// </summary>
public class LoadWalkOverRequest
{
    /// <summary>
    /// The team that showed up (the walkover winner). Must be one of the
    /// match's two teams.
    /// </summary>
    [Required]
    public required Guid PresentTeamId { get; set; }

    /// <summary>
    /// Optional override for the present team's awarded score. When omitted,
    /// the regulation default is used.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "The PresentTeamScore must be a non-negative number.")]
    public int? PresentTeamScore { get; set; }
}
