using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models;

/// <summary>
/// Represents a tournament in the Club12 application.
/// </summary>
[Table("Tournaments", Schema = "Club12")]
public class Tournament : EntityBase
{
    /// <summary>
    /// The description of the tournament.
    /// </summary>
    [Required]
    public required string Description { get; set; }

    /// <summary>
    /// The name of the tournament.
    /// </summary>
    [Required]
    public required string Name { get; set; }

    /// <summary>
    /// The deadline for team registrations.
    /// Must be earlier than the tournament start date.
    /// </summary>
    [Required]
    public required DateTime TeamRegistrationDeadline { get; set; }

    /// <summary>
    /// The start date of the tournament.
    /// </summary>
    [Required]
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// The maximum number of teams allowed to participate in the tournament.
    /// </summary>
    [Required]
    public required int MaxTeams { get; set; }

    /// <summary>
    /// The minimum number of teams required to hold the tournament.
    /// </summary>
    [Required]
    public required int MinTeams { get; set; }

    /// <summary>
    /// Indicates whether the tournament has finished.
    /// </summary>
    [DefaultValue(false)]
    public bool IsFinished { get; set; } = false;

    /// <summary>
    /// The divisions associated with the tournament.
    /// </summary>
    [InverseProperty(nameof(Division.Tournament))]
    public virtual required ICollection<Division> Divisions { get; set; }

    /// <summary>
    /// The teams registered in the tournament.
    /// </summary>
    [InverseProperty(nameof(Team.Tournament))]
    public virtual required ICollection<Team> Teams { get; set; }
}
