using Application.DTOs.Abstract.Response;
using Application.DTOs.Team.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;
using Domain.Entities.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class TeamService(IUnitOfWork unitOfWork) : ITeamService
{
    private readonly ITeamRepository teamRepository = unitOfWork.TeamRepository;
    private readonly IStageTeamMatchRepository stageTeamMatchRepository = unitOfWork.StageTeamMatchRepository;
    public async Task<Team> CreateTeamAsync(Team teamEntity)
    {
        await teamRepository.AddAsync(teamEntity);
        return teamEntity;
    }

    public async Task<Team?> GetTeamByIdAsync(Guid teamId)
        => await teamRepository.GetByIdAsync(teamId, includes: [team => team.Players]);

    public async Task DeleteTeamAsync(Guid id)
        => await teamRepository.RemoveAsync(team => team.Id == id);

    public async Task UpdateTeamAsync(Team teamEntity)
        => await teamRepository.UpdateAsync(teamEntity);

    public async Task UpdateTeamsAsync(IEnumerable<Team> teams)
        => await teamRepository.UpdateRangeAsync(teams);

    public async Task<PaginatedResponse<Team>> GetAllTeamsAsync(GetTeamsFilteredRequest filter)
    {
        Expression<Func<Team, bool>> expression = QueryableExtensions.ConstructFilterExpression<Team, GetTeamsFilteredRequest>(filter);

        if (filter.StageId.HasValue)
        {
            Expression<Func<Team, bool>> stageExpression =
                team => team.StageTeamMatches.Any(stm => stm.StageId == filter.StageId.Value);

            expression = expression.And(stageExpression);
        }

        if (filter.TournamentId.HasValue)
        {
            Expression<Func<Team, bool>> tournamentExpression =
                team => team.TournamentId == filter.TournamentId.Value;
        }

        IEnumerable<Team> filteredTeams = await teamRepository.FindAsync(
            expression,
            includes: [team => team.Players, team => team.StageTeamMatches],
            filter: filter);

        int totalCount = await teamRepository.CountAsync(expression);

        return new PaginatedResponse<Team>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredTeams
        };
    }

    public async Task RegisterTeamsToTournamentAsync(Tournament tournament, List<Guid> teamIds)
    {
        List<Team> teamsToRegister = [.. await teamRepository.FindAsync(team => teamIds.Contains(team.Id)
            || team.TournamentId == tournament.Id)];

        teamsToRegister.AsParallel().ForAll(team =>
        {
            if (!teamIds.Contains(team.Id)) team.TournamentId = null;
            else if (team.TournamentId == tournament.Id) return;
            else team.Tournament = tournament;
        });

        await teamRepository.UpdateRangeAsync(teamsToRegister);

    }
}
