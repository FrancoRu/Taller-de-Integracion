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
        // Created OpenForRegistration: divisions/stages can only be built while
        // the tournament is in that status (HU-31 structural-edit guard), and
        // structural creation is part of creation. Teams register, registration
        // closes, and the fixture is generated later when the tournament starts
        // (the canonical transition to Ongoing, HU-108).
        Tournament tournament = new()
        {
            Name = request.Name,
            Description = request.Description,
            Slug = null!,
            TeamRegistrationDeadline = request.TeamRegistrationDeadline,
            StartDate = request.StartDate,
            Category = request.Category,
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
            }
        });

        return await GetTournamentByIdAsync(tournament.Id) ?? tournament;
    }

    public async Task<Tournament?> GetTournamentByIdAsync(Guid tournamentId)
    {
        return await tournamentRepository.GetByIdAsync(
            tournamentId,
            includes: [tournament => tournament.Divisions, tournament => tournament.Season!]);
    }

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

    public async Task DeleteTournamentAsync(Guid id)
    {
        await tournamentRepository.RemoveAsync(tournament => tournament.Id == id);
    }

    public async Task UpdateTournamentAsync(Tournament tournamentEntity)
    {
        await tournamentRepository.UpdateAsync(tournamentEntity);
    }

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

        // HU-108: starting the tournament is the canonical fixture trigger.
        // After registration closes teams are assigned to divisions; only when
        // the tournament moves to Ongoing do we generate the matches for every
        // division's stages exactly once (see GenerateFixtureAsync for the
        // idempotency guard). Done BEFORE the status is committed so a
        // generation failure leaves the tournament in its prior (still
        // RegistrationClosed, editable) status instead of stranding it in
        // Ongoing with a half-built fixture.
        if (newStatus == TournamentStatus.Ongoing)
        {
            // HU-109: a tournament that cannot be completed must never be
            // started. Run the completability guard on the loaded graph BEFORE
            // generating any fixture; a violation aborts the transition (mapped
            // to 409 by the global handler) so no half-built fixture is left and
            // the tournament stays in its prior, editable status.
            IReadOnlyList<CompletabilityIssue> issues = await EvaluateCompletabilityAsync(tournamentId);
            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    ErrorMessages.Tournament.NotCompletable(SummarizeIssues(issues)));
            }

            await GenerateFixtureAsync(tournament);
        }

        TournamentStatus previousStatus = tournament.Status;
        tournament.Status = newStatus;
        await tournamentRepository.UpdateAsync(tournament);

        // HU-101: record the sensitive status change for traceability.
        await auditService.LogAsync(
            AuditAction.TournamentStatusChange,
            targetType: nameof(Tournament),
            targetId: tournamentId.ToString(),
            detail: $"{previousStatus.ToSpanishLabel()} → {newStatus.ToSpanishLabel()}");
    }

    /// <summary>
    /// Generates the automated fixture (matches) for every stage of every
    /// division in the tournament, reusing the same
    /// <see cref="IMatchService.CreateAutomatedMatchesAsync"/> path the manual
    /// generation endpoint uses. Idempotent: a stage that already has matches
    /// is skipped, so re-running never double-generates.
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
    /// Loads the graph the completability validator needs (divisions with their
    /// stages, each stage's team assignments, and playoff mappings) plus the
    /// tournament's enrolled-team registrations, then runs the single
    /// source-of-truth validator (HU-109). The graph is loaded piecewise (the
    /// generic repository cannot express nested includes) into a throwaway
    /// tournament so the validator receives a fully-loaded aggregate without the
    /// status-mutation load being marked modified.
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

        return TournamentCompletabilityValidator.Validate(graph, registrations);
    }

    /// <summary>
    /// Renders the structured completability issues into a compact, English
    /// summary used in the InvalidOperationException that blocks the start.
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
