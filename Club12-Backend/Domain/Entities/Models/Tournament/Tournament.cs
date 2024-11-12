using Entities.Models.DivisionEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.TournamentEntity;

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
    /// The divisions associated with the tournament.
    /// </summary>
    public virtual required ICollection<Division> Divisions { get; set; }
}
