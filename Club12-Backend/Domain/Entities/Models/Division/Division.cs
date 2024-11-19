using Entities.Models.MatchEntity;
using Entities.Models.PositionModel;
using Entities.Models.TeamEntity;
using Entities.Models.TournamentEntity;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.DivisionEntity;

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
    public required bool IsFinished { get; set; } = false;

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
    /// The list of teams in this division.
    /// </summary>
    public virtual required ICollection<Team> Teams { get; set; }

    /// <summary>
    /// The list of matches in this division.
    /// </summary>
    public virtual required ICollection<Match> Matches { get; set; }

    /// <summary>
    /// The positions of teams in the division.
    /// </summary>
    [NotMapped]
    public required IEnumerable<Position> Positions { get; set; } = [];
}
