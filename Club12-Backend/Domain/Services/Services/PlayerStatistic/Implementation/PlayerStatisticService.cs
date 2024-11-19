using Entities.Models.PlayerStatisticEntity;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Services.PlayerStatisticService;

namespace Club12.Services.Services.PlayerStatisticService.Implementation;

public class PlayerStatisticService(IGenericService<PlayerStatistic> _genericPlayerStatisticService) : IPlayerStatisticService
{
    public async Task<PlayerStatistic> CreatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
    {
        await _genericPlayerStatisticService.InsertAsync(playerStatisticEntity);
        return playerStatisticEntity;
    }

    public async Task<PlayerStatistic?> GetPlayerStatisticByIdAsync(Guid playerStatisticId)
    {
        return await _genericPlayerStatisticService.FilterByExpression(playerStatistic => playerStatistic.Id == playerStatisticId).FirstOrDefaultAsync();
    }

    public async Task<bool> DeletePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
    {
        try
        {
            await _genericPlayerStatisticService.DeleteAsync(playerStatisticEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
    {
        try
        {
            await _genericPlayerStatisticService.UpdateAsync(playerStatisticEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
