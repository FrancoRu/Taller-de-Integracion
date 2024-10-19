using Entities.DTOs.Abstract;
using Entities.DTOs.PlayerSanction;
using Entities.Models.PlayerSanctionEntity;
using Microsoft.EntityFrameworkCore;
using Services.DataAccessLayer.GenericEntity;
using Services.Services.PlayerSanctionService;
using Services.Utils.OrderFiltering;
using System.Linq.Expressions;

namespace Club12.Services.Services.PlayerSanctionService.Implementation;

public class PlayerSanctionService(IGenericService<PlayerSanction> genericPlayerSanctionService) : IPlayerSanctionService
{
    public async Task<PlayerSanction> CreatePlayerSanctionAsync(PlayerSanction playerSanctionEntity)
    {
        await genericPlayerSanctionService.InsertAsync(playerSanctionEntity);
        return playerSanctionEntity;
    }

    public PlayerSanction? GetPlayerSanctionByIdAsync(Guid playerSanctionId)
    {
        return genericPlayerSanctionService.TryGet(playerSanctionId);
    }


    public async Task DeletePlayerSanctionAsync(PlayerSanction playerSanctionEntity)
    {
        await genericPlayerSanctionService.DeleteAsync(playerSanctionEntity);
    }


    public async Task<bool> UpdatePlayerSanctionAsync(PlayerSanction playerSanctionEntity)
    {
        try
        {
            await genericPlayerSanctionService.UpdateAsync(playerSanctionEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<PlayerSanction>> GetExpiredSanctionsAsync(DateTime cutoffDate)
    {
        return await genericPlayerSanctionService.FilterByExpression(playerSanction => playerSanction.IssuedDate.AddDays(playerSanction.Duration) <= cutoffDate)
            .Include(s => s.Player)
            .ToListAsync();
    }

    public async Task<PaginatedResponse<PlayerSanction>> GetPlayerSanctionsAsync(GetPlayerSanctionsFilteredRequest filter)
    {

        Expression<Func<PlayerSanction, bool>> expression = QueryableExtensions.ConstructFilterExpression<PlayerSanction, GetPlayerSanctionsFilteredRequest>(filter);
        IQueryable<PlayerSanction> filteredSanctions = genericPlayerSanctionService.FilterByExpressionWithPagination(expression, filter).SortBy(filter);
        int totalCount = await genericPlayerSanctionService.GetCountAsync(expression);

        return new PaginatedResponse<PlayerSanction>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredSanctions.ToListAsync()
        };
    }
}
