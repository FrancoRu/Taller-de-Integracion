using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models;

/// <summary>
/// Represents a player in the Club12 application.
/// </summary>
[Table("Players", Schema = "Club12")]
[Index(nameof(DocumentNumber), IsUnique = true)]
public class Player : EntityBase
{
    /// <summary>
    /// The first name of the player.
    /// </summary>
    [Required]
    [MaxLength(70)]
    public required string FirstName { get; set; }

    /// <summary>
    /// The second name of the player.
    /// </summary>
    [MaxLength(70)]
    public string? SecondName { get; set; }

    /// <summary>
    /// The last name of the player.
    /// </summary>
    [Required]
    [MaxLength(70)]
    public required string LastName { get; set; }

    /// <summary>
    /// The full name of the player, composed of last name in uppercase followed by first and second name if present.
    /// </summary>
    public string FullName => string.Concat(
        LastName.ToUpper(),
        string.IsNullOrWhiteSpace(SecondName) ? $" {FirstName}" : $" {FirstName} {SecondName}"
    );

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
    /// The phone number of the individual.
    /// </summary>
    public string? PhoneNumber { get; set; }

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
