using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.DTOs.Stage.Request;
using Application.DTOs.Tournament.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Constants.Pagination;
using Application.Utils.Extensions;
using Application.Utils.Helper.Slug;

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
        return await tournamentRepository.GetByIdAsync(tournamentId, includes: [tournament => tournament.Divisions]);
    }

    public async Task<Tournament?> GetTournamentByIdOrSlugAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out Guid tournamentId))
        {
            return await GetTournamentByIdAsync(tournamentId);
        }

        IEnumerable<Tournament> matches = await tournamentRepository.FindAsync(
            tournament => tournament.Slug == idOrSlug,
            includes: [tournament => tournament.Divisions]);

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
            detail: $"{previousStatus} -> {newStatus}");
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
