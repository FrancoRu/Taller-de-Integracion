using Application.DTOs.Statistics.Response;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Historical player statistics: per-season and overall aggregations for a single person across every season.
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Returns the player's statistic card, or null if the player is unknown.
    /// </summary>
    Task<PlayerStatisticCardResponse?> GetPlayerCardAsync(Guid playerId);

    /// <summary>
    /// Returns the player's cross-season history, or null if the player is unknown.
    /// </summary>
    Task<PlayerHistoryResponse?> GetPlayerHistoryAsync(Guid playerId);
}
