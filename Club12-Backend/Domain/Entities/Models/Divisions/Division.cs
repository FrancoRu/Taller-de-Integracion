using Entities.Models.Matches;
using Entities.Models.Positions;
using Entities.Models.Stages;
using Entities.Models.Teams;
using Entities.Models.Tournaments;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.Divisions;

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
    [Column(nameof(TournamentId))]
    [Required]
    public required Tournament Tournament { get; set; }

    /// <summary>
    /// The Id of the tournament this division belongs to.
    /// </summary>
    public Guid TournamentId { get; set; }

    /// <summary>
    /// Represents if the division has the playoffs generated.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public bool PlayoffsGenerated { get; set; } = false;

    /// <summary>
    /// The list of teams in this division.
    /// </summary>
    public virtual required ICollection<Team> Teams { get; set; }

    /// <summary>
    /// The list of matches in this division.
    /// </summary>
    public virtual required ICollection<Match> Matches { get; set; }

    /// <summary>
    /// The list of Stages in this division.
    /// </summary>
    public virtual required ICollection<Stage> Stages { get; set; }


    /// <summary>
    /// The positions of teams in the division.
    /// </summary>
    [NotMapped]
    public required IEnumerable<Position> Positions { get; set; } = [];
}
