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
    private readonly IPlayerTeamRegistrationRepository _registrationRepository = unitOfWork.PlayerTeamRegistrationRepository;

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
    /// Retrieves a team by its unique identifier, with its roster (Players)
    /// scoped to one season.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="tournamentId">
    /// The season whose roster to attach; defaults to the team's own current
    /// TournamentId when omitted.
    /// </param>
    /// <returns>The team entity if found; otherwise, null.</returns>
    public async Task<Team?> GetTeamByIdAsync(Guid teamId, Guid? tournamentId = null)
    {
        Team? team = await _teamRepository.GetByIdAsync(teamId);

        if (team is null)
        {
            return null;
        }

        await AttachSeasonRostersAsync([team], tournamentId);
        return team;
    }

    /// <summary>
    /// Retrieves a team by its id or its slug, with its roster (Players)
    /// scoped to one season. The value is treated as an id when it parses as
    /// a GUID, otherwise it is looked up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The team's GUID id or its slug.</param>
    /// <param name="tournamentId">
    /// The season whose roster to attach; defaults to the team's own current
    /// TournamentId when omitted.
    /// </param>
    /// <returns>The team entity if found; otherwise, null.</returns>
    public async Task<Team?> GetTeamByIdOrSlugAsync(string idOrSlug, Guid? tournamentId = null)
    {
        if (Guid.TryParse(idOrSlug, out Guid teamId))
        {
            return await GetTeamByIdAsync(teamId, tournamentId);
        }

        IEnumerable<Team> matches = await _teamRepository.FindAsync(team => team.Slug == idOrSlug);
        Team? team = matches.FirstOrDefault();

        if (team is null)
        {
            return null;
        }

        await AttachSeasonRostersAsync([team], tournamentId);
        return team;
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

        List<Team> filteredTeams = [.. await _teamRepository.FindAsync(
            expression,
            includes: [team => team.StageTeamMatches],
            filter: filter,
            asSplitQuery: true)];

        await AttachSeasonRostersAsync(filteredTeams, filter.TournamentId);

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
    /// Populates each team's Players collection with the roster registered
    /// for one season, so a Team returned from this service never shows
    /// players from a season it no longer belongs to (the bug this replaces:
    /// Team.Players used to be the raw, permanent Player.TeamId FK
    /// collection, which kept showing a prior season's roster after the team
    /// was reassigned to a new tournament). Fetches every registration for
    /// the given teams in one batched query — avoids N+1 — then filters and
    /// groups in memory per team.
    /// </summary>
    /// <param name="teams">The teams to attach a roster to, mutated in place.</param>
    /// <param name="tournamentIdOverride">
    /// When set, every team's roster is scoped to this season instead of its
    /// own current TournamentId (used when the caller already filtered teams
    /// by a specific tournament, or explicitly asked to view a past season).
    /// </param>
    private async Task AttachSeasonRostersAsync(List<Team> teams, Guid? tournamentIdOverride)
    {
        List<Guid> teamIds = [.. teams.Select(t => t.Id)];

        if (teamIds.Count == 0)
        {
            return;
        }

        List<PlayerTeamRegistration> registrations = [.. await _registrationRepository.FindAsync(
            registration => teamIds.Contains(registration.TeamId),
            includes: [registration => registration.Player!])];

        ILookup<Guid, PlayerTeamRegistration> registrationsByTeam = registrations.ToLookup(r => r.TeamId);

        foreach (Team team in teams)
        {
            Guid? season = tournamentIdOverride ?? team.TournamentId;

            team.Players = season is null
                ? []
                : [.. registrationsByTeam[team.Id]
                    .Where(r => r.TournamentId == season.Value)
                    .Select(r => r.Player!)];
        }
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
