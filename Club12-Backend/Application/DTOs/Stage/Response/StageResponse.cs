using Application.DTOs.Abstract.Response;

using Domain.Enums;

using System;

namespace Application.DTOs.Stage.Response;

/// <summary>
/// Represents the data returned when querying a stage entity.
/// </summary>
public class StageResponse : BaseEntityResponse
{
    public required DateTime StartDate { get; set; }

    public required DateTime EndDate { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public stage links.
    /// </summary>
    public required string Slug { get; set; }

    public string? Description { get; set; }

    public required StageType StageType { get; set; }

    /// <summary>
    /// Indicates whether the stage is currently active.
    /// </summary>
    public required bool IsActive { get; set; }

    /// <summary>
    /// Indicates whether the stage is an elimination stage.
    /// </summary>
    public bool IsElimination { get; set; }

    public required Guid DivisionId { get; set; }

    public int Order { get; set; }

    /// <summary>
    /// Optional label grouping this stage with parallel elimination brackets; null uses the default bracket.
    /// </summary>
    public string? BracketName { get; set; }

    /// <summary>
    /// Number of games in a series between two teams at this round, one of 1, 3, 5, or 7.
    /// </summary>
    public int BestOf { get; set; }

    /// <summary>
    /// How many times each pair of teams plays within this group stage.
    /// </summary>
    public int RoundRobinLegs { get; set; }
}
