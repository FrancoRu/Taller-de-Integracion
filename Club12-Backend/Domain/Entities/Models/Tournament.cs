using Domain.Enums;

using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

/// <summary>
/// Represents a tournament in the Club12 application.
/// </summary>
public class Tournament : EntityBase
{
    /// <summary>
    /// The description of the tournament.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The name of the tournament.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public tournament links.
    /// Generated once from the name at creation time and never changed afterward,
    /// so shared links keep working even if the tournament is renamed.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// The deadline for team registrations.
    /// Must be earlier than the tournament start date.
    /// </summary>
    public required DateTime TeamRegistrationDeadline { get; set; }

    /// <summary>
    /// The start date of the tournament.
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Current lifecycle status of the tournament.
    /// </summary>
    public TournamentStatus Status { get; set; } = TournamentStatus.Scheduled;

    /// <summary>
    /// The divisions associated with the tournament.
    /// </summary>
    public virtual required ICollection<Division> Divisions { get; set; }

    /// <summary>
    /// The teams registered in the tournament.
    /// </summary>
    public virtual required ICollection<Team> Teams { get; set; }
}
