using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerStatistic.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;
using Domain.Entities.Models;
using LinqKit;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class PlayerStatisticService(IPlayerStatisticRepository playerStatisticRepository) : IPlayerStatisticService
{
    public async Task<PlayerStatistic> CreatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
    {
        await playerStatisticRepository.AddAsync(playerStatisticEntity);
        return playerStatisticEntity;
    }

    public async Task<PlayerStatistic?> GetPlayerStatisticByIdAsync(Guid playerStatisticId) 
        => await playerStatisticRepository.GetByIdAsync(playerStatisticId);

    public async Task DeletePlayerStatisticAsync(Guid id)
        => await playerStatisticRepository.RemoveAsync(playerStatistic => playerStatistic.Id == id);

    public async Task UpdatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
        => await playerStatisticRepository.UpdateAsync(playerStatisticEntity);

    public async Task<PaginatedResponse<PlayerStatistic>> GetPlayerStatisticsAsync(GetPlayerStatisticsFilteredRequest filter)
    {
        Expression<Func<PlayerStatistic, bool>> expression =
            QueryableExtensions.ConstructFilterExpression<PlayerStatistic, GetPlayerStatisticsFilteredRequest>(filter);

        if (filter.TeamId.HasValue)
        {
            Expression<Func<PlayerStatistic, bool>> teamExpression =
                playerStatistic => playerStatistic.Player != null && playerStatistic.Player.TeamId == filter.TeamId.Value;
            expression = expression.And(teamExpression);
        }

        IEnumerable<PlayerStatistic> filteredStatistics = await playerStatisticRepository.FindAsync(
            expression,
            includes: [playerStatistic => playerStatistic.Match!, playerStatistic => playerStatistic.Player!],
            filter: filter);

        int totalCount = await playerStatisticRepository.CountAsync(expression);

        return new PaginatedResponse<PlayerStatistic>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredStatistics
        };
    }
}
