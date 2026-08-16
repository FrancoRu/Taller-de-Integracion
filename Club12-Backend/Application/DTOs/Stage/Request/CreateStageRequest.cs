
using System;
using System.ComponentModel.DataAnnotations;
using Application.Utils.Constants.Stage;
using Domain.Enums;

namespace Application.DTOs.Stage.Request;

/// <summary>
/// Represents the payload for creating a new stage in a tournament.
/// </summary>
public class CreateStageRequest
{
    /// <summary>
    /// The name of the stage (e.g., "Group A", "Quarterfinals").
    /// </summary>
    [Required(ErrorMessage = "Stage name field is required.")]
    public required string Name { get; set; }

    /// <summary>
    /// Optional description providing additional details about the stage.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of the stage, such as "Group", "Elimination", etc.
    /// Should match an enum value on the service side.
    /// </summary>
    [AllowedValues(StageType.Group, StageType.RoundOf16, StageType.QuarterFinal, StageType.SemiFinal, StageType.ThirdPlace, StageType.Final)]
    public required StageType StageType { get; set; } = StageType.Group;

    /// <summary>
    /// Indicates whether the stage is currently active.
    /// If null, the default should be true.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Indicates whether this is an elimination stage.
    /// If null, the default should be false.
    /// </summary>
    public bool? IsElimination { get; set; }

    /// <summary>
    /// The starting date of the stage.
    /// </summary>
    [Required(ErrorMessage = "Start date field is required.")]
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// The ending date of the stage.
    /// </summary>
    [Required(ErrorMessage = "End date field is required.")]
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// The ID of the division to which this stage belongs.
    /// </summary>
    [Required(ErrorMessage = "Division ID field is required.")]
    public required Guid DivisionId { get; set; }

    /// <summary>
    /// Optional label grouping this stage with other parallel elimination
    /// brackets in the same division (e.g. "Copa de Oro", "Copa de Plata").
    /// Null means the stage belongs to the division's single/default bracket.
    /// </summary>
    public string? BracketName { get; set; }

    /// <summary>
    /// Number of games in a series between two teams at this round: 1, 3,
    /// 5, or 7. Defaults to 1 (a single match decides the round).
    /// </summary>
    [AllowedValues(1, 3, 5, 7)]
    public int BestOf { get; set; } = 1;

    /// <summary>
    /// How many times each pair of teams plays within this group stage
    /// (1 = single round-robin, 2 = double, ...). Only meaningful for a
    /// Group stage.
    /// </summary>
    [Range(RoundRobinFormat.MIN_LEGS, RoundRobinFormat.MAX_LEGS)]
    public int RoundRobinLegs { get; set; } = RoundRobinFormat.MIN_LEGS;
}
