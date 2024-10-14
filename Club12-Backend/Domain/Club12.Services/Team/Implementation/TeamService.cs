using Club12.Entities.TeamEntity;
using Club12.Services.DataAccessLayer.GenericEntity;

namespace Club12.Services.Teams.Implementation;

public class TeamService(
    IGenericService<Team> genericTeamService
    ) : ITeamService
{
    public Team CreateTeam(Team teamEntity)
    {
        genericTeamService.Insert(teamEntity);
        return teamEntity;
    }

    public Team? GetTeamById(Guid teamId)
    {
        return genericTeamService.TryGet(teamId);
    }

    public void DeleteTeam(Team teamEntity)
    {
        genericTeamService.Delete(teamEntity);
    }

    public async Task<bool> UpdateTeam(Team teamEntity)
    {
        try
        {
            await genericTeamService.UpdateAsync(teamEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
