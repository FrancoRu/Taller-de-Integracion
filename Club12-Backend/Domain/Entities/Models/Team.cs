using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models;

/// <summary>
/// Represents a team in the Club12 application.
/// </summary>
[Table("Teams", Schema = "Club12")]
public class Team : EntityBase
{
    /// <summary>
    /// The name of the team.
    /// </summary>
    [Required]
    public required string Name { get; set; }

    /// <summary>
    /// The three-letter code of the team.
    /// </summary>
    [Required]
    public required string ThreeLetterCode { get; set; }

    /// <summary>
    /// The URL of the team's logo.
    /// </summary>
    public required string LogoUrl { get; set; }

    /// <summary>
    /// The color of the team's shirt.
    /// </summary>
    [Required]
    public required string ShirtColor { get; set; }

    [Column(nameof(TournamentId))]
    public Tournament? Tournament { get; set; }

    public Guid? TournamentId { get; set; }

    /// <summary>
    /// The players belonging to the team.
    /// </summary>
    public virtual required ICollection<Player> Players { get; set; }

    /// <summary>
    /// The staff belonging to the team.
    /// </summary>
    public virtual required ICollection<Staff> Staff { get; set; }


    /// <summary>
    /// The collection of <see cref="StageTeamMatch"/> entities that represent the association between this team and the stages they participate in within matches.
    /// </summary>

    [InverseProperty(nameof(StageTeamMatch.Team))]
    public virtual ICollection<StageTeamMatch> StageTeamMatches { get; set; } = [];
}
