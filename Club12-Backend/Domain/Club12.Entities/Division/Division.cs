using Club12.Entities.TeamEntity;
using Club12.Entities.TournamentEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.DivisionEntity;

/// <summary>
/// Represents a division in the Club12 application.
/// </summary>
[Table("Divisions", Schema = "Club12")]
public class Division : EntityBase
{
    /// <summary>
    /// The name of the Division.
    /// </summary>
    [Required]
    [MaxLength(30)]
    public required string Name { get; set; }

    /// <summary>
    /// The tournament this division belongs to.
    /// </summary>
    [Column("TournamentId")]
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
}
