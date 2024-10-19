using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Player;

/// <summary>
/// Represents a request to create a Player.
/// </summary>
public class CreatePlayerRequest
{
    /// <summary>
    /// The name of the Player.
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; set; }

    /// <summary>
    /// The last name of the player.
    /// </summary>
    [Required(ErrorMessage = "The LastName field is required.")]
    public required string LastName { get; set; }

    /// <summary>
    /// The document number of the player.
    /// </summary>
    [Required(ErrorMessage = "The DocumentNumber field is required.")]
    public required string DocumentNumber { get; set; }

    /// <summary>
    /// The unique identifier of the Team to which the Player belongs.
    /// </summary>
    [Required(ErrorMessage = "The TeamId field is required.")]
    public required Guid TeamId { get; set; }
}
