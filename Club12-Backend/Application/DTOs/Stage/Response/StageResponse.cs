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
    /// The unique, URL-friendly identifier used in public stage links.
    /// </summary>
    public required string Slug { get; set; }

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

    /// <summary>
    /// Optional label grouping this stage with other parallel elimination
    /// brackets in the same division (e.g. "Copa de Oro", "Copa de Plata").
    /// Null means the stage belongs to the division's single/default bracket.
    /// </summary>
    public string? BracketName { get; set; }

    /// <summary>
    /// Number of games in a series between two teams at this round: 1, 3,
    /// 5, or 7. 1 means a single match decides the round.
    /// </summary>
    public int BestOf { get; set; }

    /// <summary>
    /// How many times each pair of teams plays within this group stage
    /// (1 = single round-robin, 2 = double, ...).
    /// </summary>
    public int RoundRobinLegs { get; set; }
}
