using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerSanction.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;
using Domain.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class PlayerSanctionService(IPlayerSanctionRepository playerSanctionRepository) : IPlayerSanctionService
{
    public async Task<PlayerSanction> CreatePlayerSanctionAsync(PlayerSanction playerSanctionEntity)
    {
        await playerSanctionRepository.AddAsync(playerSanctionEntity);
        return playerSanctionEntity;
    }

    public async Task<PlayerSanction?> GetPlayerSanctionByIdAsync(Guid playerSanctionId) => 
        await playerSanctionRepository.GetByIdAsync(playerSanctionId);

    public async Task DeletePlayerSanctionAsync(Guid id)
        => await playerSanctionRepository.RemoveAsync(playerSanction => playerSanction.Id == id);
    

    public async Task UpdatePlayerSanctionAsync(PlayerSanction playerSanctionEntity)
        => await playerSanctionRepository.UpdateAsync(playerSanctionEntity);

    public async Task<IEnumerable<PlayerSanction>> GetExpiredSanctionsAsync(DateTime cutoffDate)
        => await playerSanctionRepository.FindAsync(
            playerSanction => playerSanction.IssuedDate.AddDays(playerSanction.Duration) <= cutoffDate, 
            includes: [playerSanction => playerSanction.Player]);

    public async Task<PaginatedResponse<PlayerSanction>> GetPlayerSanctionsAsync(GetPlayerSanctionsFilteredRequest filter)
    {
        Expression<Func<PlayerSanction, bool>> expression = QueryableExtensions.ConstructFilterExpression<PlayerSanction, GetPlayerSanctionsFilteredRequest>(filter);
        IEnumerable<PlayerSanction> filteredSanctions = await playerSanctionRepository.FindAsync(expression, 
            filter: filter, 
            includes: [playerSanction => playerSanction.Player, playerSanction => playerSanction.Match]);

        int totalCount = await playerSanctionRepository.CountAsync(expression);

        return new PaginatedResponse<PlayerSanction>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredSanctions
        };
    }
}
