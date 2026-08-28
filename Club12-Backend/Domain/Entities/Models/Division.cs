using Domain.Enums;

using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

/// <summary>
/// Represents a division in the Club12 application.
/// </summary>
public class Division : EntityBase
{
    /// <summary>
    /// The name of the Divisions.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public division links.
    /// Generated once from the name at creation time and never changed
    /// afterward, so shared links keep working even if the division is
    /// renamed.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// If the division is finished.
    /// </summary>
    public bool IsFinished { get; set; } = false;

    /// <summary>
    /// The tournament this division belongs to.
    /// </summary>
    public required Tournament Tournament { get; set; }

    /// <summary>
    /// The Id of the tournament this division belongs to.
    /// </summary>
    public Guid TournamentId { get; set; }

    /// <summary>
    /// Competitive category (gender) of the division (HU-48). Must match the
    /// parent <see cref="Tournament"/>'s <see cref="Tournament.Category"/>: a
    /// single tournament can never mix feminine and masculine divisions. The
    /// invariant is enforced when a division is created or updated. Defaults to
    /// <see cref="TournamentCategory.Masculine"/>.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    /// <summary>
    /// The list of Stages in this division.
    /// </summary>
    public virtual required ICollection<Stage> Stages { get; set; }

    /// <summary>
    /// Marks a division that intentionally draws teams from across every
    /// other division in the tournament (e.g. "Copa Club12"), rather than
    /// a team's single competitive tier. Team-assignment consistency
    /// checks exempt cross-division-cup divisions from the "one team, one
    /// division" rule so the same team can belong to its zone AND the cup.
    /// </summary>
    public bool IsCrossDivisionCup { get; set; } = false;

    /// <summary>
    /// Points awarded for a win when building this division's standings
    /// (HU-79). Configurable per division so the scoring rule sits in the
    /// same aggregate that owns the standings and the playoff mapping.
    /// Defaults to 2 (FIBA-style: 2 for a win, 1 for a loss).
    /// </summary>
    public int PointsForWin { get; set; } = 2;

    /// <summary>
    /// Points awarded for a loss when building this division's standings
    /// (HU-79). Basketball has no draws, so every finished match awards
    /// <see cref="PointsForWin"/> to one team and this to the other.
    /// Defaults to 1.
    /// </summary>
    public int PointsForLoss { get; set; } = 1;

    /// <summary>
    /// Maps this division's final standings position ranges to playoff
    /// destinations (HU-45), e.g. 1-4 → "Copa Oro", 5-8 → "Copa Plata".
    /// Positions not covered by any range do not qualify for a playoff.
    /// </summary>
    public virtual ICollection<DivisionPlayoffMapping> PlayoffMappings { get; set; } = [];
}
