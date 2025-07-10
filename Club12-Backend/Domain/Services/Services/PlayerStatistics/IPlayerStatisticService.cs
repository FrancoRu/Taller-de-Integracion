using Entities.Models.PlayerStatistics;

namespace Services.Services.PlayerStatistics;

/// <summary>
/// Represents a service for managing player statistics.
/// </summary>
public interface IPlayerStatisticService
{
    /// <summary>
    /// Creates a new player statistic asynchronously.
    /// </summary>
    /// <param name="playerStatisticEntity">The player statistic entity to create.</param>
    /// <returns>The created player statistic.</returns>
    Task<PlayerStatistic> CreatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity);

    /// <summary>
    /// Retrieves a player statistic by its ID asynchronously.
    /// </summary>
    /// <param name="playerStatisticId">The ID of the player statistic to retrieve.</param>
    /// <returns>The player statistic with the specified ID, or null if not found.</returns>
    Task<PlayerStatistic?> GetPlayerStatisticByIdAsync(Guid playerStatisticId);

    /// <summary>
    /// Deletes a player statistic asynchronously.
    /// </summary>
    /// <param name="playerStatisticEntity">The player statistic to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    Task<bool> DeletePlayerStatisticAsync(PlayerStatistic playerStatisticEntity);

    /// <summary>
    /// Updates a player statistic asynchronously.
    /// </summary>
    /// <param name="playerStatisticEntity">The player statistic to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity);
}
