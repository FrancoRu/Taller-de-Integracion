using Club12.Entities.PlayerEntity;
using Club12.Services.DataAccessLayer;

namespace Club12.Services.Players.Implementation;

public class PlayerService : IPlayerService
{
    private readonly IGenericService<Player> _genericPlayerService;

    public PlayerService(
        IGenericService<Player> genericPlayerService
    )
    {
        _genericPlayerService = genericPlayerService;
    }

    public Player CreatePlayer(Player PlayerEntity)
    {
        _genericPlayerService.Insert(PlayerEntity);
        return PlayerEntity;
    }

    public Player? GetPlayerById(Guid PlayerId)
    {
        return _genericPlayerService.TryGet(PlayerId);
    }

    public void DeletePlayer(Player PlayerEntity)
    {
        _genericPlayerService.Delete(PlayerEntity);
    }

    public async Task<bool> UpdatePlayer(Player PlayerEntity)
    {
        try
        {
            await _genericPlayerService.UpdateAsync(PlayerEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
