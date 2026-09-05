using Domain.Enums;

using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Division : EntityBase
{
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public division links, generated once from the name and never changed afterward.
    /// </summary>
    public required string Slug { get; set; }

    public bool IsFinished { get; set; } = false;

    public required Tournament Tournament { get; set; }

    public Guid TournamentId { get; set; }

    /// <summary>
    /// Competitive gender category of the division, which must match the parent Tournament's Category.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    public virtual required ICollection<Stage> Stages { get; set; }

    /// <summary>
    /// Marks a division that intentionally draws teams from across every other division in the tournament, rather than a team's single competitive tier.
    /// </summary>
    public bool IsCrossDivisionCup { get; set; } = false;

    /// <summary>
    /// Points awarded for a win when building this division's standings, configurable per division. Defaults to 2.
    /// </summary>
    public int PointsForWin { get; set; } = 2;

    /// <summary>
    /// Points awarded for a loss when building this division's standings. Defaults to 1.
    /// </summary>
    public int PointsForLoss { get; set; } = 1;

    /// <summary>
    /// How many teams qualify to the bracket from each internal group of a multi-group cross-division cup. Defaults to 1.
    /// </summary>
    public int QualifiersPerGroup { get; set; } = 1;

    /// <summary>
    /// Maps this division's final standings position ranges to playoff destinations.
    /// </summary>
    public virtual ICollection<DivisionPlayoffMapping> PlayoffMappings { get; set; } = [];
}
