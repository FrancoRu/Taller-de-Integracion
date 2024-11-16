using Entities.DTOs.Abstract;

using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Player;

/// <summary>
/// Represents a response for a Player, inheriting from the base response.
/// </summary>
public class PlayerResponse : BaseEntityResponse
{
    /// <summary>
    /// The name of the Player.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// The middle name of the player.
    /// </summary>
    public required string SecondName { get; set; }

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
