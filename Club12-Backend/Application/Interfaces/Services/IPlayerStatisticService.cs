using Domain.Entities.Models;
using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

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

    Task DeletePlayerStatisticAsync(Guid id);

    Task UpdatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity);
}
