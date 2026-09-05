using Domain.Enums;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models;

public class Player : EntityBase
{
    public required string FirstName { get; set; }
    public string? SecondName { get; set; }
    public required string LastName { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public player links, generated once from the player's full name and never changed afterward.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// The single canonical source string every slug is derived from, in the player's raw casing and with no document number.
    /// </summary>
    /// <param name="lastName">The player's last name.</param>
    /// <param name="firstName">The player's first name.</param>
    /// <param name="secondName">The optional second given name; blank is treated as absent.</param>
    /// <returns>The space-joined name source, formed by joining last name, first name, and second name with spaces.</returns>
    public static string BuildSlugSource(string lastName, string firstName, string? secondName)
    {
        if (string.IsNullOrWhiteSpace(secondName))
        {
            return $"{lastName} {firstName}";
        }

        return $"{lastName} {firstName} {secondName}";
    }

    /// <summary>
    /// Computed and not persisted: the canonical slug source for this player.
    /// </summary>
    public string SlugSource => BuildSlugSource(LastName, FirstName, SecondName);

    /// <summary>
    /// Computed and not persisted to the database: display name with the last name upper-cased.
    /// </summary>
    public string FullName => BuildSlugSource(LastName.ToUpper(), FirstName, SecondName);

    public required string DocumentNumber { get; set; }
    public required bool IsSanctioned { get; set; } = false;
    public string? PhoneNumber { get; set; }
    public required DateTime BirthDate { get; set; }
    public required string SocialSecurity { get; set; }

    /// <summary>
    /// Denormalized convenience pointer to the player's current team, always kept in sync with their latest PlayerTeamRegistration.
    /// </summary>
    public required Team Team { get; set; }
    public Guid TeamId { get; set; }

    public virtual ICollection<Scorer> Scorers { get; set; } = [];

    /// <summary>
    /// Every season this player was registered to a team, the source of truth for roster membership.
    /// </summary>
    public virtual ICollection<PlayerTeamRegistration> PlayerTeamRegistrations { get; set; } = [];

    /// <summary>
    /// Transient and not persisted: the medical-record eligibility status of this player for the season roster currently being viewed.
    /// </summary>
    [NotMapped]
    public MedicalRecordStatus? MedicalRecordStatus { get; set; }

    /// <summary>
    /// Transient and not persisted: whether the season roster currently being viewed set a real, non-legacy stored medical-record file reference.
    /// </summary>
    [NotMapped]
    public bool HasMedicalRecordFile { get; set; }

    /// <summary>
    /// Transient and not persisted: whether the player is habilitado for the season roster currently being viewed.
    /// </summary>
    [NotMapped]
    public bool IsHabilitado =>
        MedicalRecordStatus == Domain.Enums.MedicalRecordStatus.Approved && HasMedicalRecordFile;

    /// <summary>
    /// Transient and not persisted: the player's jersey number for the season roster currently being viewed.
    /// </summary>
    [NotMapped]
    public int? JerseyNumber { get; set; }
}