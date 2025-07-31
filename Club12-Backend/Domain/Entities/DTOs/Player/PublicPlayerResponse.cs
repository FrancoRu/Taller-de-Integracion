using Entities.DTOs.Abstract;

namespace Entities.DTOs.Player;

/// <summary>
/// Represents a response for a Player, inheriting from the base response.
/// </summary>
public class PublicPlayerResponse : BaseEntityResponse
{
    /// <summary>
    /// The first name of the Player.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// The second name of the Player.
    /// </summary>
    public required string SecondName { get; set; }

    /// <summary>
    /// The last name of the player.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// The full name of the player.
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// The unique identifier of the Team to which the Player belongs.
    /// </summary>
    public required Guid TeamId { get; set; }
}
