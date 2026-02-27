using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Player.Request;

/// <summary>
/// Represents a request to create a Player.
/// </summary>
public class CreatePlayerRequest
{
    /// <summary>
    /// The first name of the player.
    /// </summary>
    [Required(ErrorMessage = "The First Name field is required.")]
    [MaxLength(70, ErrorMessage = "The Name field must not exceed 70 characters.")]
    public required string FirstName { get; set; }

    /// <summary>
    /// The second name of the player.
    /// </summary>
    [MaxLength(70, ErrorMessage = "The Second Name field must not exceed 70 characters.")]
    public string? SecondName { get; set; } = null;

    /// <summary>
    /// The last name of the player.
    /// </summary>
    [Required(ErrorMessage = "The LastName field is required.")]
    [MaxLength(70, ErrorMessage = "The LastName field must not exceed 70 characters.")]
    public required string LastName { get; set; }

    /// <summary>
    /// The document number of the player.
    /// </summary>
    [Required(ErrorMessage = "The DocumentNumber field is required.")]
    [MaxLength(15, ErrorMessage = "The DocumentNumber field must not exceed 15 characters.")]
    public required string DocumentNumber { get; set; }

    /// <summary>
    /// The birthdate of the player.
    /// </summary>
    [Required(ErrorMessage = "The BirthDate field is required.")]
    public required DateTime BirthDate { get; set; }

    /// <summary>
    /// The phone number of the player.
    /// </summary>
    [Required(ErrorMessage = "The PhoneNumber field is required.")]
    [MaxLength(15, ErrorMessage = "The PhoneNumber field must not exceed 15 characters.")]
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// The medical social work or health provider of the player.
    /// </summary>
    [Required(ErrorMessage = "The SocialSecurity field is required.")]
    [MaxLength(100, ErrorMessage = "The SocialSecurity field must not exceed 100 characters.")]
    public required string SocialSecurity { get; set; }


    /// <summary>
    /// The team the player belongs to.
    /// </summary>
    [Required(ErrorMessage = "The TeamId field is required.")]
    public required Guid TeamId { get; set; }

}
