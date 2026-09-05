using Application.DTOs.Statistics.Response;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Read-only historical-statistics aggregations that group a person's data across every season by their stable PlayerId.
/// </summary>
public interface IStatisticsRepository
{
    /// <summary>
    /// Returns the player's statistic card with total and average points and games played per season and overall, or null when the player does not exist.
    /// </summary>
    Task<PlayerStatisticCardResponse?> GetPlayerCardAsync(Guid playerId);

    /// <summary>
    /// Returns the player's per-season trajectory with team, stats, and sanctions for each season they were registered, or null when the player does not exist.
    /// </summary>
    Task<PlayerHistoryResponse?> GetPlayerHistoryAsync(Guid playerId);
}
