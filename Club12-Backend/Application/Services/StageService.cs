using Application.DTOs.Abstract.Request;
using Application.DTOs.Abstract.Response;
using Application.DTOs.Stage.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Constants.Stage;
using Application.Utils.Extensions;
using Application.Utils.Helper.Playoff;
using Application.Utils.Helper.Slug;
using Application.Utils.Helper.StageHelper;
using Application.Utils.Helper.Standings;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using LinqKit;

using Microsoft.Extensions.Logging;

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
public class StageService(IUnitOfWork unitOfWork, ILogger<StageService> logger) : IStageService
{
    private readonly IStageRepository _stageRepository = unitOfWork.StageRepository;
    private readonly IDivisionRepository _divisionRepository = unitOfWork.DivisionRepository;
    private readonly IStageTeamMatchRepository _stageTeamMatchRepository = unitOfWork.StageTeamMatchRepository;
    private readonly ITeamRepository _teamRepository = unitOfWork.TeamRepository;
    private readonly IMatchRepository _matchRepository = unitOfWork.MatchRepository;
    private readonly IMatchSeriesRepository _matchSeriesRepository = unitOfWork.MatchSeriesRepository;

    /// <summary>
    /// The elimination rounds a wizard-built cup can be made of, in bracket
    /// order (see <c>qualifiersToStageTypes</c> on the frontend, which never
    /// produces <see cref="StageType.ThirdPlace"/> — that stage type only
    /// exists for the legacy fixed-size bracket generator). Used to find "the
    /// next round of this same cup" without relying on <see cref="Stage.Order"/>,
    /// which the wizard's per-cup stage creation never sets.
    /// </summary>
    private static readonly StageType[] EliminationProgression =
    [
        StageType.RoundOf16,
        StageType.QuarterFinal,
        StageType.SemiFinal,
        StageType.Final,
    ];

    /// <summary>
    /// The stage type immediately after <paramref name="current"/> in
    /// <see cref="EliminationProgression"/>, or null when <paramref name="current"/>
    /// is the Final, not part of the progression (Group), or unrecognized.
    /// </summary>
    private static StageType? NextStageType(StageType current)
    {
        int index = Array.IndexOf(EliminationProgression, current);
        if (index < 0 || index == EliminationProgression.Length - 1)
        {
            return null;
        }

        return EliminationProgression[index + 1];
    }

    /// <summary>
    /// This stage type's position in <see cref="EliminationProgression"/> —
    /// lower means earlier in the bracket. Types outside the progression
    /// (Group, ThirdPlace) sort last; callers that use this to pick "the
    /// earliest round of a cup" already exclude Group upstream, and a
    /// wizard-built cup never produces ThirdPlace (see
    /// <see cref="EliminationProgression"/>'s own remarks).
    /// </summary>
    private static int EliminationDepth(StageType stageType)
    {
        int index = Array.IndexOf(EliminationProgression, stageType);
        return index < 0 ? int.MaxValue : index;
    }

    /// <summary>
    /// Retrieves a stage by its unique identifier.
    /// </summary>
    /// <param name="stageId">The unique identifier of the stage.</param>
    /// <returns>The stage entity if found; otherwise, null.</returns>
    public async Task<Stage?> GetStageByIdAsync(Guid stageId)
    {
        return await _stageRepository.GetByIdAsync(stageId, includes: [stage => stage.Division]);
    }

    /// <summary>
    /// Retrieves a stage by its id or its slug. The value is treated as an id
    /// when it parses as a GUID, otherwise it is looked up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The stage's GUID id or its slug.</param>
    /// <returns>The matching stage, or null if not found.</returns>
    public async Task<Stage?> GetStageByIdOrSlugAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out Guid stageId))
        {
            return await GetStageByIdAsync(stageId);
        }

        IEnumerable<Stage> matches = await _stageRepository.FindAsync(
            stage => stage.Slug == idOrSlug,
            includes: [stage => stage.Division]);

        return matches.FirstOrDefault();
    }

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

        IEnumerable<Stage> filteredPlayers = await _stageRepository.FindAsync(expression, filter: filter);

        int totalCount = await _stageRepository.CountAsync(expression);

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
    /// <exception cref="InvalidOperationException">
    /// Thrown when the stage's tournament has already started (its fixture is
    /// generated); removing a phase then would corrupt the bracket/fixture.
    /// </exception>
    public async Task DeleteStageAsync(Guid id)
    {
        Stage? stage = await _stageRepository.GetByIdAsync(id);

        if (stage is not null)
        {
            await EnsureDivisionStructureEditableAsync(stage.DivisionId);
        }

        await _stageRepository.RemoveAsync(stage => stage.Id == id);
    }

    /// <summary>
    /// Guards manual phase (stage) structure edits against the state of the
    /// division's tournament. A division's stages may only be added or removed
    /// while the tournament has not started yet — i.e. before its fixture is
    /// generated. The fixture is generated when the tournament transitions to
    /// <see cref="TournamentStatus.Ongoing"/> (see TournamentService.ChangeStatusAsync);
    /// once that has happened (<see cref="TournamentStatus.Ongoing"/> or
    /// <see cref="TournamentStatus.Finished"/>) the matches already reference the
    /// existing set of stages, so adding or removing a stage would corrupt the
    /// bracket. Editing stays allowed while the tournament is
    /// <see cref="TournamentStatus.Scheduled"/>,
    /// <see cref="TournamentStatus.OpenForRegistration"/>, or
    /// <see cref="TournamentStatus.RegistrationClosed"/> (structure still
    /// editable). A division whose tournament cannot be resolved is left for the
    /// normal not-found handling downstream.
    /// </summary>
    /// <param name="divisionId">The division whose tournament is checked.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown (mapped to 409) when the tournament has already started.
    /// </exception>
    private async Task EnsureDivisionStructureEditableAsync(Guid divisionId)
    {
        Division? division = await _divisionRepository.GetByIdAsync(
            divisionId, includes: [division => division.Tournament]);

        if (division?.Tournament is null)
        {
            return;
        }

        bool fixtureGenerated = division.Tournament.Status
            is TournamentStatus.Ongoing or TournamentStatus.Finished;

        if (fixtureGenerated)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.StructureLockedTournamentStarted);
        }
    }

    /// <summary>
    /// Updates an existing stage entity.
    /// </summary>
    /// <param name="stageEntity">The stage entity to update.</param>
    public async Task UpdateStageAsync(Stage stageEntity)
    {
        await _stageRepository.UpdateAsync(stageEntity);
    }

    /// <summary>
    /// Creates a new stage entity if it does not already exist in the specified division.
    /// </summary>
    /// <param name="stageEntity">The stage entity to create.</param>
    /// <returns>The created stage entity.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a stage with the same name already exists in the division, or if a
    /// non-cross-division-cup division already has a Group stage and
    /// <paramref name="stageEntity"/> is also a Group stage (a regular division's
    /// round-robin phase is a single stage — see <see cref="AssignTeamsToStageAsync"/>'s
    /// comment on why a Group stage can hold an entire zone's teams — so a second one
    /// would be an orphaned, ambiguous fixture). A cross-division cup
    /// (<see cref="Division.IsCrossDivisionCup"/>) is exempt: it may hold several Group
    /// stages whose top teams are pooled to seed one bracket (HU-110).
    /// </exception>
    public async Task<Stage> CreateStageAsync(Stage stageEntity)
    {
        await EnsureDivisionStructureEditableAsync(stageEntity.DivisionId);

        bool existStage = await _stageRepository.ExistsAsync(
            s => s.Name == stageEntity.Name && s.DivisionId == stageEntity.DivisionId);

        if (existStage)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.AlreadyExistsInDivision(stageEntity.Name));
        }

        if (stageEntity.StageType == StageType.Group)
        {
            bool hasGroupStage = await _stageRepository.ExistsAsync(
                s => s.DivisionId == stageEntity.DivisionId && s.StageType == StageType.Group);

            // HU-110: a multi-group cross-division cup ("Copa Club12") is
            // seeded by pooling the top teams of SEVERAL internal group
            // stages, so it may legitimately hold more than one Group stage.
            // Every regular division keeps the original one-Group-per-division
            // rule (a second one would be an orphaned, ambiguous fixture).
            bool isCrossDivisionCup = await _divisionRepository.ExistsAsync(
                d => d.Id == stageEntity.DivisionId && d.IsCrossDivisionCup);

            if (hasGroupStage && !isCrossDivisionCup)
            {
                throw new InvalidOperationException(ErrorMessages.Stage.GroupStageAlreadyExistsInDivision);
            }
        }

        stageEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            stageEntity.Name,
            candidate => _stageRepository.ExistsAsync(stage => stage.Slug == candidate));

        await _stageRepository.AddAsync(stageEntity);
        return stageEntity;
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
            Slug = string.Empty,
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
        Division division = await _divisionRepository.GetByIdAsync(divisionId, includes: [division => division.Stages, division => division.Tournament])
            ?? throw new InvalidOperationException(ErrorMessages.Stage.DivisionNotFound);

        if (division.Stages.Count > 0)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.DivisionAlreadyHasStages);
        }

        int registeredTeams = await _teamRepository.CountAsync(team => team.TournamentId == division.TournamentId);

        if (!IsValidTournamentSize(registeredTeams))
        {
            throw new InvalidOperationException(
                ErrorMessages.Stage.InvalidTournamentSize(
                    registeredTeams,
                    $"{TournamentBracketSize.EIGHT}, {TournamentBracketSize.SIXTEEN}, {TournamentBracketSize.THIRTY_TWO}, or {TournamentBracketSize.SIXTY_FOUR}"));
        }

        if (registeredTeams % MaxTeams.GROUP != 0)
        {
            throw new InvalidOperationException(
                ErrorMessages.Stage.TeamsNotDivisibleForGroups(registeredTeams, MaxTeams.GROUP));
        }

        List<Stage> stages = [];

        DateTime startDate = division.Tournament.StartDate;

        int totalGroups = registeredTeams / MaxTeams.GROUP;

        int order = 0;

        for (int i = 1; i <= totalGroups; i++)
        {
            Stage groupStage = BuildStage(StageType.Group, StageTemplate.Group, startDate, division, daysMultiplier: 2);

            char groupLetter = (char) (i + 64);

            groupStage.Name = $"{StageTemplate.Group.Name} - Grupo {groupLetter}";
            groupStage.Order = order++;
            stages.Add(groupStage);
        }

        startDate = stages[0].EndDate.AddDays(StageTemplate.StandardGapDays);

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

        finalStage.Order = order;

        await AssignStageSlugsAsync(stages);

        await _stageRepository.AddRangeAsync(stages);

        return stages;
    }

    /// <summary>
    /// Assigns a unique slug to every stage in a freshly built batch (e.g. a
    /// division's full set of automated stages) before it is persisted.
    /// Uniqueness is checked against both already-persisted stages and the
    /// slugs already assigned earlier in this same batch, since none of the
    /// batch's stages exist in the repository yet when this runs. Mirrors
    /// MatchService.AssignMatchSlugsAsync.
    /// </summary>
    private async Task AssignStageSlugsAsync(List<Stage> stages)
    {
        HashSet<string> slugsAssignedInBatch = [];

        foreach (Stage stage in stages)
        {
            stage.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
                stage.Name,
                async candidate => slugsAssignedInBatch.Contains(candidate)
                    || await _stageRepository.ExistsAsync(s => s.Slug == candidate));

            slugsAssignedInBatch.Add(stage.Slug);
        }
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
        IEnumerable<StageTeamMatch> existingMatches = await _stageTeamMatchRepository.FindAsync(stageTeamMatch => stageTeamMatch.StageId == stage.Id);

        // MaxTeams.GROUP (4) is the auto-bracket-generator's fixed group
        // SIZE (see CreateAutomatedStagesAsync below), not a general cap on
        // how many teams a Group-type stage may ever hold. A single Group
        // stage manually built by the tournament wizard represents a whole
        // zone's round-robin phase and can legitimately need far more than
        // 4 teams (e.g. a 9- or 14-team zone), so it's capped at the same
        // ceiling the tournament itself enforces instead of the
        // auto-generator's per-group size.
        int maxTeams = stage.StageType == StageType.Group
            ? MaxTeams.GROUP_STAGE_CAP
            : StageHelper.GetMaxTeamsForStage(stage.StageType);
        int availableSlots = maxTeams - existingMatches.Count();

        if (availableSlots <= 0)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.MaxTeamsReached(maxTeams));
        }

        List<StageTeamMatch> newItems;

        if (!auto)
        {
            if (teamIds == null || teamIds.Count == 0)
            {
                return;
            }

            List<Guid> filteredIds = [.. teamIds
                .Distinct()
                .Where(id => !existingMatches.Any(stm => stm.TeamId == id))];

            if (filteredIds.Count > availableSlots)
            {
                throw new InvalidOperationException(ErrorMessages.Stage.NotEnoughSlots(filteredIds.Count, availableSlots));
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
            List<Team> teams = [.. await _teamRepository.FindAsync(
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
            await _stageTeamMatchRepository.AddRangeAsync(newItems);
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
        if (stage.Division.IsCrossDivisionCup)
        {
            return;
        }

        List<Guid> conflictingTeamIds = await FindTeamsInAnotherDivisionAsync(stage, teamIds);

        if (conflictingTeamIds.Count > 0)
        {
            throw new InvalidOperationException(
                ErrorMessages.Stage.ConflictingTeamAssignment(string.Join(", ", conflictingTeamIds)));
        }
    }

    /// <summary>
    /// Returns the subset of teamIds already assigned to a stage in a
    /// different, non-cross-division-cup division of the same tournament.
    /// </summary>
    private async Task<List<Guid>> FindTeamsInAnotherDivisionAsync(Stage stage, IEnumerable<Guid> teamIds)
    {
        List<Guid> teamIdList = [.. teamIds];
        if (teamIdList.Count == 0)
        {
            return [];
        }

        IEnumerable<StageTeamMatch> conflicting = await _stageTeamMatchRepository.FindAsync(stm =>
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
        if (teamIds == null || teamIds.Count == 0)
        {
            return;
        }

        await _stageTeamMatchRepository.RemoveAsync(stm =>
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
        Stage stage = await _stageRepository.GetByIdAsync(stageId,
            includes: [s => s.Matches, s => s.StageTeamMatches, s => s.Division])
            ?? throw new InvalidOperationException(ErrorMessages.Stage.NotFoundGeneric);

        if (stage.Matches.Count == 0)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.GenerateMatchesBeforeSeeding);
        }

        if (stage.Matches.Any(m => m.HomeTeamId.HasValue || m.VisitorTeamId.HasValue))
        {
            throw new InvalidOperationException(ErrorMessages.Stage.AlreadySeeded);
        }

        // HU-110: a cross-division cup with more than one internal group is
        // seeded by pooling the top QualifiersPerGroup teams of every group
        // and ordering them by group-stage strength, rather than from the
        // teams pre-assigned to this stage. A cross cup with a single group,
        // and every regular division, falls through to the unchanged
        // single-standings path below.
        if (stage.Division.IsCrossDivisionCup)
        {
            List<Stage> groupStages = [.. await _stageRepository.FindAsync(
                s => s.DivisionId == stage.DivisionId && s.StageType == StageType.Group)];

            if (groupStages.Count > 1)
            {
                return await SeedMultiGroupCrossCupStageAsync(stage, groupStages);
            }
        }

        List<Guid> assignedTeamIds = [.. stage.StageTeamMatches.Select(stm => stm.TeamId)];
        int slotCapacity = stage.Matches.Count * 2;

        if (assignedTeamIds.Count < 2 || assignedTeamIds.Count > slotCapacity)
        {
            throw new InvalidOperationException(
                ErrorMessages.Stage.SeedTeamCountOutOfRange(assignedTeamIds.Count, slotCapacity));
        }

        List<Match> groupMatches = [.. await _matchRepository.FindAsync(m =>
            m.Stage.DivisionId == stage.DivisionId && m.Stage.StageType == StageType.Group,
            includes: [m => m.HomeTeam!, m => m.VisitorTeam!, m => m.WinningTeam!])];

        List<Position> standings = PositionCalculator.CalculatePositions(
            groupMatches, stage.Division.PointsForWin, stage.Division.PointsForLoss);

        List<Guid> orderedTeamIds = [.. standings
            .Where(position => assignedTeamIds.Contains(position.TeamId))
            .Select(position => position.TeamId)];

        if (orderedTeamIds.Count != assignedTeamIds.Count)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.SeedMissingStandings);
        }

        List<Match> orderedMatches = await FillStageWithSeedsAsync(stage, orderedTeamIds);

        await _matchRepository.UpdateRangeAsync(orderedMatches);
        await TryAdvanceStageWinnerAsync(stage.Id);

        return orderedMatches;
    }

    /// <summary>
    /// Seeds a multi-group cross-division cup's first elimination stage
    /// (HU-110). Each internal <see cref="StageType.Group"/> stage's standings
    /// are computed independently; the top
    /// <see cref="Division.QualifiersPerGroup"/> teams of every group are
    /// pooled and ordered by group-stage strength
    /// (see <see cref="CrossCupGroupSeeder"/>), then paired into the bracket
    /// with the shared classic-seed/BYE placement. The pool must hold at least
    /// two qualifiers.
    /// </summary>
    private async Task<List<Match>> SeedMultiGroupCrossCupStageAsync(Stage stage, List<Stage> groupStages)
    {
        List<Match> groupMatches = [.. await _matchRepository.FindAsync(m =>
            m.Stage.DivisionId == stage.DivisionId && m.Stage.StageType == StageType.Group,
            includes: [m => m.HomeTeam!, m => m.VisitorTeam!, m => m.WinningTeam!])];

        List<IReadOnlyList<Position>> standingsPerGroup = [.. groupStages
            .Select(groupStage => (IReadOnlyList<Position>) PositionCalculator.CalculatePositions(
                [.. groupMatches.Where(m => m.StageId == groupStage.Id)],
                stage.Division.PointsForWin,
                stage.Division.PointsForLoss))];

        List<Guid> orderedTeamIds = CrossCupGroupSeeder.ResolveSeedOrder(
            standingsPerGroup, stage.Division.QualifiersPerGroup);

        List<Match> seeded = await FillStageWithSeedsAsync(stage, orderedTeamIds);

        await _matchRepository.UpdateRangeAsync(seeded);
        await TryAdvanceStageWinnerAsync(stage.Id);

        return seeded;
    }

    /// <summary>
    /// Seeds every playoff cup of a division from its final group-stage
    /// standings using the division's position-range mapping (HU-45/HU-81).
    /// Each mapped destination (e.g. "Copa Oro", "Copa Plata") is populated
    /// from the standings positions its range covers and seeded into the
    /// first-round elimination stage whose <see cref="Stage.BracketName"/>
    /// matches that destination, reusing the same classic bracket seeding as
    /// the single-cup path. Single-cup tournaments keep using
    /// <see cref="SeedKnockoutStageAsync"/> unchanged.
    /// </summary>
    /// <param name="divisionId">The division whose group stage has finished.</param>
    /// <returns>The seeded matches per destination cup (BracketName → matches).</returns>
    public async Task<Dictionary<string, List<Match>>> SeedPlayoffCupsAsync(Guid divisionId)
    {
        Division division = await _divisionRepository.GetByIdAsync(
            divisionId, includes: [d => d.PlayoffMappings])
            ?? throw new InvalidOperationException(ErrorMessages.Stage.DivisionNotFound);

        if (division.PlayoffMappings.Count == 0)
        {
            throw new InvalidOperationException(ErrorMessages.Playoff.NoMappingsConfigured);
        }

        List<Match> groupMatches = [.. await _matchRepository.FindAsync(m =>
            m.Stage.DivisionId == divisionId && m.Stage.StageType == StageType.Group,
            includes: [m => m.HomeTeam!, m => m.VisitorTeam!, m => m.WinningTeam!])];

        List<Position> standings = PositionCalculator.CalculatePositions(
            groupMatches, division.PointsForWin, division.PointsForLoss);

        Dictionary<string, List<Guid>> qualifiersByCup = PlayoffQualificationResolver.Resolve(
        [
            new PlayoffQualificationResolver.DivisionStandings
            {
                Standings = standings,
                Mappings = [.. division.PlayoffMappings],
            },
        ]);

        List<Stage> eliminationStages = [.. await _stageRepository.FindAsync(
            s => s.DivisionId == divisionId && s.StageType != StageType.Group,
            includes: [s => s.Matches])];

        Dictionary<string, List<Match>> seededByCup = [];

        foreach ((string destination, List<Guid> orderedTeamIds) in qualifiersByCup)
        {
            if (orderedTeamIds.Count < 2)
            {
                continue;
            }

            // A cup with more than one round (e.g. Semifinal + Final) has EVERY
            // round unseeded at this point — Stage.Order is never actually set
            // for wizard-built stages (CreateDivisionWithStagesAsync never
            // assigns it), so it cannot disambiguate which round is "first".
            // Bracket depth (EliminationProgression) can: the group-stage
            // standings always seed the EARLIEST round of the cup.
            Stage cupStage = eliminationStages
                .Where(s => s.BracketName == destination
                    && s.Matches.Count > 0
                    && !s.Matches.Any(m => m.HomeTeamId.HasValue || m.VisitorTeamId.HasValue))
                .OrderBy(s => EliminationDepth(s.StageType))
                .FirstOrDefault()
                ?? throw new InvalidOperationException(ErrorMessages.Playoff.CupStageNotFound(destination));

            List<Match> seeded = await FillStageWithSeedsAsync(cupStage, orderedTeamIds);
            await _matchRepository.UpdateRangeAsync(seeded);
            seededByCup[destination] = seeded;

            // A bye is already decided the moment it is seeded (no match needs
            // to be played) — push it into the next round right away instead of
            // waiting for a result-loading call that will never come for it.
            await TryAdvanceStageWinnerAsync(cupStage.Id);
        }

        return seededByCup;
    }

    /// <inheritdoc/>
    public async Task TryAutoSeedPlayoffPhaseAsync(Guid finishedMatchStageId)
    {
        try
        {
            Stage? finishedStage = await _stageRepository.GetByIdAsync(finishedMatchStageId);
            if (finishedStage is null || finishedStage.StageType != StageType.Group)
            {
                return;
            }

            Guid divisionId = finishedStage.DivisionId;

            List<Stage> groupStages = [.. await _stageRepository.FindAsync(
                s => s.DivisionId == divisionId && s.StageType == StageType.Group,
                includes: [s => s.Matches])];

            bool groupPhaseComplete = groupStages.Count > 0
                && groupStages.TrueForAll(s => s.Matches.Count > 0 && s.Matches.All(m => m.IsFinished));

            if (!groupPhaseComplete)
            {
                return;
            }

            Division? division = await _divisionRepository.GetByIdAsync(
                divisionId, includes: [d => d.PlayoffMappings]);

            if (division is null || division.PlayoffMappings.Count == 0)
            {
                return;
            }

            List<Stage> eliminationStages = [.. await _stageRepository.FindAsync(
                s => s.DivisionId == divisionId && s.StageType != StageType.Group,
                includes: [s => s.Matches])];

            bool anyEliminationStageSeeded = eliminationStages
                .Exists(s => s.Matches.Any(m => m.HomeTeamId.HasValue || m.VisitorTeamId.HasValue));

            // Nothing to seed, or an admin already seeded a cup by hand — auto-seed
            // only ever fires from a fully-unseeded state, so it never fights a
            // partial manual seed (SeedPlayoffCupsAsync would throw for whichever
            // cup is already done).
            if (eliminationStages.Count == 0 || anyEliminationStageSeeded)
            {
                return;
            }

            await SeedPlayoffCupsAsync(divisionId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Auto-seed skipped for the group phase of stage {StageId}: the playoff cups " +
                "could not be seeded automatically. An admin can still seed them by hand.",
                finishedMatchStageId);
        }
    }

    /// <summary>
    /// Pairs an ordered (best seed first) list of team ids into a stage's
    /// already-generated, still-empty matches using the classic bracket seed
    /// order, marking bye pairs as finished walkover wins. Shared by the
    /// single-cup and multi-cup seeding paths. When the stage plays a real
    /// series (<see cref="Stage.BestOf"/> &gt; 1), a real pairing (not a bye)
    /// gets a brand-new <see cref="MatchSeries"/> and the pre-generated empty
    /// match becomes that series' game 1 — the same treatment
    /// <see cref="TryAdvanceStageWinnerAsync"/> gives a later round once both
    /// its teams are known. A bye stays a single finished match regardless of
    /// <see cref="Stage.BestOf"/>: nothing is actually played, so there is
    /// nothing to make a series out of.
    /// </summary>
    private async Task<List<Match>> FillStageWithSeedsAsync(Stage stage, IReadOnlyList<Guid> orderedTeamIds)
    {
        List<(Guid HomeTeamId, Guid? VisitorTeamId)> pairs = PlayoffSeeder.SeedPairs(orderedTeamIds);

        List<Match> orderedMatches = [.. stage.Matches.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];

        for (int i = 0; i < pairs.Count; i++)
        {
            Match slot = orderedMatches[i];
            slot.HomeTeamId = pairs[i].HomeTeamId;
            slot.VisitorTeamId = pairs[i].VisitorTeamId;

            if (pairs[i].VisitorTeamId is null)
            {
                slot.IsFinished = true;
                slot.WinningTeamId = pairs[i].HomeTeamId;
                continue;
            }

            if (stage.BestOf > 1)
            {
                MatchSeries series = await CreateSeriesForPairAsync(
                    stage.Id, stage.BestOf, pairs[i].HomeTeamId, pairs[i].VisitorTeamId!.Value);
                slot.SeriesId = series.Id;
                slot.GameNumber = 1;
            }
        }

        return orderedMatches;
    }

    /// <summary>
    /// Creates and persists a new best-of-N series between two teams at a
    /// stage. Builds the entity directly (rather than reusing
    /// <see cref="MatchSeriesService"/>) because this runs from the trusted
    /// internal seeding/advancement path, where the pairing is already known
    /// to be correct — the public create-series endpoint's
    /// "team assigned to this stage via StageTeamMatch" guard does not apply
    /// here: elimination-stage teams are never assigned that way (they are
    /// seeded directly onto the match/series, matching how a BestOf=1 slot has
    /// always worked).
    /// </summary>
    private async Task<MatchSeries> CreateSeriesForPairAsync(Guid stageId, int bestOf, Guid homeTeamId, Guid visitorTeamId)
    {
        MatchSeries series = new()
        {
            StageId = stageId,
            HomeTeamId = homeTeamId,
            VisitorTeamId = visitorTeamId,
            BestOf = bestOf,
            CreatedBy = AuditConstants.SystemUser,
        };

        await _matchSeriesRepository.AddAsync(series);
        return series;
    }

    /// <inheritdoc/>
    public async Task TryAdvanceStageWinnerAsync(Guid decidedStageId)
    {
        try
        {
            Stage? stage = await _stageRepository.GetByIdAsync(
                decidedStageId, includes: [s => s.Matches, s => s.MatchSeries]);

            if (stage is null || stage.BracketName is null)
            {
                return;
            }

            // One entry per bracket slot: once a series' 2nd/3rd game gets
            // added (AddGameToSeriesAsync), it lands in this SAME stage's
            // Matches too — only that slot's game 1 (or its lone match, for a
            // BestOf=1/bye slot, which never gets a GameNumber) represents the
            // slot itself, so later games must be filtered out here.
            List<Match> orderedMatches = [.. stage.Matches
                .Where(m => m.GameNumber is null or 1)
                .OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];

            await AdvanceWinnersToNextRoundAsync(stage, orderedMatches);

            // The third-place decider is a side slot, not part of the main
            // advancement line (EliminationProgression skips it) — it is
            // populated separately, from the semifinal's LOSERS, once both
            // semifinal slots are decided.
            if (stage.StageType == StageType.SemiFinal)
            {
                await AdvanceLosersToThirdPlaceAsync(stage, orderedMatches);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Could not advance the winner(s)/loser(s) from stage {StageId}. " +
                "An admin can still fill the next round in by hand.",
                decidedStageId);
        }
    }

    private async Task AdvanceWinnersToNextRoundAsync(Stage stage, List<Match> orderedMatches)
    {
        StageType? nextType = NextStageType(stage.StageType);
        if (nextType is null)
        {
            return;
        }

        Stage? nextStage = (await _stageRepository.FindAsync(
            s => s.DivisionId == stage.DivisionId
                && s.BracketName == stage.BracketName
                && s.StageType == nextType.Value,
            includes: [s => s.Matches])).FirstOrDefault();

        if (nextStage is null)
        {
            return;
        }

        List<Match> nextOrderedMatches = [.. nextStage.Matches
            .Where(m => m.GameNumber is null or 1)
            .OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];

        HashSet<Match> touched = [];

        for (int slotIndex = 0; slotIndex < orderedMatches.Count; slotIndex++)
        {
            Guid? winnerId = ResolveSlotWinner(orderedMatches[slotIndex], stage);
            int nextSlotIndex = slotIndex / 2;

            if (winnerId is null || nextSlotIndex >= nextOrderedMatches.Count)
            {
                continue;
            }

            Match target = nextOrderedMatches[nextSlotIndex];
            bool isHomeSlot = slotIndex % 2 == 0;

            if (isHomeSlot)
            {
                if (target.HomeTeamId == winnerId)
                {
                    continue;
                }

                target.HomeTeamId = winnerId;
            }
            else
            {
                if (target.VisitorTeamId == winnerId)
                {
                    continue;
                }

                target.VisitorTeamId = winnerId;
            }

            touched.Add(target);
        }

        if (touched.Count == 0)
        {
            return;
        }

        // A target slot that just got its second team AND belongs to a
        // series-based round becomes game 1 of a new series — the exact
        // same treatment a freshly-seeded first round gets
        // (FillStageWithSeedsAsync).
        if (nextStage.BestOf > 1)
        {
            foreach (Match target in touched)
            {
                if (target.HomeTeamId.HasValue && target.VisitorTeamId.HasValue && !target.SeriesId.HasValue)
                {
                    MatchSeries series = await CreateSeriesForPairAsync(
                        nextStage.Id, nextStage.BestOf, target.HomeTeamId.Value, target.VisitorTeamId.Value);
                    target.SeriesId = series.Id;
                    target.GameNumber = 1;
                }
            }
        }

        await _matchRepository.UpdateRangeAsync(touched);
    }

    /// <summary>
    /// Once a semifinal slot is decided, pushes its LOSER into the division's
    /// third-place decider (same BracketName) — the same slot convention
    /// <see cref="AdvanceWinnersToNextRoundAsync"/> uses for winners: the
    /// first semifinal slot's loser becomes Home, the second's becomes
    /// Visitor. A no-op when the cup was configured with no third-place stage
    /// (<see cref="StageType.ThirdPlace"/> is optional, added via
    /// <c>qualifiersToStageTypes</c> only when the admin opts in).
    /// </summary>
    private async Task AdvanceLosersToThirdPlaceAsync(Stage semiFinalStage, List<Match> orderedMatches)
    {
        Stage? thirdPlaceStage = (await _stageRepository.FindAsync(
            s => s.DivisionId == semiFinalStage.DivisionId
                && s.BracketName == semiFinalStage.BracketName
                && s.StageType == StageType.ThirdPlace,
            includes: [s => s.Matches])).FirstOrDefault();

        if (thirdPlaceStage is null)
        {
            return;
        }

        Match? target = thirdPlaceStage.Matches
            .Where(m => m.GameNumber is null or 1)
            .OrderBy(m => m.MatchDate).ThenBy(m => m.Id)
            .FirstOrDefault();

        if (target is null)
        {
            return;
        }

        bool touched = false;

        for (int slotIndex = 0; slotIndex < orderedMatches.Count && slotIndex < 2; slotIndex++)
        {
            Guid? loserId = ResolveSlotLoser(orderedMatches[slotIndex], semiFinalStage);
            if (loserId is null)
            {
                continue;
            }

            bool isHomeSlot = slotIndex % 2 == 0;

            if (isHomeSlot)
            {
                if (target.HomeTeamId == loserId)
                {
                    continue;
                }

                target.HomeTeamId = loserId;
            }
            else
            {
                if (target.VisitorTeamId == loserId)
                {
                    continue;
                }

                target.VisitorTeamId = loserId;
            }

            touched = true;
        }

        if (!touched)
        {
            return;
        }

        if (thirdPlaceStage.BestOf > 1
            && target.HomeTeamId.HasValue && target.VisitorTeamId.HasValue
            && !target.SeriesId.HasValue)
        {
            MatchSeries series = await CreateSeriesForPairAsync(
                thirdPlaceStage.Id, thirdPlaceStage.BestOf, target.HomeTeamId.Value, target.VisitorTeamId.Value);
            target.SeriesId = series.Id;
            target.GameNumber = 1;
        }

        await _matchRepository.UpdateRangeAsync([target]);
    }

    /// <summary>
    /// The winning team of one bracket slot (a single match, or a whole
    /// series), or null when that slot is not decided yet. A series slot
    /// (<see cref="Match.SeriesId"/> set) is decided once its
    /// <see cref="MatchSeries.WinningTeamId"/> is set — a plain slot
    /// (BestOf = 1, or a bye) is decided once the match itself is finished.
    /// </summary>
    private static Guid? ResolveSlotWinner(Match slotFirstMatch, Stage stage)
    {
        if (slotFirstMatch.SeriesId.HasValue)
        {
            MatchSeries? series = stage.MatchSeries.FirstOrDefault(s => s.Id == slotFirstMatch.SeriesId.Value);
            return series?.WinningTeamId;
        }

        return slotFirstMatch.IsFinished ? slotFirstMatch.WinningTeamId : null;
    }

    /// <summary>
    /// The losing team of one decided bracket slot — the counterpart to
    /// <see cref="ResolveSlotWinner"/>, used to seed the third-place decider
    /// from semifinal losers. Null for a slot that is not decided yet, or a
    /// bye (only one side was ever assigned, so there is no real loser).
    /// </summary>
    private static Guid? ResolveSlotLoser(Match slotFirstMatch, Stage stage)
    {
        Guid? winnerId = ResolveSlotWinner(slotFirstMatch, stage);
        if (winnerId is null || slotFirstMatch.HomeTeamId is null || slotFirstMatch.VisitorTeamId is null)
        {
            return null;
        }

        return winnerId == slotFirstMatch.HomeTeamId ? slotFirstMatch.VisitorTeamId : slotFirstMatch.HomeTeamId;
    }

    /// <summary>
    /// Validates if the tournament size is a valid power of 2 and within acceptable range.
    /// </summary>
    /// <param name="teamCount">The number of teams.</param>
    /// <returns>True if valid tournament size, false otherwise.</returns>
    private static bool IsValidTournamentSize(int teamCount)
    {
        return teamCount is TournamentBracketSize.EIGHT
            or TournamentBracketSize.SIXTEEN
            or TournamentBracketSize.THIRTY_TWO
            or TournamentBracketSize.SIXTY_FOUR;
    }
}
