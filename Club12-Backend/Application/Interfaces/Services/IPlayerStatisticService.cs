using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerStatistic.Request;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Retrieves a paginated, filtered list of player statistics.
    /// </summary>
    Task<PaginatedResponse<PlayerStatistic>> GetPlayerStatisticsAsync(GetPlayerStatisticsFilteredRequest filter);

    /// <summary>
    /// Loads a whole team's coherent scoring sheet for a match (HU-71): the
    /// listed players' points must add up to the team's final score and every
    /// player must be on the roster and eligible, or nothing is saved.
    /// </summary>
    Task<List<PlayerStatistic>> LoadTeamMatchSheetAsync(LoadMatchSheetRequest request);

    /// <summary>
    /// Finishes a match by loading both teams' scoring sheets in one
    /// operation (HU-72): the final score is derived as the sum of each
    /// team's listed player points, rather than typed in separately.
    /// </summary>
    /// <returns>The finalized match, or null if no match with that id exists.</returns>
    Task<Match?> LoadMatchResultFromSheetsAsync(LoadMatchResultFromSheetsRequest request);
}
