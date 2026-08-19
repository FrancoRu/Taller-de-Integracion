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
}
