using Domain.Enums;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Divisions.Request;

/// <summary>
/// Represents a request to create a division.
/// </summary>
public class CreateDivisionRequest
{
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "The TournamentId field is required.")]
    public required Guid TournamentId { get; set; }

    /// <summary>
    /// Marks this division as a cross-division cup that intentionally draws teams from every division.
    /// </summary>
    public bool IsCrossDivisionCup { get; set; } = false;

    /// <summary>
    /// Points awarded for a win in this division's standings; defaults to 2.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "PointsForWin cannot be negative.")]
    public int PointsForWin { get; set; } = 2;

    /// <summary>
    /// Points awarded for a loss in this division's standings; defaults to 1.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "PointsForLoss cannot be negative.")]
    public int PointsForLoss { get; set; } = 1;

    /// <summary>
    /// How many teams qualify from each internal group of a multi-group cross-division cup; defaults to 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "QualifiersPerGroup must be at least 1.")]
    public int QualifiersPerGroup { get; set; } = 1;

    /// <summary>
    /// Competitive category of the division, which must match the parent tournament's category.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    /// <summary>
    /// Optional position-range to playoff-destination mapping the wizard sends to seed multiple cups.
    /// </summary>
    public List<PlayoffMappingRequest>? PlayoffMappings { get; set; }
}
