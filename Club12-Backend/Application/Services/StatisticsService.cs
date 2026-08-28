using Application.DTOs.Statistics.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

using System;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Thin application service over <see cref="IStatisticsRepository"/> for the
/// historical-statistics reads (HU-87 / HU-88). The aggregation lives in the
/// repository (an EF query against PlayerStatistic / PlayerTeamRegistration /
/// PlayerSanction), mirroring how <see cref="ScorerService"/> delegates the
/// goleadores ranking to <see cref="IScorerRepository"/>.
/// </summary>
public class StatisticsService(IStatisticsRepository statisticsRepository) : IStatisticsService
{
    public Task<PlayerStatisticCardResponse?> GetPlayerCardAsync(Guid playerId) =>
        statisticsRepository.GetPlayerCardAsync(playerId);

    public Task<PlayerHistoryResponse?> GetPlayerHistoryAsync(Guid playerId) =>
        statisticsRepository.GetPlayerHistoryAsync(playerId);
}
