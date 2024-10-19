using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Player;

/// <summary>
/// Represents a request to create a Player.
/// </summary>
public class UpdatePlayerRequest
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
}
