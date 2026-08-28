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
    /// <summary>
    /// The name of the division.
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; set; }

    /// <summary>
    /// The Id of the tournament this division belongs to.
    /// </summary>
    [Required(ErrorMessage = "The TournamentId field is required.")]
    public required Guid TournamentId { get; set; }

    /// <summary>
    /// Marks this division as a cross-division cup (e.g. "Copa Club12")
    /// that intentionally draws teams from every other division in the
    /// tournament, rather than being a team's single competitive tier.
    /// Defaults to false.
    /// </summary>
    public bool IsCrossDivisionCup { get; set; } = false;

    /// <summary>
    /// Points awarded for a win in this division's standings (HU-79).
    /// Defaults to 2.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "PointsForWin cannot be negative.")]
    public int PointsForWin { get; set; } = 2;

    /// <summary>
    /// Points awarded for a loss in this division's standings (HU-79).
    /// Defaults to 1.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "PointsForLoss cannot be negative.")]
    public int PointsForLoss { get; set; } = 1;

    /// <summary>
    /// How many teams qualify to the bracket from EACH internal group of a
    /// multi-group cross-division cup (HU-110). Only meaningful when
    /// <see cref="IsCrossDivisionCup"/> is true and the division holds more
    /// than one Group stage; harmless default 1 everywhere else.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "QualifiersPerGroup must be at least 1.")]
    public int QualifiersPerGroup { get; set; } = 1;

    /// <summary>
    /// Competitive category (gender) of the division (HU-48). Must match the
    /// parent tournament's category — a single tournament cannot mix feminine
    /// and masculine divisions, so a mismatch is rejected. Defaults to
    /// <see cref="TournamentCategory.Masculine"/> when omitted.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    /// <summary>
    /// Optional position-range → playoff-destination mapping (HU-45) the
    /// wizard sends so the system can seed multiple cups (HU-81). Ranges must
    /// not overlap.
    /// </summary>
    public List<PlayoffMappingRequest>? PlayoffMappings { get; set; }
}
