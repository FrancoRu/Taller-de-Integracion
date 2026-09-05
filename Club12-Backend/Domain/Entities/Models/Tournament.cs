using Domain.Enums;

using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Tournament : EntityBase
{
    public required string Description { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public tournament links, generated once from the name and never changed afterward.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// The deadline for team registrations, which must be earlier than the tournament start date.
    /// </summary>
    public required DateTime TeamRegistrationDeadline { get; set; }

    public required DateTime StartDate { get; set; }

    public TournamentStatus Status { get; set; } = TournamentStatus.Scheduled;

    /// <summary>
    /// Competitive gender category of the tournament, the source of truth for the no-mixing invariant across its divisions.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    /// <summary>
    /// The optional id of the season this tournament belongs to, purely additive and never affecting the tournament's own Category.
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// The season this tournament belongs to, or null when it is not grouped under any season.
    /// </summary>
    public virtual Season? Season { get; set; }

    public virtual required ICollection<Division> Divisions { get; set; }

    public virtual required ICollection<Team> Teams { get; set; }
}
