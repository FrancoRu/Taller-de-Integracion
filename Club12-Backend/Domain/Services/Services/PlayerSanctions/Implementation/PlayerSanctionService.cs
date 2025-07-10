using Entities.DTOs.Abstract;
using Entities.DTOs.PlayerSanction;
using Entities.Models.PlayerSanctions;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

namespace Services.Services.PlayerSanctions.Implementation;

public class PlayerSanctionService(IGenericService<PlayerSanction> _genericPlayerSanctionService) : IPlayerSanctionService
{
    public async Task<PlayerSanction> CreatePlayerSanctionAsync(PlayerSanction playerSanctionEntity)
    {
        await _genericPlayerSanctionService.InsertAsync(playerSanctionEntity);
        return playerSanctionEntity;
    }

    public async Task<PlayerSanction?> GetPlayerSanctionByIdAsync(Guid playerSanctionId) => await _genericPlayerSanctionService.FilterByExpression(playerSanction => playerSanction.Id == playerSanctionId).FirstOrDefaultAsync();

    public async Task<bool> DeletePlayerSanctionAsync(PlayerSanction playerSanctionEntity)
    {
        try
        {
            await _genericPlayerSanctionService.DeleteAsync(playerSanctionEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdatePlayerSanctionAsync(PlayerSanction playerSanctionEntity)
    {
        try
        {
            await _genericPlayerSanctionService.UpdateAsync(playerSanctionEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<PlayerSanction>> GetExpiredSanctionsAsync(DateTime cutoffDate) => await _genericPlayerSanctionService.FilterByExpression(playerSanction => playerSanction.IssuedDate.AddDays(playerSanction.Duration) <= cutoffDate)
            .Include(s => s.Player)
            .ToListAsync();

    public async Task<PaginatedResponse<PlayerSanction>> GetPlayerSanctionsAsync(GetPlayerSanctionsFilteredRequest filter)
    {
        Expression<Func<PlayerSanction, bool>> expression = QueryableExtensions.ConstructFilterExpression<PlayerSanction, GetPlayerSanctionsFilteredRequest>(filter);
        IQueryable<PlayerSanction> filteredSanctions = _genericPlayerSanctionService.FilterByExpressionWithPagination(expression, filter).SortBy(filter);
        int totalCount = await _genericPlayerSanctionService.GetCountAsync(expression);

        return new PaginatedResponse<PlayerSanction>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredSanctions.ToListAsync()
        };
    }
}
