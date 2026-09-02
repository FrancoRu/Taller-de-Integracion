using Application.Utils.Constants.Validation;

using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Player.Request;

/// <summary>
/// Represents a request to update a Player.
/// </summary>
public class UpdatePlayerRequest
{
    /// <summary>
    /// The first name of the player.
    /// </summary>
    [Required(ErrorMessage = "The First Name field is required.")]
    [MaxLength(PlayerFieldLengths.NameMaxLength, ErrorMessage = "The Name field must not exceed 70 characters.")]
    public required string FirstName { get; set; }

    /// <summary>
    /// The second name of the player.
    /// </summary>
    [MaxLength(PlayerFieldLengths.NameMaxLength, ErrorMessage = "The Second Name field must not exceed 70 characters.")]
    public string? SecondName { get; set; } = null;

    /// <summary>
    /// The last name of the player.
    /// </summary>
    [MaxLength(PlayerFieldLengths.NameMaxLength, ErrorMessage = "The LastName field must not exceed 70 characters.")]
    public string? LastName { get; set; }

    /// <summary>
    /// The document number of the player.
    /// </summary>
    [MaxLength(PlayerFieldLengths.DocumentNumberMaxLength, ErrorMessage = "The DocumentNumber field must not exceed 15 characters.")]
    [RegularExpression(ValidationPatterns.DocumentNumber, ErrorMessage = ValidationPatterns.DocumentNumberError)]
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// The birthdate of the player. Must be at least 15 years ago.
    /// </summary>
    [MinimumAge(15)]
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// The phone number of the player.
    /// </summary>
    [MaxLength(PlayerFieldLengths.PhoneNumberMaxLength, ErrorMessage = "The PhoneNumber field must not exceed 15 characters.")]
    [ArgentinePhoneNumber]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// The medical social work or health provider of the player.
    /// </summary>
    [MaxLength(PlayerFieldLengths.SocialSecurityMaxLength, ErrorMessage = "The SocialSecurity field must not exceed 100 characters.")]
    public string? SocialSecurity { get; set; }

    /// <summary>
    /// The team the player belongs to.
    /// </summary>
    public Guid? TeamId { get; set; }

    /// <summary>
    /// Indicates if the player is federated.
    /// </summary>
    public bool? IsFederated { get; set; }

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
}
