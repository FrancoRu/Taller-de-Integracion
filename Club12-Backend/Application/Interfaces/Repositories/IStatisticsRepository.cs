using Application.DTOs.Statistics.Response;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Read-only historical-statistics aggregations (HU-87 / HU-88) that group a
/// person's data across every season by their stable PlayerId.
/// </summary>
public interface IStatisticsRepository
{
    /// <summary>
    /// HU-87: the player's statistic card — total/average points and games
    /// played, per season and overall. Returns null when the player does not
    /// exist.
    /// </summary>
    Task<PlayerStatisticCardResponse?> GetPlayerCardAsync(Guid playerId);

    /// <summary>
    /// HU-88: the player's per-season trajectory — team, stats and sanctions
    /// for each season they were registered. Returns null when the player does
    /// not exist.
    /// </summary>
    Task<PlayerHistoryResponse?> GetPlayerHistoryAsync(Guid playerId);
}
