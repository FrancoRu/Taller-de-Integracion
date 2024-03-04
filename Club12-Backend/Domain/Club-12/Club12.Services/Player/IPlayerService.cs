using Club12.Entities.PlayerEntity;

namespace Club12.Services.Players;

/// <summary>
/// Represents a service for managing Players.
/// </summary>
public interface IPlayerService
{
    /// <summary>
    /// Creates a new Player.
    /// </summary>
    /// <param name="PlayerEntity">The Player entity to create.</param>
    /// <returns>The created Player.</returns>
    Player CreatePlayer(Player PlayerEntity);

    /// <summary>
    /// Retrieves a Player by its id.
    /// </summary>
    /// <param name="PlayerId">The id of the Player to retrieve.</param>
    /// <returns>The Player with the specified id, or null if not found.</returns>
    Player? GetPlayerById(Guid PlayerId);

    /// <summary>
    /// Updates a Player asynchronously.
    /// </summary>
    /// <param name="Player">The Player to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdatePlayer(Player Player);

    /// <summary>
    /// Deletes a Player.
    /// </summary>
    /// <param name="Player">The Player to delete.</param>
    void DeletePlayer(Player Player);
}
