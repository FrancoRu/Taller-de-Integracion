using Application.DTOs.Abstract.Response;
using Domain.Enums;
using System;

namespace Application.DTOs.Stage.Response;

/// <summary>
/// Represents the data returned when querying a stage entity.
/// </summary>
public class StageResponse : BaseEntityResponse
{
    /// <summary>
    /// The start date of the stage.
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// The end date of the stage.
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// The name of the stage.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description providing additional details about the stage.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of the stage as a string (e.g., "Group", "QuarterFinal").
    /// Corresponds to a stage type enum in the service layer.
    /// </summary>
    public required StageType StageType { get; set; }

    /// <summary>
    /// Indicates whether the stage is currently active.
    /// </summary>
    public required bool IsActive { get; set; }

    /// <summary>
    /// Indicates whether the stage is an elimination stage.
    /// </summary>
    public bool IsElimination { get; set; }

    /// <summary>
    /// The unique identifier of the division this stage belongs to.
    /// </summary>
    public required Guid DivisionId { get; set; }

    /// <summary>
    /// The order of the current Stage
    /// </summary>
    public int Order { get; set; }
}
