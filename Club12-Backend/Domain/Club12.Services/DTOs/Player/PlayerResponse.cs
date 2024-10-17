using Club12.DTOs.Abstract;
using System.ComponentModel.DataAnnotations;

namespace Club12.Services.DTOs.Player;

/// <summary>
/// Represents a response for a Player, inheriting from the base response.
/// </summary>
public record PlayerResponse : BaseEntityResponse
{
    /// <summary>
    /// The name of the Player.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The middle name of the player.
    /// </summary>
    public required string MiddleName { get; set; }

    /// <summary>
    /// The last name of the player.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// The document number of the player.
    /// </summary>
    [Required(ErrorMessage = "The DocumentNumber field is required.")]
    public required string DocumentNumber { get; set; }

    /// <summary>
    /// The unique identifier of the Team to which the Player belongs.
    /// </summary>
    public required string TeamId { get; set; }
}
