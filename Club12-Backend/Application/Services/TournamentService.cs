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
    IMatchService matchService) : ITournamentService
{
    public async Task<Tournament> CreateTournamentAsync(Tournament tournamentEntity)
    {
        tournamentEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            tournamentEntity.Name,
            candidate => tournamentRepository.ExistsAsync(tournament => tournament.Slug == candidate));

        await tournamentRepository.AddAsync(tournamentEntity);
        return tournamentEntity;
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

        // Closing registration is the canonical fixture trigger: generate the
        // matches for every division's stages exactly once (see
        // GenerateFixtureAsync for the idempotency guard). Done BEFORE the
        // status is committed so a generation failure leaves the tournament in
        // its prior (still editable) status instead of stranding it in
        // RegistrationClosed with a half-built fixture.
        if (newStatus == TournamentStatus.RegistrationClosed)
        {
            await GenerateFixtureAsync(tournament);
        }

        tournament.Status = newStatus;
        await tournamentRepository.UpdateAsync(tournament);
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

            foreach (Stage stage in stages.Items)
            {
                PaginatedResponse<Match> existingMatches = await matchService.GetAllMatchesAsync(new GetMatchesFilteredRequest
                {
                    StageId = stage.Id,
                    PageSize = 1,
                });

                if (existingMatches.TotalCount > 0)
                {
                    continue;
                }

                await matchService.CreateAutomatedMatchesAsync(stage.Id);
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
