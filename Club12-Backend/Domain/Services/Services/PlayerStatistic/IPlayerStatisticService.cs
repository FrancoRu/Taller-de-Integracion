using Entities.Models.PlayerStatisticEntity;

namespace Services.Services.PlayerStatisticService;

/// <summary>
/// Represents a service for managing player statistics.
/// </summary>
public interface IPlayerStatisticService
{
    /// <summary>
    /// Creates a new player statistic.
    /// </summary>
    /// <param name="playerStatisticEntity">The player statistic entity to create.</param>
    /// <returns>The created player statistic.</returns>
    PlayerStatistic CreatePlayerStatistic(PlayerStatistic playerStatisticEntity);

    /// <summary>
    /// Retrieves a player statistic by its ID.
    /// </summary>
    /// <param name="playerStatisticId">The ID of the player statistic to retrieve.</param>
    /// <returns>The player statistic with the specified ID, or null if not found.</returns>
    PlayerStatistic? GetPlayerStatisticById(Guid playerStatisticId);

    /// <summary>
    /// Deletes a player statistic.
    /// </summary>
    /// <param name="playerStatisticEntity">The player statistic to delete.</param>
    void DeletePlayerStatistic(PlayerStatistic playerStatisticEntity);

    /// <summary>
    /// Updates a player statistic asynchronously.
    /// </summary>
    /// <param name="playerStatisticEntity">The player statistic to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity);
}
