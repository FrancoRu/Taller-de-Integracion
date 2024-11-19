using Entities.DTOs.Player.Validation;

using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Player;

/// <summary>
/// Represents a request to update a Player.
/// </summary>
public class UpdatePlayerRequest
{
    /// <summary>
    /// The names of the player.
    /// </summary>
    [MaxLength(70, ErrorMessage = "The Name field must not exceed 70 characters.")]
    public string? Name { get; set; }

    /// <summary>
    /// The last name of the player.
    /// </summary>
    [MaxLength(70, ErrorMessage = "The LastName field must not exceed 70 characters.")]
    public string? LastName { get; set; }

    /// <summary>
    /// The document number of the player.
    /// </summary>
    [MaxLength(15, ErrorMessage = "The DocumentNumber field must not exceed 15 characters.")]
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// The birthdate of the player.
    /// </summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// The phone number of the player.
    /// </summary>
    [MaxLength(15, ErrorMessage = "The PhoneNumber field must not exceed 15 characters.")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// The medical social work or health provider of the player.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The SocialSecurity field must not exceed 100 characters.")]
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
    [FederatedValidation(ErrorMessage = "Both Club and Category are required if the player is federated.")]
    public string? Club { get; set; }

    /// <summary>
    /// The category the player belongs to, if federated.
    /// </summary>
    [MaxLength(50)]
    [FederatedValidation(ErrorMessage = "Both Club and Category are required if the player is federated.")]
    public string? Category { get; set; }
}
