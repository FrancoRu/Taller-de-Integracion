using Entities.Models.DivisionEntity;
using Entities.Models.PlayerEntity;
using Entities.Models.StaffEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.TeamEntity;

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
    /// Gets or sets the color of the shirt.
    /// </summary>
    public required string ShirtColor { get; set; }

    /// <summary>
    /// The division that the team belongs to.
    /// </summary>
    [Required]
    [Column("DivisionId")]
    public required Division Division { get; set; }

    /// <summary>
    /// The ID of the division that the team belongs to.
    /// </summary>
    public Guid DivisionId { get; set; }

    /// <summary>
    /// The players belonging to the team.
    /// </summary>
    public virtual required ICollection<Player> Players { get; set; }

    /// <summary>
    /// The collection of staff members associated with the entity.
    /// Limited to a maximum of 3 staff members.
    /// </summary>
    public virtual IEnumerable<Staff>? Staffs { get; set; }
}
