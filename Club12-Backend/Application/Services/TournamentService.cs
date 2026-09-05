using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.DTOs.Stage.Request;
using Application.DTOs.Tournament.Request;
using Application.DTOs.Tournament.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Constants.Pagination;
using Application.Utils.Extensions;
using Application.Utils.Helper.Slug;
using Application.Utils.Helper.Tournament;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Owns the tournament lifecycle: structural creation, status transitions, and their cascades.
/// </summary>
public class TournamentService(
    ITournamentRepository tournamentRepository,
    IStageService stageService,
    IMatchService matchService,
    IAuditService auditService,
    IDivisionService divisionService,
    IUnitOfWork unitOfWork) : ITournamentService
{
    public async Task<Tournament> CreateTournamentAsync(Tournament tournamentEntity)
    {
        tournamentEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            tournamentEntity.Name,
            candidate => tournamentRepository.ExistsAsync(tournament => tournament.Slug == candidate));

        await tournamentRepository.AddAsync(tournamentEntity);
        return tournamentEntity;
    }

    /// <inheritdoc/>
    public async Task<Tournament> CreateFullTournamentAsync(CreateFullTournamentRequest request)
    {
        // Created OpenForRegistration, since divisions and stages can only be built while the tournament is in that status per the structural-edit guard, and structural creation is part of creation.
        Tournament tournament = new()
        {
            Name = request.Name,
            Description = request.Description,
            Slug = null!,
            TeamRegistrationDeadline = request.TeamRegistrationDeadline,
            StartDate = request.StartDate,
            Category = request.Category,
            SeasonId = request.SeasonId,
            Status = TournamentStatus.OpenForRegistration,
            Divisions = [],
            Teams = [],
            CreatedBy = AuditConstants.SystemUser,
        };

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await CreateTournamentAsync(tournament);

            foreach (CreateFullDivisionRequest divisionRequest in request.Divisions)
            {
                await CreateDivisionWithStagesAsync(tournament, divisionRequest);
            }
        });

        return await GetTournamentByIdAsync(tournament.Id) ?? tournament;
    }

    /// <inheritdoc/>
    public async Task<Division> AddFullDivisionAsync(Tournament tournament, CreateFullDivisionRequest divisionRequest)
    {
        Division division = null!;

        // Wrapped in its own transaction, mirroring the OpenForRegistration guard DivisionService.CreateDivisionAsync already enforces, so a division added to an existing tournament gets the same all-or-nothing guarantee a wizard-created one gets.
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            division = await CreateDivisionWithStagesAsync(tournament, divisionRequest);
        });

        return division;
    }

    /// <summary>
    /// Builds and persists one division and its stages from a CreateFullDivisionRequest.
    /// </summary>
    private async Task<Division> CreateDivisionWithStagesAsync(
        Tournament tournament, CreateFullDivisionRequest divisionRequest)
    {
        Division division = new()
        {
            Name = divisionRequest.Name,
            Slug = null!,
            Tournament = tournament,
            TournamentId = tournament.Id,
            Category = divisionRequest.Category,
            IsCrossDivisionCup = divisionRequest.IsCrossDivisionCup,
            PointsForWin = divisionRequest.PointsForWin,
            PointsForLoss = divisionRequest.PointsForLoss,
            QualifiersPerGroup = divisionRequest.QualifiersPerGroup,
            Stages = [],
            CreatedBy = AuditConstants.SystemUser,
            PlayoffMappings = (divisionRequest.PlayoffMappings ?? [])
                .Select(mapping => new DivisionPlayoffMapping
                {
                    FromPosition = mapping.FromPosition,
                    ToPosition = mapping.ToPosition,
                    Destination = mapping.Destination,
                    CreatedBy = AuditConstants.SystemUser,
                })
                .ToList(),
        };

        await divisionService.CreateDivisionAsync(division);

        foreach (CreateFullStageRequest stageRequest in divisionRequest.Stages)
        {
            Stage stage = new()
            {
                Name = stageRequest.Name,
                Slug = null!,
                Description = stageRequest.Description,
                StageType = stageRequest.StageType,
                IsActive = stageRequest.IsActive ?? true,
                IsElimination = stageRequest.IsElimination ?? (stageRequest.StageType != StageType.Group),
                StartDate = stageRequest.StartDate,
                EndDate = stageRequest.EndDate,
                DivisionId = division.Id,
                Division = division,
                BracketName = stageRequest.BracketName,
                BestOf = stageRequest.BestOf,
                RoundRobinLegs = stageRequest.RoundRobinLegs,
                Matches = [],
                CreatedBy = AuditConstants.SystemUser,
            };

            await stageService.CreateStageAsync(stage);
        }

        return division;
    }

    public async Task<Tournament?> GetTournamentByIdAsync(Guid tournamentId)
    {
        return await tournamentRepository.GetByIdAsync(
            tournamentId,
            includes: [tournament => tournament.Divisions, tournament => tournament.Season!]);
    }

    /// <summary>
    /// Retrieves a tournament by id or slug, auto-detecting which one was passed via GUID parsing.
    /// </summary>
    /// <param name="idOrSlug">The tournament's GUID id or its slug.</param>
    /// <returns>The matching tournament, or null if not found.</returns>
    public async Task<Tournament?> GetTournamentByIdOrSlugAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out Guid tournamentId))
        {
            return await GetTournamentByIdAsync(tournamentId);
        }

        IEnumerable<Tournament> matches = await tournamentRepository.FindAsync(
            tournament => tournament.Slug == idOrSlug,
            includes: [tournament => tournament.Divisions, tournament => tournament.Season!]);

        return matches.FirstOrDefault();
    }

    /// <summary>
    /// Deletes a tournament, blocking the delete once it has real history.
    /// </summary>
    /// <param name="id">The id of the tournament to delete.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown as a 409 when the tournament has already started or has played matches.
    /// </exception>
    public async Task DeleteTournamentAsync(Guid id)
    {
        Tournament? tournament = await tournamentRepository.GetByIdAsync(id);

        // Nothing to delete, so keep the historical idempotent no-op behavior.
        if (tournament is null)
        {
            return;
        }

        bool hasStarted = tournament.Status is TournamentStatus.Ongoing or TournamentStatus.Finished;
        bool hasPlayedMatches = await unitOfWork.MatchRepository.ExistsAsync(
            match => match.IsFinished && match.Stage.Division.TournamentId == id);

        if (hasStarted || hasPlayedMatches)
        {
            throw new InvalidOperationException(ErrorMessages.Tournament.HasHistoryCannotDelete);
        }

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Clear the denormalized current-season pointer of any team pointing at this tournament before the cascade delete, since Team to Tournament is NoAction and a still-set pointer would abort the delete with an opaque FK error.
            List<Team> pointingTeams = [.. await unitOfWork.TeamRepository.FindAsync(
                team => team.TournamentId == id)];

            foreach (Team team in pointingTeams)
            {
                team.TournamentId = null;
            }

            if (pointingTeams.Count > 0)
            {
                await unitOfWork.TeamRepository.UpdateRangeAsync(pointingTeams);
            }

            await tournamentRepository.RemoveAsync(tournament => tournament.Id == id);
        });
    }

    public async Task UpdateTournamentAsync(Tournament tournamentEntity)
    {
        await tournamentRepository.UpdateAsync(tournamentEntity);
    }

    /// <summary>
    /// Drives the tournament status state machine, applying whichever cascade the new status implies.
    /// </summary>
    /// <param name="tournamentId">The tournament to transition.</param>
    /// <param name="newStatus">The status to transition to.</param>
    /// <exception cref="KeyNotFoundException">Thrown when the tournament does not exist.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown as a 409 when the transition is invalid, or when moving to Ongoing while the
    /// tournament fails the completability check.
    /// </exception>
    public async Task ChangeStatusAsync(Guid tournamentId, TournamentStatus newStatus)
    {
        Tournament tournament = await tournamentRepository.GetByIdAsync(
            tournamentId, includes: [tournament => tournament.Divisions])
            ?? throw new KeyNotFoundException(ErrorMessages.Tournament.NotFound(tournamentId));

        if (tournament.Status == newStatus)
        {
            return;
        }

        if (!TournamentStatusTransitions.IsValidTransition(tournament.Status, newStatus))
        {
            throw new InvalidOperationException(
                ErrorMessages.Tournament.InvalidStatusTransition(tournament.Status, newStatus));
        }

        // Fixture generation runs before the status is committed, so a generation failure leaves the tournament in its prior editable status instead of stranding it in Ongoing with a half-built fixture.
        if (newStatus == TournamentStatus.Ongoing)
        {
            // The completability guard runs on the loaded graph before generating any fixture, so a violation aborts the transition, mapped to 409, before any half-built fixture is left.
            IReadOnlyList<CompletabilityIssue> issues = await EvaluateCompletabilityAsync(tournamentId);
            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    ErrorMessages.Tournament.NotCompletable(SummarizeIssues(issues)));
            }

            // Wrapped in a transaction so a failure partway through one division's stages rolls back every match already generated in this attempt too, leaving a clean slate to retry from.
            await unitOfWork.ExecuteInTransactionAsync(() => GenerateFixtureAsync(tournament));
        }

        // Moving Ongoing to RegistrationClosed reopens the assignment phase, so the fixture generated at start is torn down while team-to-zone assignments are kept, letting the organizer re-start to rebuild the fixture.
        if (tournament.Status == TournamentStatus.Ongoing
            && newStatus == TournamentStatus.RegistrationClosed)
        {
            await TeardownFixtureAsync(tournament);
        }

        // Canceling a tournament, or force-closing it as Finished while matches are still pending, must not leave still-to-be-played fixtures dangling under a dead tournament, so the leftover matches need their own terminal state too.
        if (newStatus is TournamentStatus.Canceled or TournamentStatus.Finished)
        {
            await CancelPendingMatchesAsync(tournamentId);
        }

        TournamentStatus previousStatus = tournament.Status;
        tournament.Status = newStatus;
        await tournamentRepository.UpdateAsync(tournament);

        // Record the sensitive status change for traceability.
        await auditService.LogAsync(
            AuditAction.TournamentStatusChange,
            targetType: nameof(Tournament),
            targetId: tournamentId.ToString(),
            targetName: tournament.Name,
            detail: $"{previousStatus.ToSpanishLabel()} → {newStatus.ToSpanishLabel()}");
    }

    /// <summary>
    /// Generates the automated fixture for every stage of every division in the tournament.
    /// </summary>
    private async Task GenerateFixtureAsync(Tournament tournament)
    {
        foreach (Division division in tournament.Divisions)
        {
            PaginatedResponse<Stage> stages = await stageService.GetAllStagesAsync(new GetStagesFilteredRequest
            {
                DivisionId = division.Id,
                PageSize = PaginationDefaults.MaxPageSize,
            });

            foreach (Guid stageId in stages.Items.Select(stage => stage.Id))
            {
                PaginatedResponse<Match> existingMatches = await matchService.GetAllMatchesAsync(new GetMatchesFilteredRequest
                {
                    StageId = stageId,
                    PageSize = 1,
                });

                if (existingMatches.TotalCount > 0)
                {
                    continue;
                }

                await matchService.CreateAutomatedMatchesAsync(stageId);
            }
        }
    }

    /// <summary>
    /// Deletes every generated match and best-of-N series across the tournament, back to a fixture-less state.
    /// </summary>
    private async Task TeardownFixtureAsync(Tournament tournament)
    {
        List<Guid> stageIds = [];

        foreach (Division division in tournament.Divisions)
        {
            PaginatedResponse<Stage> stages = await stageService.GetAllStagesAsync(new GetStagesFilteredRequest
            {
                DivisionId = division.Id,
                PageSize = PaginationDefaults.MaxPageSize,
            });

            stageIds.AddRange(stages.Items.Select(stage => stage.Id));
        }

        if (stageIds.Count == 0)
        {
            return;
        }

        // Reverting is only safe while nothing has been played, since a finished match carries results and scorers that tearing down the fixture would lose.
        int playedMatches = await unitOfWork.MatchRepository.CountAsync(
            match => stageIds.Contains(match.StageId) && match.IsFinished);

        if (playedMatches > 0)
        {
            throw new InvalidOperationException(ErrorMessages.Tournament.CannotRevertWithPlayedMatches);
        }

        // Matches are removed first, since they may reference a series, then the series.
        await unitOfWork.MatchRepository.RemoveAsync(match => stageIds.Contains(match.StageId));
        await unitOfWork.MatchSeriesRepository.RemoveAsync(series => stageIds.Contains(series.StageId));
    }

    /// <summary>
    /// Marks every not-yet-finished match of a tournament as MatchStatus.Canceled.
    /// </summary>
    private async Task CancelPendingMatchesAsync(Guid tournamentId)
    {
        List<Match> pendingMatches = [.. await unitOfWork.MatchRepository.FindAsync(
            match => !match.IsFinished
                && match.Status != MatchStatus.Canceled
                && match.Stage.Division.TournamentId == tournamentId)];

        if (pendingMatches.Count == 0)
        {
            return;
        }

        foreach (Match match in pendingMatches)
        {
            match.Status = MatchStatus.Canceled;
        }

        await unitOfWork.MatchRepository.UpdateRangeAsync(pendingMatches);
    }

    /// <inheritdoc/>
    public async Task<TournamentCompletabilityResponse> GetCompletabilityAsync(Guid tournamentId)
    {
        IReadOnlyList<CompletabilityIssue> issues = await EvaluateCompletabilityAsync(tournamentId);

        return new TournamentCompletabilityResponse
        {
            CanStart = issues.Count == 0,
            Issues = [.. issues],
        };
    }

    /// <summary>
    /// Loads the graph the completability validator needs and runs it against a throwaway tournament.
    /// </summary>
    private async Task<IReadOnlyList<CompletabilityIssue>> EvaluateCompletabilityAsync(Guid tournamentId)
    {
        bool exists = await tournamentRepository.ExistsAsync(tournament => tournament.Id == tournamentId);
        if (!exists)
        {
            throw new KeyNotFoundException(ErrorMessages.Tournament.NotFound(tournamentId));
        }

        List<Division> divisions = [.. await unitOfWork.DivisionRepository.FindAsync(
            division => division.TournamentId == tournamentId,
            includes: [division => division.Stages, division => division.PlayoffMappings],
            asSplitQuery: true)];

        List<Guid> stageIds = [.. divisions.SelectMany(division => division.Stages).Select(stage => stage.Id)];

        List<StageTeamMatch> stageTeamMatches = stageIds.Count == 0
            ? []
            : [.. await unitOfWork.StageTeamMatchRepository.FindAsync(
                match => stageIds.Contains(match.StageId),
                includes: [match => match.Team!])];

        ILookup<Guid, StageTeamMatch> matchesByStage = stageTeamMatches.ToLookup(match => match.StageId);
        foreach (Stage stage in divisions.SelectMany(division => division.Stages))
        {
            stage.StageTeamMatches = [.. matchesByStage[stage.Id]];
        }

        List<TeamTournamentRegistration> registrations =
            [.. await unitOfWork.TeamTournamentRegistrationRepository.FindAsync(
                registration => registration.TournamentId == tournamentId,
                includes: [registration => registration.Team!])];

        // Only HABILITADO players count toward the minimum, per the owner's "no se puede arrancar torneos sin al menos 4 habilitados" rule, since a registration that is merely on the roster but Pending, Rejected, or Approved with no real stored file could never legally play.
        Dictionary<Guid, int> habilitadoPlayerCountsByTeam = (await unitOfWork.PlayerTeamRegistrationRepository.FindAsync(
                registration => registration.TournamentId == tournamentId))
            .Where(registration => registration.IsHabilitado)
            .GroupBy(registration => registration.TeamId)
            .ToDictionary(group => group.Key, group => group.Count());

        Tournament graph = new()
        {
            Name = string.Empty,
            Description = string.Empty,
            Slug = string.Empty,
            TeamRegistrationDeadline = default,
            StartDate = default,
            Divisions = divisions,
            Teams = [],
            CreatedBy = AuditConstants.SystemUser,
        };

        return TournamentCompletabilityValidator.Validate(graph, registrations, habilitadoPlayerCountsByTeam);
    }

    /// <summary>
    /// Renders the structured completability issues into a compact summary for the blocking exception.
    /// </summary>
    private static string SummarizeIssues(IReadOnlyList<CompletabilityIssue> issues)
    {
        return string.Join("; ", issues.Select(issue => issue.Code switch
        {
            CompletabilityIssueCodes.ZoneTooFewTeams =>
                $"zone '{issue.DivisionName}' has {issue.AssignedTeams} team(s), needs at least {TournamentCompletabilityValidator.MinTeamsPerZone}",
            CompletabilityIssueCodes.TeamNotAssigned =>
                $"team '{issue.TeamName}' is not assigned to any zone",
            CompletabilityIssueCodes.TeamInMultipleZones =>
                $"team '{issue.TeamName}' is assigned to more than one zone",
            CompletabilityIssueCodes.PlayoffRangeExceedsTeams =>
                $"zone '{issue.DivisionName}' has a playoff range starting at position {issue.FromPosition} but only {issue.AssignedTeams} team(s) assigned",
            CompletabilityIssueCodes.CrossCupGroupTooFewTeams =>
                $"cross-cup '{issue.DivisionName}' group has {issue.AssignedTeams} team(s), needs at least {TournamentCompletabilityValidator.MinTeamsPerZone}",
            CompletabilityIssueCodes.TeamTooFewPlayers =>
                $"team '{issue.TeamName}' has {issue.PlayerCount} habilitado player(s), needs at least {TournamentCompletabilityValidator.MinPlayersPerTeam}",
            _ => issue.Code,
        }));
    }

    public async Task<PaginatedResponse<Tournament>> GetAllTournamentsAsync(GetTournamentsFilteredRequest filter)
    {
        Expression<Func<Tournament, bool>> expression = QueryableExtensions.ConstructFilterExpression<Tournament, GetTournamentsFilteredRequest>(filter);
        IEnumerable<Tournament> filteredTournaments = await tournamentRepository.FindAsync(expression, filter: filter);
        int totalCount = await tournamentRepository.CountAsync(expression);

        return new PaginatedResponse<Tournament>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredTournaments
        };
    }
}
