using Club12.Entities.PlayerEntity;
using Club12.Services.DataAccessLayer.GenericEntity;

namespace Club12.Services.Players.Implementation;

public class PlayerService : IPlayerService
{
    private readonly IGenericService<Player> _genericPlayerService;

    public PlayerService(IGenericService<Player> genericPlayerService)
    {
        _genericPlayerService = genericPlayerService;
    }

    public Player CreatePlayer(Player playerEntity)
    {
        _genericPlayerService.Insert(playerEntity);
        return playerEntity;
    }

    public Player? GetPlayerById(Guid playerId)
    {
        return _genericPlayerService.TryGet(playerId);
    }

    public void DeletePlayer(Player playerEntity)
    {
        _genericPlayerService.Delete(playerEntity);
    }

    public async Task<bool> UpdatePlayer(Player playerEntity)
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
}
