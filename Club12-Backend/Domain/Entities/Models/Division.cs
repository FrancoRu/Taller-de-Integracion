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
}
