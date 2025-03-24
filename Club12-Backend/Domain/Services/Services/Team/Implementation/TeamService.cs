using Entities.DTOs.Abstract;
using Entities.DTOs.Team;
using Entities.Models.TeamEntity;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Services.TeamService;
using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

namespace Club12.Services.Services.TeamService.Implementation;

public class TeamService(IGenericService<Team> _genericTeamService) : ITeamService
{
    public async Task<Team> CreateTeamAsync(Team teamEntity)
    {
        await _genericTeamService.InsertAsync(teamEntity);
        return teamEntity;
    }

    public async Task<Team?> GetTeamByIdAsync(Guid teamId)
    {
        return await _genericTeamService.FilterByExpression(team => team.Id == teamId)
                                        .Include(team => team.Players)
                                        .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteTeamAsync(Team teamEntity)
    {
        try
        {
            await _genericTeamService.DeleteAsync(teamEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateTeamAsync(Team teamEntity)
    {
        try
        {
            await _genericTeamService.UpdateAsync(teamEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateTeamsAsync(IEnumerable<Team> teams)
    {
        try
        {
            await _genericTeamService.UpdateRangeAsync(teams);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PaginatedResponse<Team>> GetAllTeamsAsync(GetTeamsFilteredRequest filter)
    {
        Expression<Func<Team, bool>> expression = QueryableExtensions.ConstructFilterExpression<Team, GetTeamsFilteredRequest>(filter);
        IQueryable<Team> filteredTeams = _genericTeamService.FilterByExpressionWithPagination(expression, filter, team => team.Players).SortBy(filter);
        int totalCount = await _genericTeamService.GetCountAsync(expression);

        return new PaginatedResponse<Team>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredTeams.ToListAsync()
        };
    }
}
