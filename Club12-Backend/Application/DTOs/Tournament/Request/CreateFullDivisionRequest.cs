using Application.DTOs.Divisions.Request;

using Domain.Enums;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Tournament.Request;

/// <summary>
/// HU-38: one division within a <see cref="CreateFullTournamentRequest"/>. The
/// TournamentId is implied by nesting, so it is not carried here.
/// </summary>
public class CreateFullDivisionRequest
{
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; set; }

    /// <summary>Marks a cross-division cup that draws teams from every zone.</summary>
    public bool IsCrossDivisionCup { get; set; } = false;

    [Range(0, int.MaxValue, ErrorMessage = "PointsForWin cannot be negative.")]
    public int PointsForWin { get; set; } = 2;

    [Range(0, int.MaxValue, ErrorMessage = "PointsForLoss cannot be negative.")]
    public int PointsForLoss { get; set; } = 1;

    /// <summary>
    /// Competitive category (gender) of the division (HU-48). Must match the
    /// parent tournament's category — a mismatch aborts the whole atomic create.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    /// <summary>Optional position-range → playoff-destination mappings (HU-45).</summary>
    public List<PlayoffMappingRequest>? PlayoffMappings { get; set; }

    /// <summary>The stages (group + cup elimination rounds) to create in this division.</summary>
    public List<CreateFullStageRequest> Stages { get; set; } = [];
}
