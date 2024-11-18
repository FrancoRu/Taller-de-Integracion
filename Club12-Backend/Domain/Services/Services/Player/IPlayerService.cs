using Entities.DTOs.Abstract;
using Entities.DTOs.Player;
using Entities.Models.PlayerEntity;

namespace Services.Services.PlayerService;

/// <summary>
/// Represents a service for managing Players.
/// </summary>
public interface IPlayerService
{
    /// <summary>
    /// Creates a new Player.
    /// </summary>
    /// <param name="playerEntity">The Player entity to create.</param>
    /// <returns>The created Player.</returns>
    Task<Player> CreatePlayerAsync(Player playerEntity);

    /// <summary>
    /// Retrieves a Player by its id.
    /// </summary>
    /// <param name="playerId">The id of the Player to retrieve.</param>
    /// <returns>The Player with the specified id, or null if not found.</returns>
    Task<Player?> GetPlayerByIdAsync(Guid playerId);

    /// <summary>
    /// Updates a Player asynchronously.
    /// </summary>
    /// <param name="playerEntity">The Player to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdatePlayerAsync(Player playerEntity);

    /// <summary>
    /// Deletes a Player asynchronously.
    /// </summary>
    /// <param name="playerEntity">The Player to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    Task<bool> DeletePlayerAsync(Player playerEntity);

    /// <summary>
    /// Retrieves players with pagination and filtering.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the players.</returns>
    Task<PaginatedResponse<Player>> GetAllPlayersAsync(PlayerFilterRequestBase filter);
}
