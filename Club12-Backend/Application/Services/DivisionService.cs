using Application.DTOs.Abstract.Response;
using Application.DTOs.Divisions.Request;
using Application.DTOs.Match.Request;
using Application.DTOs.Stage.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants.Pagination;
using Application.Utils.Extensions;
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
    IStageTeamMatchRepository stageTeamMatchRepository) : IDivisionService
{
    /// <summary>
    /// Creates a new division entity asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division entity to create.</param>
    /// <returns>The created Division entity.</returns>
    public async Task<Division> CreateDivisionAsync(Division divisionEntity)
    {
        await divisionRepository.AddAsync(divisionEntity);
        return divisionEntity;
    }

    /// <summary>
    /// Deletes a division entity by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the division to delete.</param>
    public async Task DeleteDivisionAsync(Guid id)
        => await divisionRepository.RemoveAsync(division => division.Id == id);
    
    /// <summary>
    /// Updates an existing division entity asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division entity with updated values.</param>
    public async Task UpdateDivisionAsync(Division divisionEntity)
        => await divisionRepository.UpdateAsync(divisionEntity);

    /// <summary>
    /// Retrieves a division entity by its unique identifier asynchronously.
    /// Returns only the basic division data.
    /// </summary>
    /// <param name="divisionId">The unique identifier of the division.</param>
    /// <returns>The Division entity if found; otherwise, null.</returns>
    public async Task<Division?> GetSimpleDivisionByIdAsync(Guid divisionId)
        => await divisionRepository.GetByIdAsync(divisionId);

    /// <summary>
    /// Retrieves a division entity by its unique identifier asynchronously,
    /// including related tournament and stages data.
    /// </summary>
    /// <param name="divisionId">The unique identifier of the division.</param>
    /// <returns>The Division entity with related data if found; otherwise, null.</returns>
    public async Task<Division?> GetFullDivisionByIdAsync(Guid divisionId)
        => await divisionRepository.GetByIdAsync(divisionId, includes: [division => division.Tournament, division => division.Stages]);

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

        PaginatedResponse<Match> matches = await matchService.GetAllMatchesAsync(new GetMatchesFilteredRequest
        {
            StageId = groupStage.Id,
            IsFinished = true,
            PageSize = PaginationDefaults.MaxPageSize,
        });

        return PositionCalculator.CalculatePositions(matches.Items);
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
}
