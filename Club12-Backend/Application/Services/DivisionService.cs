using Application.DTOs.Abstract.Response;
using Application.DTOs.Divisions.Request;
using Application.DTOs.Match.Request;
using Application.DTOs.Stage.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Constants.Pagination;
using Application.Utils.Extensions;
using Application.Utils.Helper.Playoff;
using Application.Utils.Helper.Slug;
using Application.Utils.Helper.Standings;

using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Service class responsible for managing Division entities.
/// Provides methods for creating, updating, deleting, and retrieving divisions,
/// as well as fetching paginated lists of divisions with filtering support.
/// </summary>
public class DivisionService(
    IDivisionRepository divisionRepository,
    IStageService stageService,
    IMatchService matchService,
    ITeamRepository teamRepository,
    IStageTeamMatchRepository stageTeamMatchRepository,
    ITournamentRepository tournamentRepository) : IDivisionService
{
    /// <summary>
    /// Creates a new division entity asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division entity to create.</param>
    /// <returns>The created Division entity.</returns>
    public async Task<Division> CreateDivisionAsync(Division divisionEntity)
    {
        await EnsureTournamentAllowsDivisionAsync(divisionEntity);

        PlayoffMappingValidator.Validate(divisionEntity.PlayoffMappings);

        divisionEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            divisionEntity.Name,
            candidate => divisionRepository.ExistsAsync(division => division.Slug == candidate));

        await divisionRepository.AddAsync(divisionEntity);
        return divisionEntity;
    }

    /// <summary>
    /// Deletes a division entity by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the division to delete.</param>
    public async Task DeleteDivisionAsync(Guid id)
    {
        await divisionRepository.RemoveAsync(division => division.Id == id);
    }

    /// <summary>
    /// Updates an existing division entity asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division entity with updated values.</param>
    public async Task UpdateDivisionAsync(Division divisionEntity)
    {
        await EnsureTournamentAllowsDivisionAsync(divisionEntity);

        PlayoffMappingValidator.Validate(divisionEntity.PlayoffMappings);

        await divisionRepository.UpdateAsync(divisionEntity);
    }

    /// <summary>
    /// Guards a division create/update against its tournament. Two rules:
    /// <list type="bullet">
    /// <item>HU-31: divisions may only be created or edited while their
    /// tournament is <see cref="TournamentStatus.OpenForRegistration"/>. Once
    /// registration has closed (or the tournament is
    /// Scheduled/Ongoing/Finished/Canceled) the structure is frozen.</item>
    /// <item>HU-48: a division's <see cref="Division.Category"/> must match its
    /// tournament's <see cref="Tournament.Category"/> — a single tournament can
    /// never mix feminine and masculine divisions.</item>
    /// </list>
    /// A division pointing at a tournament that does not exist is left for the
    /// normal not-found handling downstream.
    /// </summary>
    private async Task EnsureTournamentAllowsDivisionAsync(Division division)
    {
        Tournament? tournament = await tournamentRepository.GetByIdAsync(division.TournamentId);

        if (tournament is null)
        {
            return;
        }

        if (tournament.Status != TournamentStatus.OpenForRegistration)
        {
            throw new InvalidOperationException(
                ErrorMessages.Tournament.StructuralEditNotAllowed(tournament.Status));
        }

        if (division.Category != tournament.Category)
        {
            throw new InvalidOperationException(
                ErrorMessages.Tournament.CategoryMismatch(division.Category, tournament.Category));
        }
    }

    /// <summary>
    /// Retrieves a division entity by its unique identifier asynchronously.
    /// Returns only the basic division data.
    /// </summary>
    /// <param name="divisionId">The unique identifier of the division.</param>
    /// <returns>The Division entity if found; otherwise, null.</returns>
    public async Task<Division?> GetSimpleDivisionByIdAsync(Guid divisionId)
    {
        return await divisionRepository.GetByIdAsync(divisionId);
    }

    /// <summary>
    /// Retrieves a division by its id or its slug. The value is treated as an
    /// id when it parses as a GUID, otherwise it is looked up as a slug.
    /// Returns only the basic division data.
    /// </summary>
    /// <param name="idOrSlug">The division's GUID id or its slug.</param>
    /// <returns>The matching Division, or null if not found.</returns>
    public async Task<Division?> GetSimpleDivisionByIdOrSlugAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out Guid divisionId))
        {
            return await GetSimpleDivisionByIdAsync(divisionId);
        }

        IEnumerable<Division> matches = await divisionRepository.FindAsync(
            division => division.Slug == idOrSlug);

        return matches.FirstOrDefault();
    }

    /// <summary>
    /// Retrieves a division entity by its unique identifier asynchronously,
    /// including related tournament and stages data.
    /// </summary>
    /// <param name="divisionId">The unique identifier of the division.</param>
    /// <returns>The Division entity with related data if found; otherwise, null.</returns>
    public async Task<Division?> GetFullDivisionByIdAsync(Guid divisionId)
    {
        return await divisionRepository.GetByIdAsync(divisionId, includes: [division => division.Tournament, division => division.Stages]);
    }

    /// <summary>
    /// Retrieves a paginated and filtered list of division entities asynchronously.
    /// </summary>
    /// <param name="filter">The filter and pagination parameters.</param>
    /// <returns>A PaginatedResponse{Division} containing the filtered divisions.</returns>
    public async Task<PaginatedResponse<Division>> GetAllDivisionsAsync(GetDivisionsFilteredRequest filter)
    {
        Expression<Func<Division, bool>> expression = QueryableExtensions.ConstructFilterExpression<Division, GetDivisionsFilteredRequest>(filter);
        IEnumerable<Division> filteredDivisions = await divisionRepository.FindAsync(expression, filter: filter);

        int totalCount = await divisionRepository.CountAsync(expression);

        return new PaginatedResponse<Division>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredDivisions
        };
    }

    /// <summary>
    /// Computes standings for a division from its Group stage's finished
    /// matches. Elimination-stage matches do not feed a standings table.
    /// </summary>
    /// <param name="divisionId">The id of the division.</param>
    /// <returns>One Position per team with at least one finished Group-stage match; empty if the division has no Group stage or no finished matches yet.</returns>
    public async Task<List<Position>> GetPositionsByDivisionIdAsync(Guid divisionId)
    {
        PaginatedResponse<Stage> stages = await stageService.GetAllStagesAsync(new GetStagesFilteredRequest
        {
            DivisionId = divisionId,
            StageType = StageType.Group,
            PageSize = PaginationDefaults.MaxPageSize,
        });

        Stage? groupStage = stages.Items.FirstOrDefault();
        if (groupStage is null)
        {
            return [];
        }

        Division? division = await divisionRepository.GetByIdAsync(divisionId);
        int pointsForWin = division?.PointsForWin ?? PositionCalculator.DefaultPointsForWin;
        int pointsForLoss = division?.PointsForLoss ?? PositionCalculator.DefaultPointsForLoss;

        PaginatedResponse<Match> matches = await matchService.GetAllMatchesAsync(new GetMatchesFilteredRequest
        {
            StageId = groupStage.Id,
            IsFinished = true,
            PageSize = PaginationDefaults.MaxPageSize,
        });

        return PositionCalculator.CalculatePositions(matches.Items, pointsForWin, pointsForLoss);
    }

    /// <summary>
    /// Returns every team registered to the tournament that does not yet
    /// belong to any division (regular or cross-division-cup).
    /// </summary>
    /// <param name="tournamentId">The id of the tournament.</param>
    public async Task<List<Team>> GetUnassignedTeamsAsync(Guid tournamentId)
    {
        List<Team> registeredTeams = [.. await teamRepository.FindAsync(team => team.TournamentId == tournamentId)];
        if (registeredTeams.Count == 0)
        {
            return [];
        }

        List<Guid> registeredTeamIds = [.. registeredTeams.Select(t => t.Id)];

        IEnumerable<StageTeamMatch> assignments = await stageTeamMatchRepository.FindAsync(stm =>
            registeredTeamIds.Contains(stm.TeamId)
            && stm.Stage!.Division.TournamentId == tournamentId);

        HashSet<Guid> assignedTeamIds = [.. assignments.Select(a => a.TeamId)];

        return [.. registeredTeams.Where(t => !assignedTeamIds.Contains(t.Id))];
    }

    /// <summary>
    /// Reassigns a division to a different tournament, moving everything
    /// under it — stages, matches, and team assignments — along with it,
    /// since none of that data carries its own tournament reference. Only
    /// the target tournament's existence is validated here; the caller must
    /// still call <see cref="UpdateDivisionAsync"/> to persist the change.
    /// </summary>
    /// <param name="division">The division to reassign. Its Tournament navigation and TournamentId are mutated in place.</param>
    /// <param name="tournamentId">The id of the tournament the division should belong to.</param>
    /// <returns>True if the target tournament exists and the division was reassigned in memory; false if no tournament with that id exists.</returns>
    public async Task<bool> TryAssignTournamentAsync(Division division, Guid tournamentId)
    {
        if (division.TournamentId == tournamentId)
        {
            return true;
        }

        Tournament? targetTournament = await tournamentRepository.GetByIdAsync(tournamentId);
        if (targetTournament is null)
        {
            return false;
        }

        division.Tournament = targetTournament;
        division.TournamentId = tournamentId;

        return true;
    }
}
