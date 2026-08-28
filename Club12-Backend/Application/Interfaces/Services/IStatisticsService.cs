using Application.DTOs.Statistics.Response;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Historical player statistics (HU-87 / HU-88): per-season and overall
/// aggregations for a single person across every season.
/// </summary>
public interface IStatisticsService
{
    /// <summary>HU-87: the player's statistic card, or null if the player is unknown.</summary>
    Task<PlayerStatisticCardResponse?> GetPlayerCardAsync(Guid playerId);

    /// <summary>HU-88: the player's cross-season history, or null if the player is unknown.</summary>
    Task<PlayerHistoryResponse?> GetPlayerHistoryAsync(Guid playerId);
}
