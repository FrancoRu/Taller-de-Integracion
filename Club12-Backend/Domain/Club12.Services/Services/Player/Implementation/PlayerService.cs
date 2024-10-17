using Club12.Entities.PlayerEntity;
using Club12.Services.DataAccessLayer.GenericEntity;
using Club12.Services.DTOs.Abstract;
using Club12.Services.Utils.OrderFiltering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Club12.Services.Services.PlayerService.Implementation;

public class PlayerService(IGenericService<Player> genericPlayerService) : IPlayerService
{
    public Player CreatePlayer(Player playerEntity)
    {
        genericPlayerService.Insert(playerEntity);
        return playerEntity;
    }

    public Player? GetPlayerById(Guid playerId)
    {
        return genericPlayerService.TryGet(playerId);
    }

    public void DeletePlayer(Player playerEntity)
    {
        genericPlayerService.Delete(playerEntity);
    }

    public async Task<bool> UpdatePlayer(Player playerEntity)
    {
        try
        {
            await genericPlayerService.UpdateAsync(playerEntity);
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
        IQueryable<Player> filteredPlayers = genericPlayerService.FilterByExpressionWithPagination(expression, filter).SortBy(filter);
        int totalCount = await genericPlayerService.GetCountAsync(expression);

        return new PaginatedResponse<Player>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredPlayers.ToListAsync()
        };
    }
}
