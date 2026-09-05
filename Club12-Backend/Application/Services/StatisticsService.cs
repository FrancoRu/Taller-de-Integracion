using Application.DTOs.Statistics.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

using System;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Thin application service over IStatisticsRepository for the historical-statistics reads.
/// </summary>
public class StatisticsService(IStatisticsRepository statisticsRepository) : IStatisticsService
{
    public Task<PlayerStatisticCardResponse?> GetPlayerCardAsync(Guid playerId) =>
        statisticsRepository.GetPlayerCardAsync(playerId);

    public Task<PlayerHistoryResponse?> GetPlayerHistoryAsync(Guid playerId) =>
        statisticsRepository.GetPlayerHistoryAsync(playerId);
}
