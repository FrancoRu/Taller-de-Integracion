using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models;

/// <summary>
/// Represents a stage within a tournament or league, such as group stage, knockout rounds, etc.
/// </summary>
[Table("Stages", Schema = "Club12")]
public class Stage : EntityBase
{
    /// <summary>
    /// The name of the stage (e.g., "Group A", "Quarterfinals").
    /// </summary>
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// A detailed description of the stage and its purpose or rules.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// The type of the stage (e.g., "Group", "RoundOf16", "Final").
    /// Typically corresponds to a value from the <see cref="StageType"/> enum.
    /// </summary>
    public required StageType StageType { get; set; }

    /// <summary>
    /// Indicates whether the stage is currently active.
    /// </summary>
    [DefaultValue(true)]
    public required bool IsActive { get; set; }

    /// <summary>
    /// Indicates whether this stage follows an elimination format (e.g., single elimination).
    /// </summary>
    [DefaultValue(false)]
    public bool IsElimination { get; set; }

    /// <summary>
    /// The starting date of the stage.
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// The ending date of the stage.
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Foreign key to the division this stage belongs to.
    /// </summary>
    public required Guid DivisionId { get; set; }

    /// <summary>
    /// Navigation property to the related division.
    /// </summary>
    [ForeignKey(nameof(DivisionId))]
    public required Division Division { get; set; }

    /// <summary>
    /// The collection of matches associated with this stage.
    /// </summary>
    public virtual required ICollection<Match> Matches { get; set; } = [];

    /// <summary>
    /// The order number of the current stage
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// The collection of stage-team-match associations that link this stage to the teams participating in it within the context of matches.
    /// </summary>

    [InverseProperty(nameof(StageTeamMatch.Stage))]
    public virtual ICollection<StageTeamMatch> StageTeamMatches { get; set; } = [];
}

