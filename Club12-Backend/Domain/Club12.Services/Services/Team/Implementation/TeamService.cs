using Club12.Entities.TeamEntity;
using Club12.Services.DataAccessLayer.GenericEntity;
using Club12.Services.DTOs.Abstract;
using Club12.Services.DTOs.Team;
using Club12.Services.Utils.OrderFiltering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Club12.Services.Services.TeamService.Implementation;

public class TeamService(IGenericService<Team> genericTeamService) : ITeamService
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

    public async Task<PaginatedResponse<Team>> GetTeamsAsync(GetTeamsFilteredRequest filter)
    {
        Expression<Func<Team, bool>> expression = QueryableExtensions.ConstructFilterExpression<Team, GetTeamsFilteredRequest>(filter);
        IQueryable<Team> filteredTeams = genericTeamService.FilterByExpressionWithPagination(expression, filter).SortBy(filter);
        int totalCount = await genericTeamService.GetCountAsync(expression);

        return new PaginatedResponse<Team>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredTeams.ToListAsync()
        };
    }
}
