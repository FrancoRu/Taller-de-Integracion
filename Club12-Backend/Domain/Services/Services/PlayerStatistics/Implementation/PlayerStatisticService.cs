using Entities.Models.PlayerStatistics;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;

namespace Services.Services.PlayerStatistics.Implementation;

public class PlayerStatisticService(IGenericService<PlayerStatistic> _genericPlayerStatisticService) : IPlayerStatisticService
{
    public async Task<PlayerStatistic> CreatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
    {
        await _genericPlayerStatisticService.InsertAsync(playerStatisticEntity);
        return playerStatisticEntity;
    }

    public async Task<PlayerStatistic?> GetPlayerStatisticByIdAsync(Guid playerStatisticId) => await _genericPlayerStatisticService.FilterByExpression(playerStatistic => playerStatistic.Id == playerStatisticId).FirstOrDefaultAsync();

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
