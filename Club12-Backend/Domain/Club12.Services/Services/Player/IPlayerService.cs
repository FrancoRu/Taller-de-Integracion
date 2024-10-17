using Club12.Entities.PlayerEntity;

namespace Club12.Services.Services.PlayerService;
/// <summary>
/// Represents a service for managing Players.
/// </summary>
public interface IPlayerService
{
    /// <summary>
    /// Creates a new Player.
    /// </summary>
    /// <param name="playerEntity">The Player entity to create.</param>
    /// <param name="userId">The ID of the user performing the operation.</param>
    /// <returns>The created Player.</returns>
    Player CreatePlayer(Player playerEntity);

    /// <summary>
    /// Retrieves a Player by its id.
    /// </summary>
    /// <param name="playerId">The id of the Player to retrieve.</param>
    /// <returns>The Player with the specified id, or null if not found.</returns>
    Player? GetPlayerById(Guid playerId);

    /// <summary>
    /// Updates a Player asynchronously.
    /// </summary>
    /// <param name="playerEntity">The Player to update.</param>
    /// <param name="userId">The ID of the user performing the operation.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdatePlayer(Player playerEntity);

    /// <summary>
    /// Deletes a Player.
    /// </summary>
    /// <param name="playerEntity">The Player to delete.</param>
    void DeletePlayer(Player playerEntity);
}
