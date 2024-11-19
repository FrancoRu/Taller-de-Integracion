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
    /// The names of the player.
    /// </summary>
    [Required]
    [MaxLength(70)]
    public required string Names { get; set; }

    /// <summary>
    [MaxLength(35)]
    public required string Name { get; set; }

    /// <summary>
    /// The last name of the player.
    /// </summary>
    [Required]
    [MaxLength(70)]
    public required string LastName { get; set; }

    /// <summary>
    /// The document number of the player.
    /// </summary>
    [Required]
    [MaxLength(15)]
    public required string DocumentNumber { get; set; }

    /// <summary>
    /// Indicates if the player is currently sanctioned.
    /// </summary>
    [Required]
    [DefaultValue(false)]
    public required bool IsSanctioned { get; set; } = false;

    /// <summary>
    /// Indicates whether the individual is part of a federation.
    /// </summary>
    public required bool IsFederated { get; set; } = false;

    /// <summary>
    /// Gets or sets the name of the social work or insurance organization associated with the individual.
    /// </summary>
    public string? SocialWork { get; set; }

    /// <summary>
    /// Gets or sets the club or category to which the individual belongs.
    /// </summary>
    public string? ClubOrCategory { get; set; }

    /// <summary>
    /// Gets or sets the phone number of the individual.
    /// </summary>
    public string? PhoneNumber { get; set; }


    /// <summary>
    /// The club the player belongs to, if federated.
    /// </summary>
    [MaxLength(100)]
    public string? Club { get; set; }

    /// <summary>
    /// The category the player belongs to, if federated.
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// The birthdate of the player.
    /// </summary>
    public required DateTime BirthDate { get; set; }
     

    /// <summary>
    /// The medical social work or health provider of the player.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string SocialSecurity { get; set; }

    /// <summary>
    /// The team the player belongs to.
    /// </summary>
    [Column(nameof(TeamId))]
    [Required]
    public required Team Team { get; set; }

    /// <summary>
    /// The Id of the team the player belongs to.
    /// </summary>
    public Guid TeamId { get; set; }
}
