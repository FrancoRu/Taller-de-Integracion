using Entities.Models.TeamEntity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.PlayerEntity;

/// <summary>
/// Represents a player in the Club12 application.
/// </summary>
[Table("Players", Schema = "Club12")]
public class Player : EntityBase
{
    /// <summary>
    /// The first name of the player.
    /// </summary>
    [Required]
    [MaxLength(35)]
    public required string FirstName { get; set; }

    /// <summary>
    /// The second name of the player.
    /// </summary>
    [Required]
    [MaxLength(35)]
    public required string SecondName { get; set; }

    /// <summary>
    /// The last name of the player.
    /// </summary>
    [Required]
    [MaxLength(35)]
    public required string LastName { get; set; }

    /// <summary>
    /// The document number of the player.
    /// </summary>
    [Required]
    [MaxLength(11)]
    public required string DocumentNumber { get; set; }

    /// <summary>
    /// Indicates if the player is currently sanctioned.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public required bool IsSanctioned { get; set; } = false;

    /// <summary>
    /// The team the player belongs to.
    /// </summary>
    [Column("TeamId")]
    [Required]
    public required Team Team { get; set; }

    /// <summary>
    /// The Id of the team the player belongs to.
    /// </summary>
    public Guid TeamId { get; set; }
}
