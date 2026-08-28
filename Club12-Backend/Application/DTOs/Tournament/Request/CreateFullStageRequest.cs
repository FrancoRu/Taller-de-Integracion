using Application.Utils.Constants.Stage;

using Domain.Enums;

using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Tournament.Request;

/// <summary>
/// HU-38: one stage within a <see cref="CreateFullDivisionRequest"/>. The
/// DivisionId is implied by nesting. Mirrors the granular CreateStageRequest
/// minus the DivisionId.
/// </summary>
public class CreateFullStageRequest
{
    [Required(ErrorMessage = "Stage name field is required.")]
    public required string Name { get; set; }

    public string? Description { get; set; }

    [AllowedValues(StageType.Group, StageType.RoundOf16, StageType.QuarterFinal, StageType.SemiFinal, StageType.ThirdPlace, StageType.Final)]
    public required StageType StageType { get; set; } = StageType.Group;

    /// <summary>Defaults to true when null.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Defaults to (StageType != Group) when null.</summary>
    public bool? IsElimination { get; set; }

    [Required(ErrorMessage = "Start date field is required.")]
    public required DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date field is required.")]
    public required DateTime EndDate { get; set; }

    /// <summary>Groups parallel elimination brackets under a cup name (e.g. "Copa de Oro").</summary>
    public string? BracketName { get; set; }

    [AllowedValues(1, 3, 5, 7)]
    public int BestOf { get; set; } = 1;

    [Range(RoundRobinFormat.MIN_LEGS, RoundRobinFormat.MAX_LEGS)]
    public int RoundRobinLegs { get; set; } = RoundRobinFormat.MIN_LEGS;
}
