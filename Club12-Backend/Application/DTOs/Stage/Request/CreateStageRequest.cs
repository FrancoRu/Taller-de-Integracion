
using Application.Utils.Constants.Stage;

using Domain.Enums;

using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Stage.Request;

/// <summary>
/// Represents the payload for creating a new stage in a tournament.
/// </summary>
public class CreateStageRequest
{
    /// <summary>
    /// The name of the stage.
    /// </summary>
    [Required(ErrorMessage = "Stage name field is required.")]
    public required string Name { get; set; }

    public string? Description { get; set; }

    [AllowedValues(StageType.Group, StageType.RoundOf16, StageType.QuarterFinal, StageType.SemiFinal, StageType.ThirdPlace, StageType.Final)]
    public required StageType StageType { get; set; } = StageType.Group;

    /// <summary>
    /// Whether the stage is currently active; null defaults to true.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Whether this is an elimination stage; null defaults to false.
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
    /// Optional label grouping this stage with parallel elimination brackets; null uses the default bracket.
    /// </summary>
    public string? BracketName { get; set; }

    /// <summary>
    /// Number of games in a series between two teams at this round, one of 1, 3, 5, or 7; defaults to 1.
    /// </summary>
    [AllowedValues(1, 3, 5, 7)]
    public int BestOf { get; set; } = 1;

    /// <summary>
    /// How many times each pair of teams plays within this group stage; only meaningful for a Group stage.
    /// </summary>
    [Range(RoundRobinFormat.MIN_LEGS, RoundRobinFormat.MAX_LEGS)]
    public int RoundRobinLegs { get; set; } = RoundRobinFormat.MIN_LEGS;
}
