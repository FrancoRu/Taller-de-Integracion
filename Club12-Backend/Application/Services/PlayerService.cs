using Application.DTOs.Abstract.Response;
using Application.DTOs.Player.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class PlayerService(IPlayerRepository playerRepository) : IPlayerService
{
    public async Task<Player> CreatePlayerAsync(Player playerEntity)
    {
        await playerRepository.AddAsync(playerEntity);
        return playerEntity;
    }

    public async Task<Player?> GetPlayerByIdAsync(Guid playerId)
    {
        return await playerRepository.GetByIdAsync(playerId);
    }

    public async Task DeletePlayerAsync(Guid id)
    {
        await playerRepository.RemoveAsync(player => player.Id == id);
    }

    public async Task UpdatePlayerAsync(Player playerEntity)
    {
        await playerRepository.UpdateAsync(playerEntity);
    }

    public async Task<PaginatedResponse<Player>> GetAllPlayersAsync(PlayerFilterRequestBase filter)
    {
        Expression<Func<Player, bool>> expression = QueryableExtensions.ConstructFilterExpression<Player, PlayerFilterRequestBase>(filter);
        IEnumerable<Player> filteredPlayers = await playerRepository.FindAsync(expression, filter: filter);

        int totalCount = await playerRepository.CountAsync(expression);

        return new PaginatedResponse<Player>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredPlayers
        };
    }
}
