using Application.DTOs.Divisions.Request;

using Domain.Enums;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Tournament.Request;

/// <summary>
/// One division within a CreateFullTournamentRequest; the TournamentId is implied by nesting.
/// </summary>
public class CreateFullDivisionRequest
{
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; set; }

    /// <summary>
    /// Marks a cross-division cup that draws teams from every zone.
    /// </summary>
    public bool IsCrossDivisionCup { get; set; } = false;

    [Range(0, int.MaxValue, ErrorMessage = "PointsForWin cannot be negative.")]
    public int PointsForWin { get; set; } = 2;

    [Range(0, int.MaxValue, ErrorMessage = "PointsForLoss cannot be negative.")]
    public int PointsForLoss { get; set; } = 1;

    /// <summary>
    /// How many teams qualify from each internal group of a multi-group cross-division cup; defaults to 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "QualifiersPerGroup must be at least 1.")]
    public int QualifiersPerGroup { get; set; } = 1;

    /// <summary>
    /// Competitive category of the division; a mismatch with the parent tournament aborts the whole create.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    /// <summary>
    /// Optional position-range to playoff-destination mappings.
    /// </summary>
    public List<PlayoffMappingRequest>? PlayoffMappings { get; set; }

    /// <summary>
    /// The stages, group and cup elimination rounds alike, to create in this division.
    /// </summary>
    public List<CreateFullStageRequest> Stages { get; set; } = [];
}
