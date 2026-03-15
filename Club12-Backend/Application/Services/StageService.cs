using Application.DTOs.Abstract.Request;
using Application.DTOs.Abstract.Response;
using Application.DTOs.Stage.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants.Stage;
using Application.Utils.Extensions;
using Application.Utils.Helper.StageHelper;
using Domain.Entities.Models;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Service responsible for managing tournament stages, including creation, retrieval, updating, deletion,
/// automated stage generation, and team assignments within stages.
/// </summary>
public class StageService(IUnitOfWork unitOfWork) : IStageService
{
    private readonly IStageRepository stageRepository = unitOfWork.StageRepository;
    private readonly IDivisionRepository divisionRepository = unitOfWork.DivisionRepository;
    private readonly IStageTeamMatchRepository stageTeamMatchRepository = unitOfWork.StageTeamMatchRepository;
    private readonly ITeamRepository teamRepository = unitOfWork.TeamRepository;

    /// <summary>
    /// Retrieves a stage by its unique identifier.
    /// </summary>
    /// <param name="stageId">The unique identifier of the stage.</param>
    /// <returns>The stage entity if found; otherwise, null.</returns>
    public async Task<Stage?> GetStageByIdAsync(Guid stageId)
        => await stageRepository.GetByIdAsync(stageId);

    /// <summary>
    /// Retrieves a paginated list of stages based on the provided filter criteria.
    /// </summary>
    /// <param name="filter">Filtering and pagination options.</param>
    /// <returns>A paginated response containing the filtered stages.</returns>
    public async Task<PaginatedResponse<Stage>> GetAllStagesAsync(GetStagesFilteredRequest filter)
    {
        Expression<Func<Stage, bool>> expression = QueryableExtensions.ConstructFilterExpression<Stage, GetStagesFilteredRequest>(filter);
        IEnumerable<Stage> filteredPlayers = await stageRepository.FindAsync(expression, filter: filter);

        int totalCount = await stageRepository.CountAsync(expression);

        return new PaginatedResponse<Stage>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredPlayers
        };
    }

    /// <summary>
    /// Deletes a stage by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the stage to delete.</param>
    public async Task DeleteStageAsync(Guid id)
        => await stageRepository.RemoveAsync(stage => stage.Id == id);

    /// <summary>
    /// Updates an existing stage entity.
    /// </summary>
    /// <param name="stageEntity">The stage entity to update.</param>
    public async Task UpdateStageAsync(Stage stageEntity)
        => await stageRepository.UpdateAsync(stageEntity);

    /// <summary>
    /// Creates a new stage entity if it does not already exist in the specified division.
    /// </summary>
    /// <param name="stageEntity">The stage entity to create.</param>
    /// <returns>The created stage entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a stage with the same name already exists in the division.</exception>
    public async Task<Stage> CreateStageAsync(Stage stageEntity)
    {
        bool existStage = await stageRepository.ExistsAsync(
            s => s.Name == stageEntity.Name && s.DivisionId == stageEntity.DivisionId);

        if (!existStage)
        {
            await stageRepository.AddAsync(stageEntity);
            return stageEntity;
        }

        throw new InvalidOperationException($"Stage with name '{stageEntity.Name}' already exists in the current division.");
    }

    /// <summary>
    /// Builds a new stage entity based on the provided parameters.
    /// </summary>
    /// <param name="stageType">The type of the stage.</param>
    /// <param name="template">The template to use for the stage.</param>
    /// <param name="startDate">The start date of the stage.</param>
    /// <param name="division">The division associated with the stage.</param>
    /// <param name="daysMultiplier">Multiplier for the stage duration in days.</param>
    /// <returns>A new stage entity.</returns>
    private static Stage BuildStage(StageType stageType, Template template, DateTime startDate, Division division, int daysMultiplier = 1)
    {
        return new Stage
        {
            Id = Guid.Empty,
            Name = template.Name,
            Description = template.Description,
            StageType = stageType,
            IsActive = true,
            IsElimination = stageType != StageType.Group,
            StartDate = startDate,
            EndDate = startDate.AddDays(StageTemplate.DurationDays * daysMultiplier),
            Division = division,
            DivisionId = division.Id,
            Matches = [],
            DateCreated = DateTime.UtcNow,
            CreatedBy = "System",
        };
    }

    /// <summary>
    /// Automatically generates and creates all stages for a division based on tournament size and structure.
    /// </summary>
    /// <param name="divisionId">The unique identifier of the division.</param>
    /// <returns>A list of created stage entities.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the division is not found, already has stages, or has an invalid tournament size.</exception>
    public async Task<List<Stage>> CreateAutomatedStagesAsync(Guid divisionId)
    {
        Division division = await divisionRepository.GetByIdAsync(divisionId, includes: [division => division.Stages, division => division.Tournament])
            ?? throw new InvalidOperationException("Division not found.");

        if (division.Stages.Count > 0)
        {
            throw new InvalidOperationException("Cannot process the current request because the current division already has some stage.");
        }

        int maxTeams = division.Tournament.MaxTeams;

        // Validate maxTeams is a valid tournament size
        if (!IsValidTournamentSize(maxTeams))
        {
            throw new InvalidOperationException($"Invalid tournament size: {maxTeams}. Valid sizes are 8, 16, 32, or 64 teams.");
        }

        List<Stage> stages = [];


        DateTime startDate = division.Tournament.StartDate;
        // Step 1: Create Group Stage

        int totalGroups = maxTeams / 4;

        int order = 0;

        for (int i = 1; i <= totalGroups; i++)
        {
            Stage groupStage = BuildStage(StageType.Group, StageTemplate.Group, startDate, division, daysMultiplier: 2);

            char groupLetter = (char)(i + 64);

            groupStage.Name = $"{StageTemplate.Group.Name} - Grupo {groupLetter}";
            groupStage.Order = order++;
            stages.Add(groupStage);
        }

        startDate = stages.First().EndDate.AddDays(2);

        // Step 2: Create Quarter-finals (Cuartos) Stage if necessary
        if (maxTeams >= 16)
        {
            Stage quarterFinalStage = BuildStage(StageType.QuarterFinal, StageTemplate.QuarterFinal, startDate, division);
            stages.Add(quarterFinalStage);
            quarterFinalStage.Order = order++;
            startDate = quarterFinalStage.EndDate.AddDays(2);
        }

        // Step 3: Create Semi-final Stage
        Stage semiFinalStage = BuildStage(StageType.SemiFinal, StageTemplate.SemiFinal, startDate, division);
        stages.Add(semiFinalStage);
        semiFinalStage.Order = order++;
        startDate = semiFinalStage.EndDate.AddDays(1);

        // Step 4: Create Third Place Stage
        Stage thirdPlaceStage = BuildStage(StageType.ThirdPlace, StageTemplate.ThirdPlace, startDate, division);
        stages.Add(thirdPlaceStage);
        thirdPlaceStage.Order = order++;
        startDate = thirdPlaceStage.EndDate.AddDays(2);

        // Step 5: Create Final Stage
        Stage finalStage = BuildStage(StageType.Final, StageTemplate.Final, startDate, division);
        stages.Add(finalStage);

        finalStage.Order = order++;

        await stageRepository.AddRangeAsync(stages);

        return stages;
    }

    /// <summary>
    /// Assigns teams to a stage, either manually by team IDs or automatically based on available slots.
    /// </summary>
    /// <param name="stage">The stage to assign teams to.</param>
    /// <param name="teamIds">Optional list of team IDs to assign.</param>
    /// <param name="auto">If true, assigns teams automatically based on available slots.</param>
    /// <exception cref="Exception">Thrown if the stage already has the maximum number of teams or if too many teams are assigned.</exception>
    public async Task AssignTeamsToStageAsync(Stage stage, List<Guid>? teamIds = null, bool auto = false)
    {
        IEnumerable<StageTeamMatch> existingMatches = await stageTeamMatchRepository.FindAsync(stageTeamMatch => stageTeamMatch.StageId == stage.Id);

        int maxTeams = StageHelper.GetMaxTeamsForStage(stage.StageType);
        int availableSlots = maxTeams - existingMatches.Count();

        if (availableSlots <= 0)
        {
            throw new Exception($"This Stage already has the maximum of {maxTeams} teams.");
        }

        List<StageTeamMatch> newItems = [];

        if (!auto)
        {
            if (teamIds == null || teamIds.Count == 0) return;

            List<Guid> filteredIds = [.. teamIds
                .Distinct()
                .Where(id => !existingMatches.Any(stm => stm.TeamId == id))];

            if (filteredIds.Count > availableSlots)
            {
                throw new Exception($"Cannot add {filteredIds.Count} teams. Only {availableSlots} slots available.");
            }

            newItems = [.. filteredIds.Select(teamId => new StageTeamMatch
            {
                Id = Guid.Empty,
                StageId = stage.Id,
                TeamId = teamId,
                DateCreated = DateTime.UtcNow,
                CreatedBy = "System",
            })];
        }
        else
        {
            PaginatedFilterRequest filter = new()
            {
                PageSize = availableSlots,
            };
            List<Team> teams = [.. await teamRepository.FindAsync(
                team => !team.StageTeamMatches.Any(stm => stm.TeamId == team.Id && stm.StageId == stage.Id), filter: filter)];

            newItems = [.. teams.Select(t => new StageTeamMatch
            {
                Id = Guid.Empty,
                StageId = stage.Id,
                TeamId = t.Id,
                CreatedBy = "System",
                DateCreated = DateTime.UtcNow,
            })];
        }

        if (newItems.Count != 0)
        {
            await stageTeamMatchRepository.AddRangeAsync(newItems);
        }
    }

    /// <summary>
    /// Unassigns teams from a stage based on the provided team IDs.
    /// </summary>
    /// <param name="stage">The stage to unassign teams from.</param>
    /// <param name="teamIds">List of team IDs to unassign.</param>
    public async Task UnassignTeamsFromStageAsync(Stage stage, List<Guid> teamIds)
    {
        if (teamIds == null || teamIds.Count == 0) return;

        await stageTeamMatchRepository.RemoveAsync(stm =>
            stm.StageId == stage.Id && teamIds.Contains(stm.TeamId)
        );
    }

    /// <summary>
    /// Validates if the tournament size is a valid power of 2 and within acceptable range.
    /// </summary>
    /// <param name="teamCount">The number of teams.</param>
    /// <returns>True if valid tournament size, false otherwise.</returns>
    private static bool IsValidTournamentSize(int teamCount)
        => teamCount == 8 || teamCount == 16 || teamCount == 32 || teamCount == 64;
}
