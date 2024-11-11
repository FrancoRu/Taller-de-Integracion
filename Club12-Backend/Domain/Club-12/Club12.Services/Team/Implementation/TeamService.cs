using Club12.Entities.TeamEntity;
using Club12.Services.DataAccessLayer.GenericEntity;

namespace Club12.Services.Teams.Implementation;

public class TeamService : ITeamService
{
    private readonly IGenericService<Team> _genericTeamService;

    public TeamService(
        IGenericService<Team> genericTeamService
    )
    {
        _genericTeamService = genericTeamService;
    }

    public Team CreateTeam(Team teamEntity, Guid userId)
    {
        _genericTeamService.Insert(teamEntity, userId);
        return teamEntity;
    }

    public Team? GetTeamById(Guid teamId)
    {
        return _genericTeamService.TryGet(teamId);
    }

    public void DeleteTeam(Team teamEntity)
    {
        _genericTeamService.Delete(teamEntity);
    }

    public async Task<bool> UpdateTeam(Team teamEntity, Guid userId)
    {
        try
        {
            await _genericTeamService.UpdateAsync(teamEntity, userId);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
