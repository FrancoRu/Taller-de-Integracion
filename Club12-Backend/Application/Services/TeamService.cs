using Application.DTOs.Abstract.Response;
using Application.DTOs.Team.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;
using Application.Utils.Helper.Slug;

using Domain.Entities.Models;

using LinqKit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Service class responsible for managing team-related operations within the application.
/// Provides methods for creating, retrieving, updating, deleting, and bulk updating teams,
/// as well as filtering teams and registering them to tournaments.
/// Utilizes repositories for data access and supports asynchronous operations.
/// </summary>
public class TeamService(IUnitOfWork unitOfWork) : ITeamService
{
    private readonly ITeamRepository _teamRepository = unitOfWork.TeamRepository;

    /// <summary>
    /// Creates a new team entity and persists it to the repository.
    /// </summary>
    /// <param name="teamEntity">The team entity to create.</param>
    /// <returns>The created team entity.</returns>
    public async Task<Team> CreateTeamAsync(Team teamEntity)
    {
        teamEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            teamEntity.Name,
            candidate => _teamRepository.ExistsAsync(team => team.Slug == candidate));

        await _teamRepository.AddAsync(teamEntity);
        return teamEntity;
    }

    /// <summary>
    /// Retrieves a team by its unique identifier, including its associated players.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <returns>The team entity if found; otherwise, null.</returns>
    public async Task<Team?> GetTeamByIdAsync(Guid teamId)
    {
        return await _teamRepository.GetByIdAsync(teamId, includes: [team => team.Players]);
    }

    /// <summary>
    /// Retrieves a team by its id or its slug, including its associated players.
    /// The value is treated as an id when it parses as a GUID, otherwise it is
    /// looked up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The team's GUID id or its slug.</param>
    /// <returns>The team entity if found; otherwise, null.</returns>
    public async Task<Team?> GetTeamByIdOrSlugAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out Guid teamId))
        {
            return await GetTeamByIdAsync(teamId);
        }

        IEnumerable<Team> matches = await _teamRepository.FindAsync(
            team => team.Slug == idOrSlug,
            includes: [team => team.Players]);

        return matches.FirstOrDefault();
    }

    /// <summary>
    /// Deletes a team entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the team to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteTeamAsync(Guid id)
    {
        await _teamRepository.RemoveAsync(team => team.Id == id);
    }

    /// <summary>
    /// Updates an existing team entity in the repository.
    /// </summary>
    /// <param name="teamEntity">The team entity with updated information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateTeamAsync(Team teamEntity)
    {
        await _teamRepository.UpdateAsync(teamEntity);
    }

    /// <summary>
    /// Updates a collection of team entities in bulk.
    /// </summary>
    /// <param name="teams">The collection of team entities to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateTeamsAsync(IEnumerable<Team> teams)
    {
        await _teamRepository.UpdateRangeAsync(teams);
    }

    /// <summary>
    /// Retrieves a paginated list of teams based on the provided filter criteria.
    /// Supports filtering by stage and tournament, and includes related players and stage team matches.
    /// </summary>
    /// <param name="filter">The filter criteria for retrieving teams.</param>
    /// <returns>A paginated response containing the filtered teams.</returns>
    public async Task<PaginatedResponse<Team>> GetAllTeamsAsync(GetTeamsFilteredRequest filter)
    {
        Expression<Func<Team, bool>> expression = QueryableExtensions.ConstructFilterExpression<Team, GetTeamsFilteredRequest>(filter);

        if (filter.StageId.HasValue)
        {
            Expression<Func<Team, bool>> stageExpression =
                team => team.StageTeamMatches.Any(stm => stm.StageId == filter.StageId.Value);

            expression = expression.And(stageExpression);
        }

        IEnumerable<Team> filteredTeams = await _teamRepository.FindAsync(
            expression,
            includes: [team => team.Players, team => team.StageTeamMatches],
            filter: filter);

        int totalCount = await _teamRepository.CountAsync(expression);

        return new PaginatedResponse<Team>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredTeams
        };
    }

    /// <summary>
    /// Registers a list of teams to a specified tournament.
    /// Updates the tournament association for each team based on the provided team IDs.
    /// - Teams whose IDs are not in <paramref name="teamIds"/> will be unassigned from the tournament.
    /// - Teams already assigned to the tournament are left unchanged.
    /// - Teams in <paramref name="teamIds"/> but not yet assigned will be associated with the tournament.
    /// The changes are persisted in bulk.
    /// </summary>
    /// <param name="tournament">The tournament entity to register teams to.</param>
    /// <param name="teamIds">A list of team IDs to be registered in the tournament.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RegisterTeamsToTournamentAsync(Tournament tournament, List<Guid> teamIds)
    {
        List<Team> teamsToRegister = [.. await _teamRepository.FindAsync(team => teamIds.Contains(team.Id)
            || team.TournamentId == tournament.Id)];

        teamsToRegister.AsParallel().ForAll(team =>
        {
            if (!teamIds.Contains(team.Id))
            {
                team.TournamentId = null;
            }
            else if (team.TournamentId != tournament.Id)
            {
                team.Tournament = tournament;
            }
        });

        await _teamRepository.UpdateRangeAsync(teamsToRegister);

    }
}
