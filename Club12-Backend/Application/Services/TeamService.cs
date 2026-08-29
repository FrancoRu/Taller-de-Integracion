using Application.DTOs.Abstract.Response;
using Application.DTOs.Team.Request;
using Application.DTOs.Team.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Extensions;
using Application.Utils.Helper.Slug;
using Application.Utils.Helper.Standings;
using Application.Utils.Helper.TeamProfile;

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
public class TeamService(
    IUnitOfWork unitOfWork,
    IRosterCopyService rosterCopyService,
    IDivisionService divisionService,
    ITeamPointDeductionRepository pointDeductionRepository) : ITeamService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRosterCopyService _rosterCopyService = rosterCopyService;
    private readonly IDivisionService _divisionService = divisionService;
    private readonly ITeamRepository _teamRepository = unitOfWork.TeamRepository;
    private readonly IPlayerTeamRegistrationRepository _registrationRepository = unitOfWork.PlayerTeamRegistrationRepository;
    private readonly ITeamTournamentRegistrationRepository _tournamentRegistrationRepository = unitOfWork.TeamTournamentRegistrationRepository;
    private readonly IDivisionRepository _divisionRepository = unitOfWork.DivisionRepository;
    private readonly IMatchRepository _matchRepository = unitOfWork.MatchRepository;
    private readonly ITournamentRepository _tournamentRepository = unitOfWork.TournamentRepository;
    private readonly IPlayerSanctionRepository _sanctionRepository = unitOfWork.PlayerSanctionRepository;
    private readonly ITeamPointDeductionRepository _pointDeductionRepository = pointDeductionRepository;

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
    /// Deletes a team, guarding its competitive history. A team's identity
    /// persists across seasons (HU-99), so deletion is rare and blocked whenever
    /// the team carries history that removing it would orphan or silently
    /// cascade away: any match participation (home/visitor/winner — those FKs are
    /// NoAction and would raise an opaque database error), any sanction or point
    /// deduction (Restrict FKs), or any tournament registration. Mirrors the
    /// player delete guard. A team with none of these (e.g. a freshly created or
    /// fully unenrolled club) is still deletable — its empty roster and any
    /// season registrations cascade cleanly.
    /// </summary>
    /// <param name="id">The unique identifier of the team to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown (mapped to 409) when the team has match history, sanctions, point
    /// deductions or tournament registrations.
    /// </exception>
    public async Task DeleteTeamAsync(Guid id)
    {
        bool hasMatchHistory = await _matchRepository.ExistsAsync(
            match => match.HomeTeamId == id || match.VisitorTeamId == id || match.WinningTeamId == id);
        bool hasSanctions = await _sanctionRepository.ExistsAsync(sanction => sanction.TeamId == id);
        bool hasPointDeductions = await _pointDeductionRepository.ExistsAsync(deduction => deduction.TeamId == id);
        bool hasTournamentRegistrations = await _tournamentRegistrationRepository.ExistsAsync(
            registration => registration.TeamId == id);

        if (hasMatchHistory || hasSanctions || hasPointDeductions || hasTournamentRegistrations)
        {
            throw new InvalidOperationException(ErrorMessages.Team.HasHistoryCannotDelete);
        }

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
                        // File-backed habilitación (medical-records-storage-eligibility):
                        // a bool only, never the storage path — see Player.HasMedicalRecordFile.
                        r.Player!.HasMedicalRecordFile = PlayerTeamRegistration.IsStoredReference(r.MedicalRecordFileUrl);
                        // Surface the season-scoped dorsal (HU-54) onto the
                        // roster player (transient, not persisted).
                        r.Player!.JerseyNumber = r.JerseyNumber;
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

    /// <inheritdoc />
    public async Task<Team> EnrollTeamAsync(
        Tournament tournament,
        Guid? existingTeamId,
        string? newTeamName,
        Guid? copyRosterFromTournamentId)
    {
        // Fail fast with the same rejection RegisterTeamsToTournamentAsync uses
        // (HU-31, mapped to 409) before doing any work.
        if (tournament.Status != TournamentStatus.OpenForRegistration)
        {
            throw new InvalidOperationException(
                ErrorMessages.Tournament.StructuralEditNotAllowed(tournament.Status));
        }

        Guid enrolledTeamId = Guid.Empty;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (existingTeamId is Guid existing)
            {
                _ = await _teamRepository.GetByIdAsync(existing)
                    ?? throw new KeyNotFoundException(ErrorMessages.Team.NotFound(existing));

                // A team can be enrolled only once per tournament. The unique
                // (TeamId, TournamentId) index enforces it at the DB level, but
                // RegisterTeamsToTournamentAsync is an idempotent upsert that
                // would silently no-op instead of reporting the duplicate — so
                // surface a clean conflict here (mapped to 409).
                bool alreadyEnrolled = await _tournamentRegistrationRepository.ExistsAsync(
                    registration => registration.TeamId == existing
                        && registration.TournamentId == tournament.Id);

                if (alreadyEnrolled)
                {
                    throw new InvalidOperationException(
                        ErrorMessages.Team.AlreadyEnrolled(existing, tournament.Id));
                }

                enrolledTeamId = existing;
            }
            else
            {
                Team created = await CreateTeamAsync(BuildNewTeam(newTeamName!));
                enrolledTeamId = created.Id;
            }

            // Additive enroll: keep every team already registered to this
            // tournament and add the enrolled one. Passing only the new id would
            // make RegisterTeamsToTournamentAsync reconcile the whole tournament
            // and unregister the rest of the roster.
            List<Guid> targetTeamIds =
            [
                .. (await _tournamentRegistrationRepository.FindAsync(
                        registration => registration.TournamentId == tournament.Id))
                    .Select(registration => registration.TeamId),
                enrolledTeamId,
            ];

            await RegisterTeamsToTournamentAsync(tournament, targetTeamIds);

            // Copy roster is only meaningful for an existing team; the caller
            // guarantees copyRosterFromTournamentId is only set with existingTeamId.
            if (existingTeamId is Guid teamToCopyInto && copyRosterFromTournamentId is Guid sourceTournamentId)
            {
                await _rosterCopyService.CopyRosterAsync(
                    teamToCopyInto, sourceTournamentId, teamToCopyInto, tournament.Id);
            }
        });

        // Reload with the roster scoped to THIS tournament for the response.
        return (await GetTeamByIdAsync(enrolledTeamId, tournament.Id))!;
    }

    /// <inheritdoc />
    public async Task UnenrollTeamAsync(Tournament tournament, Guid teamId)
    {
        // Roster-editing window guard: teams may only be removed before the
        // tournament starts (OpenForRegistration or RegistrationClosed). Mapped
        // to 409 by the global handler.
        if (tournament.Status is not (TournamentStatus.OpenForRegistration or TournamentStatus.RegistrationClosed))
        {
            throw new InvalidOperationException(
                ErrorMessages.Tournament.UnenrollNotAllowed(tournament.Status));
        }

        bool isEnrolled = await _tournamentRegistrationRepository.ExistsAsync(
            registration => registration.TeamId == teamId && registration.TournamentId == tournament.Id);

        if (!isEnrolled)
        {
            throw new KeyNotFoundException(ErrorMessages.Team.NotEnrolled(teamId, tournament.Id));
        }

        // Stage ids that belong to this tournament (via its divisions), so only
        // THIS tournament's assignments for the team are removed.
        List<Guid> divisionIds = [.. (await _unitOfWork.DivisionRepository.FindAsync(
            division => division.TournamentId == tournament.Id)).Select(division => division.Id)];

        List<Guid> stageIds = divisionIds.Count == 0
            ? []
            : [.. (await _unitOfWork.StageRepository.FindAsync(
                stage => divisionIds.Contains(stage.DivisionId))).Select(stage => stage.Id)];

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // This tournament's season registration for the team.
            await _tournamentRegistrationRepository.RemoveAsync(
                registration => registration.TeamId == teamId && registration.TournamentId == tournament.Id);

            // This tournament's roster registrations for the team.
            await _registrationRepository.RemoveAsync(
                registration => registration.TeamId == teamId && registration.TournamentId == tournament.Id);

            // This tournament's stage assignments for the team.
            if (stageIds.Count > 0)
            {
                await _unitOfWork.StageTeamMatchRepository.RemoveAsync(
                    match => match.TeamId == teamId && stageIds.Contains(match.StageId));
            }

            // Clear the denormalized current-season pointer only when it points
            // at this tournament; a team pointing at another season is untouched.
            Team? team = await _teamRepository.GetByIdAsync(teamId);
            if (team is not null && team.TournamentId == tournament.Id)
            {
                team.TournamentId = null;
                await _teamRepository.UpdateAsync(team);
            }
        });
    }

    /// <summary>
    /// Builds a minimal brand-new team from just a name (HU-107 new-team path):
    /// the slug is generated by <see cref="CreateTeamAsync"/>, a placeholder
    /// three-letter code is derived from the name, and the logo/shirt are left
    /// empty for the admin to fill in later. The roster starts empty.
    /// </summary>
    private static Team BuildNewTeam(string name)
    {
        string alphanumeric = new([.. name.Where(char.IsLetterOrDigit)]);
        string threeLetterCode = (alphanumeric.Length >= 3
            ? alphanumeric[..3]
            : alphanumeric.PadRight(3, 'X')).ToUpperInvariant();

        return new Team
        {
            Name = name,
            Slug = string.Empty,
            ThreeLetterCode = threeLetterCode,
            LogoUrl = string.Empty,
            ShirtColor = string.Empty,
            TournamentId = null,
            Players = [],
            CreatedBy = AuditConstants.SystemUser,
        };
    }

    /// <inheritdoc />
    public async Task<TeamSummaryResponse?> GetTeamSummaryAsync(Guid teamId, Guid? tournamentId)
    {
        // No season context (team has no current tournament and none was
        // requested) means there is no standing to report.
        if (tournamentId is null)
        {
            return null;
        }

        // Prefer the team's real competitive division (its zone) over a
        // cross-division cup like "Copa Club 12": a team can belong to BOTH, and
        // the division is what matters for its standing — the cup is secondary.
        // Ordering cups last means the first group table that contains the team
        // is its zone whenever it has one.
        List<Division> divisions = [.. (await _divisionRepository.FindAsync(
            division => division.TournamentId == tournamentId.Value))
            .OrderBy(division => division.IsCrossDivisionCup)];

        foreach (Division division in divisions)
        {
            // Reuse the canonical standings computation (per group) instead of
            // recomputing it here — a regular zone yields one group, a
            // cross-division cup one per internal group, and the team's rank is
            // always within its own group's table.
            List<GroupStandings> groups = await _divisionService.GetGroupStandingsByDivisionIdAsync(division.Id);

            var located = groups
                .Select(group => new
                {
                    Group = group,
                    Index = group.Positions.FindIndex(position => position.TeamId == teamId),
                })
                .FirstOrDefault(candidate => candidate.Index >= 0);

            if (located is not null)
            {
                Position row = located.Group.Positions[located.Index];

                return new TeamSummaryResponse
                {
                    DivisionId = division.Id,
                    DivisionName = division.Name,
                    Position = located.Index + 1,
                    TotalTeams = located.Group.Positions.Count,
                    Played = row.MatchesPlayed,
                    Wins = row.Wins,
                    Losses = row.Losses,
                    PointsFor = row.PointsFor,
                    PointsAgainst = row.PointsAgainst,
                    PointsDifference = row.PointsDifference,
                    Points = row.Points,
                };
            }
        }

        // The team plays in no group-stage table for this tournament
        // (playoff-only, unassigned, or no finished matches yet).
        return null;
    }

    /// <inheritdoc />
    public async Task<List<TeamMatchResponse>> GetTeamMatchesAsync(Guid teamId, Guid? tournamentId)
    {
        if (tournamentId is null)
        {
            return [];
        }

        // Read the team's matches (either side) scoped to the tournament via its
        // stage → division → tournament chain. Queried directly rather than
        // through the paginated match filter, which has no "by team" predicate
        // and would cap a tournament-wide read at one page.
        List<Match> matches = [.. await _matchRepository.FindAsync(
            match => (match.HomeTeamId == teamId || match.VisitorTeamId == teamId)
                && match.Stage.Division.TournamentId == tournamentId.Value,
            includes: [match => match.HomeTeam!, match => match.VisitorTeam!, match => match.Venue!])];

        return [.. matches
            .OrderBy(match => match.MatchDate)
            .Select(match => TeamMatchMapper.Map(match, teamId))];
    }

    /// <inheritdoc />
    public async Task<List<TeamParticipationResponse>> GetTeamParticipationsAsync(Guid teamId, Guid? currentTournamentId)
    {
        List<TeamTournamentRegistration> registrations = [.. await _tournamentRegistrationRepository.FindAsync(
            registration => registration.TeamId == teamId)];

        if (registrations.Count == 0)
        {
            return [];
        }

        List<Guid> tournamentIds = [.. registrations.Select(registration => registration.TournamentId).Distinct()];

        List<Tournament> tournaments = [.. await _tournamentRepository.FindAsync(
            tournament => tournamentIds.Contains(tournament.Id),
            includes: [tournament => tournament.Season!])];

        return [.. tournaments
            .Select(tournament => new TeamParticipationResponse
            {
                TournamentId = tournament.Id,
                TournamentName = tournament.Name,
                TournamentSlug = tournament.Slug,
                Category = tournament.Category.ToString(),
                SeasonId = tournament.SeasonId,
                SeasonName = tournament.Season?.Name,
                Year = tournament.Season?.Year,
                IsCurrent = currentTournamentId.HasValue && tournament.Id == currentTournamentId.Value,
            })
            // Newest first: known years descending, participations without a
            // season year last, ties broken by tournament name.
            .OrderByDescending(participation => participation.Year.HasValue)
            .ThenByDescending(participation => participation.Year)
            .ThenBy(participation => participation.TournamentName, StringComparer.OrdinalIgnoreCase)];
    }
}
