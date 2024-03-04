using Club12.Viewmodels.Abstract;

namespace Club12.Viewmodels.Player;

/// <summary>
/// Represents a response for a Player, inheriting from the base response.
/// </summary>
public class PlayerResponse : BaseResponse
{
    /// <summary>
    /// The name of the Player.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The last name of the player.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// The height of the player.
    /// </summary>
    public required double Height { get; set; }

    /// <summary>
    /// The weight of the player.
    /// </summary>
    public required double Weight { get; set; }

    /// <summary>
    /// The unique identifier of the Team to which the Player belongs.
    /// </summary>
    public required string TeamId { get; set; }
}
