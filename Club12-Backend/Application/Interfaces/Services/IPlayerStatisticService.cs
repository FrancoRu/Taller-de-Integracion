using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerStatistic.Request;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IPlayerStatisticService
{
    Task<PlayerStatistic> CreatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity);

    Task<PlayerStatistic?> GetPlayerStatisticByIdAsync(Guid playerStatisticId);

    Task DeletePlayerStatisticAsync(Guid id);

    Task UpdatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity);

    /// <summary>
    /// Retrieves a paginated, filtered list of player statistics.
    /// </summary>
    Task<PaginatedResponse<PlayerStatistic>> GetPlayerStatisticsAsync(GetPlayerStatisticsFilteredRequest filter);

    /// <summary>
    /// Loads a whole team's coherent scoring sheet for a match.
    /// </summary>
    Task<List<PlayerStatistic>> LoadTeamMatchSheetAsync(LoadMatchSheetRequest request);

    /// <summary>
    /// Finishes a match by loading both teams' scoring sheets in one operation.
    /// </summary>
    /// <returns>The finalized match, or null if no match with that id exists.</returns>
    Task<Match?> LoadMatchResultFromSheetsAsync(LoadMatchResultFromSheetsRequest request);
}
