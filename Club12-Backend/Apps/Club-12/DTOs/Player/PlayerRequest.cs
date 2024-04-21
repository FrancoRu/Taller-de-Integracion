using System.ComponentModel.DataAnnotations;

namespace Club12.Viewmodels.Player;

/// <summary>
/// Represents a request to create a Player.
/// </summary>
public class PlayerRequest
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
    /// The height of the player.
    /// </summary>
    [Required(ErrorMessage = "The Height field is required.")]
    public required double Height { get; set; }

    /// <summary>
    /// The weight of the player.
    /// </summary>
    [Required(ErrorMessage = "The Weight field is required.")]
    public required double Weight { get; set; }

    /// <summary>
    /// The unique identifier of the Team to which the Player belongs.
    /// </summary>
    [Required(ErrorMessage = "The TeamId field is required.")]
    public required Guid TeamId { get; set; }
}
