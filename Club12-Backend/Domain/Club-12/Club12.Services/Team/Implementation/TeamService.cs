using Club12.Entities.TeamEntity;
using Club12.Services.DataAccessLayer;

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

    public Team CreateTeam(Team teamEntity)
    {
        _genericTeamService.Insert(teamEntity);
        return teamEntity;
    }

    public Team? GetTeamById(Guid teamId)
    {
        return _genericTeamService.TryGet(teamId);
    }
}
