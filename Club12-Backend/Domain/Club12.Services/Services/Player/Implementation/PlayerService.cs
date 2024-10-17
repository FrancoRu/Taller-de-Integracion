using Club12.Entities.PlayerEntity;
using Club12.Services.DataAccessLayer.GenericEntity;

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
}
