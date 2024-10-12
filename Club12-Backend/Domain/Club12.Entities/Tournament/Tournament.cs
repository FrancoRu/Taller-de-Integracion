using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.TournamentEntity;

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
    /// The year of the tournament.
    /// </summary>
    [Required]
    public required int Year { get; set; }
}
