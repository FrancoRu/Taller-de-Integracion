using Application.Utils.Constants.Validation;

using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Player.Request;

/// <summary>
/// Represents a request to update a Player.
/// </summary>
public class UpdatePlayerRequest
{
    [Required(ErrorMessage = "The First Name field is required.")]
    [MaxLength(PlayerFieldLengths.NameMaxLength, ErrorMessage = "The Name field must not exceed 70 characters.")]
    public required string FirstName { get; set; }

    [MaxLength(PlayerFieldLengths.NameMaxLength, ErrorMessage = "The Second Name field must not exceed 70 characters.")]
    public string? SecondName { get; set; } = null;

    [MaxLength(PlayerFieldLengths.NameMaxLength, ErrorMessage = "The LastName field must not exceed 70 characters.")]
    public string? LastName { get; set; }

    [MaxLength(PlayerFieldLengths.DocumentNumberMaxLength, ErrorMessage = "The DocumentNumber field must not exceed 15 characters.")]
    [RegularExpression(ValidationPatterns.DocumentNumber, ErrorMessage = ValidationPatterns.DocumentNumberError)]
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// The birthdate of the player. Must be at least 15 years ago.
    /// </summary>
    [MinimumAge(15)]
    public DateTime? BirthDate { get; set; }

    [MaxLength(PlayerFieldLengths.PhoneNumberMaxLength, ErrorMessage = "The PhoneNumber field must not exceed 15 characters.")]
    [ArgentinePhoneNumber]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// The player's health insurance provider or social work plan.
    /// </summary>
    [MaxLength(PlayerFieldLengths.SocialSecurityMaxLength, ErrorMessage = "The SocialSecurity field must not exceed 100 characters.")]
    public string? SocialSecurity { get; set; }

    public Guid? TeamId { get; set; }

    public bool? IsFederated { get; set; }

    [MaxLength(100)]
    public string? Club { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }
}
