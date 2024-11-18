using Entities.DTOs.Abstract;
using Entities.DTOs.Player;
using Entities.Models.PlayerEntity;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

namespace Services.Services.PlayerService.Implementation;

public class PlayerService(IGenericService<Player> _genericPlayerService) : IPlayerService
{
    public async Task<Player> CreatePlayerAsync(Player playerEntity)
    {
        await _genericPlayerService.InsertAsync(playerEntity);
        return playerEntity;
    }

    public async Task<Player?> GetPlayerByIdAsync(Guid playerId)
    {
        return await _genericPlayerService.FilterByExpression(player => player.Id == playerId).FirstOrDefaultAsync();
    }

    public async Task<bool> DeletePlayerAsync(Player playerEntity)
    {
        try
        {
            await _genericPlayerService.DeleteAsync(playerEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdatePlayerAsync(Player playerEntity)
    {
        try
        {
            await _genericPlayerService.UpdateAsync(playerEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PaginatedResponse<Player>> GetAllPlayersAsync(GetPlayersFilteredRequest filter)
    {
        Expression<Func<Player, bool>> expression = QueryableExtensions.ConstructFilterExpression<Player, GetPlayersFilteredRequest>(filter);
        IQueryable<Player> filteredPlayers = _genericPlayerService.FilterByExpressionWithPagination(expression, filter).SortBy(filter);
        int totalCount = await _genericPlayerService.GetCountAsync(expression);

        return new PaginatedResponse<Player>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredPlayers.ToListAsync()
        };
    }
}
