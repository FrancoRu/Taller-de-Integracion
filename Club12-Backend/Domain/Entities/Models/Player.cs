using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Player : EntityBase
{
    public required string FirstName { get; set; }
    public string? SecondName { get; set; }
    public required string LastName { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public player links.
    /// Generated once from the player's full name at creation time and never
    /// changed afterward, so shared links keep working even if the player is
    /// renamed. Duplicate names are disambiguated with a numeric suffix.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>Computed — not persisted to the database.</summary>
    public string FullName => string.Concat(
        LastName.ToUpper(),
        string.IsNullOrWhiteSpace(SecondName) ? $" {FirstName}" : $" {FirstName} {SecondName}"
    );

    public required string DocumentNumber { get; set; }
    public required bool IsSanctioned { get; set; } = false;
    public string? PhoneNumber { get; set; }
    public required DateTime BirthDate { get; set; }
    public required string SocialSecurity { get; set; }

    /// <summary>
    /// Denormalized convenience pointer to the player's CURRENT team, always
    /// kept in sync with their latest <see cref="PlayerTeamRegistration"/>.
    /// This is NOT the source of truth for season-scoped roster membership —
    /// use <see cref="PlayerTeamRegistrations"/> (filtered by TournamentId)
    /// for "was this player on team X during season Y" questions.
    /// </summary>
    public required Team Team { get; set; }
    public Guid TeamId { get; set; }

    public virtual ICollection<Scorer> Scorers { get; set; } = [];

    /// <summary>
    /// Every season this player was registered to a team. The source of
    /// truth for roster membership — see <see cref="PlayerTeamRegistration"/>.
    /// </summary>
    public virtual ICollection<PlayerTeamRegistration> PlayerTeamRegistrations { get; set; } = [];
}