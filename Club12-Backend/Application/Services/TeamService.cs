using Application.DTOs.Abstract.Response;
using Application.DTOs.Team.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Extensions;
using Application.Utils.Helper.Slug;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

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
    private readonly ITeamTournamentRegistrationRepository _tournamentRegistrationRepository = unitOfWork.TeamTournamentRegistrationRepository;

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
        // TournamentId is suppressed from the auto-generated FK-equality: a
        // team's participation in a tournament is the TeamTournamentRegistration
        // join, not the denormalized Team.TournamentId "current-season" pointer,
        // so a team appears for every season it is registered in (including past
        // ones whose pointer has since moved elsewhere).
        Expression<Func<Team, bool>> expression = QueryableExtensions.ConstructFilterExpression<Team, GetTeamsFilteredRequest>(
            filter, nameof(GetTeamsFilteredRequest.TournamentId));

        if (filter.StageId.HasValue)
        {
            Expression<Func<Team, bool>> stageExpression =
                team => team.StageTeamMatches.Any(stm => stm.StageId == filter.StageId.Value);

            expression = expression.And(stageExpression);
        }

        if (filter.TournamentId.HasValue)
        {
            Expression<Func<Team, bool>> tournamentExpression =
                team => team.TeamTournamentRegistrations.Any(r => r.TournamentId == filter.TournamentId.Value);

            expression = expression.And(tournamentExpression);
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
                    .Select(r =>
                    {
                        // Surface the season-scoped eligibility onto the roster
                        // player (transient, not persisted) so responses expose
                        // habilitado/not-habilitado per player (HU-57/HU-62).
                        r.Player!.MedicalRecordStatus = r.MedicalRecordStatus;
                        return r.Player!;
                    })];
        }
    }

    /// <summary>
    /// Reconciles a tournament's team roster by UPSERTING
    /// <see cref="TeamTournamentRegistration"/> rows scoped to
    /// <paramref name="tournament"/> ONLY — the join table is the source of
    /// truth for season-scoped participation, so a team's registrations in
    /// other tournaments are never touched and history is never erased
    /// (mirrors <see cref="PlayerService"/>'s EnsureRegistrationAsync upsert).
    /// - Teams in <paramref name="teamIds"/> get or keep a registration to
    ///   this tournament (existing rows are left as-is; the unique
    ///   (TeamId, TournamentId) index guarantees no duplicates).
    /// - Teams currently registered to THIS tournament but absent from
    ///   <paramref name="teamIds"/> have only THIS tournament's registration
    ///   removed; their rows for other tournaments survive.
    /// - An empty <paramref name="teamIds"/> clears only this tournament's
    ///   members, leaving every team's other-tournament history intact.
    /// <see cref="Team.TournamentId"/> is kept in sync as a denormalized
    /// "current-season" pointer for backward compatibility, but it is not the
    /// source of truth.
    /// </summary>
    /// <param name="tournament">The tournament entity to register teams to.</param>
    /// <param name="teamIds">A list of team IDs to be registered in the tournament.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RegisterTeamsToTournamentAsync(Tournament tournament, List<Guid> teamIds)
    {
        // Structural guard (HU-31): team registrations may only be added or
        // removed while the tournament is OpenForRegistration. Once
        // registration closes the roster is frozen.
        if (tournament.Status != TournamentStatus.OpenForRegistration)
        {
            throw new InvalidOperationException(
                ErrorMessages.Tournament.StructuralEditNotAllowed(tournament.Status));
        }

        HashSet<Guid> targetTeamIds = [.. teamIds];

        List<TeamTournamentRegistration> existingRegistrations =
            [.. await _tournamentRegistrationRepository.FindAsync(
                registration => registration.TournamentId == tournament.Id)];

        // Remove ONLY this tournament's registrations for teams no longer in
        // the list — scoped by registration Id so no other season is affected.
        List<Guid> registrationIdsToRemove = [.. existingRegistrations
            .Where(registration => !targetTeamIds.Contains(registration.TeamId))
            .Select(registration => registration.Id)];

        if (registrationIdsToRemove.Count > 0)
        {
            await _tournamentRegistrationRepository.RemoveAsync(
                registration => registrationIdsToRemove.Contains(registration.Id));
        }

        // Add a registration for every listed team that does not already have
        // one for this tournament (upsert; existing rows are kept untouched).
        HashSet<Guid> alreadyRegisteredTeamIds = [.. existingRegistrations.Select(registration => registration.TeamId)];

        List<TeamTournamentRegistration> registrationsToAdd = [.. targetTeamIds
            .Where(teamId => !alreadyRegisteredTeamIds.Contains(teamId))
            .Select(teamId => new TeamTournamentRegistration
            {
                Id = Guid.Empty,
                TeamId = teamId,
                TournamentId = tournament.Id,
                DateCreated = DateTime.UtcNow,
                CreatedBy = tournament.UpdatedBy ?? tournament.CreatedBy ?? AuditConstants.SystemUser,
            })];

        if (registrationsToAdd.Count > 0)
        {
            await _tournamentRegistrationRepository.AddRangeAsync(registrationsToAdd);
        }

        // Keep the denormalized current-season pointer in sync: listed teams
        // point at this tournament, dropped teams currently pointing here are
        // cleared. Teams pointing at a different tournament are left alone.
        List<Team> affectedTeams = [.. await _teamRepository.FindAsync(team => teamIds.Contains(team.Id)
            || team.TournamentId == tournament.Id)];

        foreach (Team team in affectedTeams)
        {
            team.TournamentId = targetTeamIds.Contains(team.Id) ? tournament.Id : null;
        }

        if (affectedTeams.Count > 0)
        {
            await _teamRepository.UpdateRangeAsync(affectedTeams);
        }
    }
}
