using Entities.Models.PlayerStatisticEntity;

using Services.DataAccessLayer.GenericEntity;
using Services.Services.PlayerStatisticService;

namespace Club12.Services.Services.PlayerStatisticService.Implementation;

public class PlayerStatisticService(IGenericService<PlayerStatistic> genericPlayerStatisticService) : IPlayerStatisticService
{
    public PlayerStatistic CreatePlayerStatistic(PlayerStatistic playerStatisticEntity)
    {
        genericPlayerStatisticService.Insert(playerStatisticEntity);
        return playerStatisticEntity;
    }

    public PlayerStatistic? GetPlayerStatisticById(Guid playerStatisticId)
    {
        return genericPlayerStatisticService.TryGet(playerStatisticId);
    }

    public void DeletePlayerStatistic(PlayerStatistic playerStatisticEntity)
    {
        genericPlayerStatisticService.Delete(playerStatisticEntity);
    }

    public async Task<bool> UpdatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
    {
        try
        {
            await genericPlayerStatisticService.UpdateAsync(playerStatisticEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
