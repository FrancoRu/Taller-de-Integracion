using Domain.Enums;

using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Tournament : EntityBase
{
    public required string Description { get; set; }

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

    public required DateTime StartDate { get; set; }

    public TournamentStatus Status { get; set; } = TournamentStatus.Scheduled;

    /// <summary>
    /// Competitive category (gender) of the tournament (HU-48). By club rule
    /// the feminine competition is a separate tournament, so every division in
    /// this tournament must share this category — it is the source of truth for
    /// the "no mixing feminine and masculine" invariant. Defaults to
    /// <see cref="TournamentCategory.Masculine"/>.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    /// <summary>
    /// The id of the season this tournament belongs to (optional). A tournament
    /// may be grouped under a <see cref="Season"/> ("Temporada") alongside other
    /// tournaments of the same period; belonging to a season is purely additive
    /// and never affects the tournament's own <see cref="Category"/> (HU-48).
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// The season this tournament belongs to, or null when it is not grouped
    /// under any season.
    /// </summary>
    public virtual Season? Season { get; set; }

    public virtual required ICollection<Division> Divisions { get; set; }

    public virtual required ICollection<Team> Teams { get; set; }
}
