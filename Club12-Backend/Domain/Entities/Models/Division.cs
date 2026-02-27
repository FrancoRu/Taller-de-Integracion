using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models;

/// <summary>
/// Represents a division in the Club12 application.
/// </summary>
[Table("Divisions", Schema = "Club12")]
public class Division : EntityBase
{
    /// <summary>
    /// The name of the Divisions.
    /// </summary>
    [Required]
    [MaxLength(30)]
    public required string Name { get; set; }

    /// <summary>
    /// If the division is finished.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public bool IsFinished { get; set; } = false;

    /// <summary>
    /// The tournament this division belongs to.
    /// </summary>
    [Required]
    [ForeignKey(nameof(TournamentId))]
    public required Tournament Tournament { get; set; }

    /// <summary>
    /// The Id of the tournament this division belongs to.
    /// </summary>
    public Guid TournamentId { get; set; }

    /// <summary>
    /// The list of Stages in this division.
    /// </summary>
    [InverseProperty(nameof(Stage.Division))]
    public virtual required ICollection<Stage> Stages { get; set; }
}
