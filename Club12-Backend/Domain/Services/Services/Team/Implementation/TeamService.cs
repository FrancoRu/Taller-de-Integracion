using Entities.DTOs.Abstract;
using Entities.DTOs.Team;
using Entities.Models.TeamEntity;
using Microsoft.EntityFrameworkCore;
using Services.DataAccessLayer.GenericEntity;
using Services.Services.TeamService;
using Services.Utils.OrderFiltering;
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
        return genericTeamService.FilterByExpression(team => team.Id == teamId)
                                 .Include(team => team.Players)
                                 .FirstOrDefault();
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
        IQueryable<Team> filteredTeams = genericTeamService.FilterByExpressionWithPagination(expression, filter, team => team.Players).SortBy(filter);
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
