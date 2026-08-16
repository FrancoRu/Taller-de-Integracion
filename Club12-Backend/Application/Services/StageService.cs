using Application.DTOs.Abstract.Request;
using Application.DTOs.Abstract.Response;
using Application.DTOs.Stage.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Application.Utils.Constants.Stage;
using Application.Utils.Extensions;
using Application.Utils.Helper.Playoff;
using Application.Utils.Helper.StageHelper;
using Application.Utils.Helper.Standings;
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
/// Service responsible for managing tournament stages, including creation, retrieval, updating, deletion,
/// automated stage generation, and team assignments within stages.
/// </summary>
public class StageService(IUnitOfWork unitOfWork) : IStageService
{
    private readonly IStageRepository stageRepository = unitOfWork.StageRepository;
    private readonly IDivisionRepository divisionRepository = unitOfWork.DivisionRepository;
    private readonly IStageTeamMatchRepository stageTeamMatchRepository = unitOfWork.StageTeamMatchRepository;
    private readonly ITeamRepository teamRepository = unitOfWork.TeamRepository;
    private readonly IMatchRepository matchRepository = unitOfWork.MatchRepository;

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

        if (filter.TournamentId.HasValue)
        {
            Expression<Func<Stage, bool>> tournamentExpression = stage => stage.Division.TournamentId == filter.TournamentId.Value;
            expression = expression.And(tournamentExpression);
        }

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
            CreatedBy = AuditConstants.SystemUser,
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

        int registeredTeams = await teamRepository.CountAsync(team => team.TournamentId == division.TournamentId);

        if (!IsValidTournamentSize(registeredTeams))
        {
            throw new InvalidOperationException(
                $"Invalid number of registered teams: {registeredTeams}. Valid sizes are " +
                $"{TournamentBracketSize.EIGHT}, {TournamentBracketSize.SIXTEEN}, " +
                $"{TournamentBracketSize.THIRTY_TWO}, or {TournamentBracketSize.SIXTY_FOUR} teams.");
        }

        if (registeredTeams % MaxTeams.GROUP != 0)
        {
            throw new InvalidOperationException(
                $"The number of registered teams ({registeredTeams}) must be divisible by {MaxTeams.GROUP} to generate group stages.");
        }

        List<Stage> stages = [];

        DateTime startDate = division.Tournament.StartDate;

        int totalGroups = registeredTeams / MaxTeams.GROUP;

        int order = 0;

        for (int i = 1; i <= totalGroups; i++)
        {
            Stage groupStage = BuildStage(StageType.Group, StageTemplate.Group, startDate, division, daysMultiplier: 2);

            char groupLetter = (char)(i + 64);

            groupStage.Name = $"{StageTemplate.Group.Name} - Grupo {groupLetter}";
            groupStage.Order = order++;
            stages.Add(groupStage);
        }

        startDate = stages.First().EndDate.AddDays(StageTemplate.StandardGapDays);

        if (registeredTeams >= TournamentBracketSize.SIXTEEN)
        {
            Stage quarterFinalStage = BuildStage(StageType.QuarterFinal, StageTemplate.QuarterFinal, startDate, division);
            stages.Add(quarterFinalStage);
            quarterFinalStage.Order = order++;
            startDate = quarterFinalStage.EndDate.AddDays(StageTemplate.StandardGapDays);
        }

        Stage semiFinalStage = BuildStage(StageType.SemiFinal, StageTemplate.SemiFinal, startDate, division);
        stages.Add(semiFinalStage);
        semiFinalStage.Order = order++;
        startDate = semiFinalStage.EndDate.AddDays(StageTemplate.ThirdPlaceGapDays);

        Stage thirdPlaceStage = BuildStage(StageType.ThirdPlace, StageTemplate.ThirdPlace, startDate, division);
        stages.Add(thirdPlaceStage);
        thirdPlaceStage.Order = order++;
        startDate = thirdPlaceStage.EndDate.AddDays(StageTemplate.StandardGapDays);

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
    /// <exception cref="InvalidOperationException">Thrown if the stage already has the maximum number of teams or if too many teams are assigned.</exception>
    public async Task AssignTeamsToStageAsync(Stage stage, List<Guid>? teamIds = null, bool auto = false)
    {
        IEnumerable<StageTeamMatch> existingMatches = await stageTeamMatchRepository.FindAsync(stageTeamMatch => stageTeamMatch.StageId == stage.Id);

        int maxTeams = StageHelper.GetMaxTeamsForStage(stage.StageType);
        int availableSlots = maxTeams - existingMatches.Count();

        if (availableSlots <= 0)
        {
            throw new InvalidOperationException($"This Stage already has the maximum of {maxTeams} teams.");
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
                throw new InvalidOperationException($"Cannot add {filteredIds.Count} teams. Only {availableSlots} slots available.");
            }

            await EnsureNoCrossDivisionConflictAsync(stage, filteredIds);

            newItems = [.. filteredIds.Select(teamId => new StageTeamMatch
            {
                Id = Guid.Empty,
                StageId = stage.Id,
                TeamId = teamId,
                DateCreated = DateTime.UtcNow,
                CreatedBy = AuditConstants.SystemUser,
            })];
        }
        else
        {
            PaginatedFilterRequest filter = new()
            {
                PageSize = availableSlots,
            };
            List<Team> teams = [.. await teamRepository.FindAsync(
                team => team.TournamentId == stage.Division.TournamentId
                    && !team.StageTeamMatches.Any(stm => stm.TeamId == team.Id && stm.StageId == stage.Id), filter: filter)];

            if (!stage.Division.IsCrossDivisionCup)
            {
                List<Guid> conflictingTeamIds = await FindTeamsInAnotherDivisionAsync(stage, [.. teams.Select(t => t.Id)]);
                teams = [.. teams.Where(t => !conflictingTeamIds.Contains(t.Id))];
            }

            newItems = [.. teams.Select(t => new StageTeamMatch
            {
                Id = Guid.Empty,
                StageId = stage.Id,
                TeamId = t.Id,
                CreatedBy = AuditConstants.SystemUser,
                DateCreated = DateTime.UtcNow,
            })];
        }

        if (newItems.Count != 0)
        {
            await stageTeamMatchRepository.AddRangeAsync(newItems);
        }
    }

    /// <summary>
    /// Throws if any of the given teams is already assigned to a stage in a
    /// different, non-cross-division-cup division of the same tournament as
    /// <paramref name="stage"/>'s division. Skips the check entirely when
    /// <paramref name="stage"/>'s own division is itself a cross-division
    /// cup, since that division is expected to share teams with every zone.
    /// </summary>
    private async Task EnsureNoCrossDivisionConflictAsync(Stage stage, IEnumerable<Guid> teamIds)
    {
        if (stage.Division.IsCrossDivisionCup) return;

        List<Guid> conflictingTeamIds = await FindTeamsInAnotherDivisionAsync(stage, teamIds);

        if (conflictingTeamIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot assign team(s) {string.Join(", ", conflictingTeamIds)} to this division: " +
                "already assigned to another division of the same tournament.");
        }
    }

    /// <summary>
    /// Returns the subset of teamIds already assigned to a stage in a
    /// different, non-cross-division-cup division of the same tournament.
    /// </summary>
    private async Task<List<Guid>> FindTeamsInAnotherDivisionAsync(Stage stage, IEnumerable<Guid> teamIds)
    {
        List<Guid> teamIdList = [.. teamIds];
        if (teamIdList.Count == 0) return [];

        IEnumerable<StageTeamMatch> conflicting = await stageTeamMatchRepository.FindAsync(stm =>
            teamIdList.Contains(stm.TeamId)
            && stm.Stage!.DivisionId != stage.DivisionId
            && stm.Stage!.Division.TournamentId == stage.Division.TournamentId
            && !stm.Stage!.Division.IsCrossDivisionCup);

        return [.. conflicting.Select(stm => stm.TeamId).Distinct()];
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
    /// Seeds an elimination stage's already-generated, still-empty matches
    /// using the division's group-stage standings, pairing seeds in the
    /// classic bracket order (1v8, 4v5, 2v7, 3v6) so the top two seeds can
    /// only meet in the final.
    /// </summary>
    public async Task<List<Match>> SeedKnockoutStageAsync(Guid stageId)
    {
        Stage stage = await stageRepository.GetByIdAsync(stageId,
            includes: [s => s.Matches, s => s.StageTeamMatches, s => s.Division])
            ?? throw new InvalidOperationException("Stage not found.");

        if (stage.Matches.Count == 0)
        {
            throw new InvalidOperationException("Generate this stage's matches before seeding it.");
        }

        if (stage.Matches.Any(m => m.HomeTeamId.HasValue || m.VisitorTeamId.HasValue))
        {
            throw new InvalidOperationException("This stage has already been seeded.");
        }

        List<Guid> assignedTeamIds = [.. stage.StageTeamMatches.Select(stm => stm.TeamId)];
        int slotCapacity = stage.Matches.Count * 2;

        if (assignedTeamIds.Count < 2 || assignedTeamIds.Count > slotCapacity)
        {
            throw new InvalidOperationException(
                $"Cannot seed: {assignedTeamIds.Count} team(s) assigned to this stage, expected between 2 and {slotCapacity}. " +
                "A team count below the full bracket is fine (the strongest seeds get a bye), but it cannot exceed the generated slots.");
        }

        List<Match> groupMatches = [.. await matchRepository.FindAsync(m =>
            m.Stage.DivisionId == stage.DivisionId && m.Stage.StageType == StageType.Group,
            includes: [m => m.HomeTeam!, m => m.VisitorTeam!, m => m.WinningTeam!])];

        List<Position> standings = PositionCalculator.CalculatePositions(groupMatches);

        List<Guid> orderedTeamIds = [.. standings
            .Where(position => assignedTeamIds.Contains(position.TeamId))
            .Select(position => position.TeamId)];

        if (orderedTeamIds.Count != assignedTeamIds.Count)
        {
            throw new InvalidOperationException(
                "Cannot seed: not every team assigned to this stage has a finished-group-stage position yet.");
        }

        List<(Guid HomeTeamId, Guid? VisitorTeamId)> pairs = PlayoffSeeder.SeedPairs(orderedTeamIds);

        List<Match> orderedMatches = [.. stage.Matches.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];

        for (int i = 0; i < pairs.Count; i++)
        {
            orderedMatches[i].HomeTeamId = pairs[i].HomeTeamId;
            orderedMatches[i].VisitorTeamId = pairs[i].VisitorTeamId;

            if (pairs[i].VisitorTeamId is null)
            {
                orderedMatches[i].IsFinished = true;
                orderedMatches[i].WinningTeamId = pairs[i].HomeTeamId;
            }
        }

        await matchRepository.UpdateRangeAsync(orderedMatches);

        return orderedMatches;
    }

    /// <summary>
    /// Validates if the tournament size is a valid power of 2 and within acceptable range.
    /// </summary>
    /// <param name="teamCount">The number of teams.</param>
    /// <returns>True if valid tournament size, false otherwise.</returns>
    private static bool IsValidTournamentSize(int teamCount)
        => teamCount == TournamentBracketSize.EIGHT
        || teamCount == TournamentBracketSize.SIXTEEN
        || teamCount == TournamentBracketSize.THIRTY_TWO
        || teamCount == TournamentBracketSize.SIXTY_FOUR;
}
