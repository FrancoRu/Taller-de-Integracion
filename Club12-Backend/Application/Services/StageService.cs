using Application.DTOs.Abstract.Request;
using Application.DTOs.Abstract.Response;
using Application.DTOs.Stage.Request;
using Application.DTOs.Stage.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Constants.Configuration;
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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using SubGroupDistributionHelper = Application.Utils.Helper.SubGroupDistribution.SubGroupDistribution;

namespace Application.Services;

/// <summary>
/// Manages a division's stages and their elimination bracket.
/// </summary>
public class StageService(
    IUnitOfWork unitOfWork,
    ILogger<StageService> logger,
    IConfiguration configuration,
    IAuditService auditService) : IStageService
{
    private readonly IStageRepository _stageRepository = unitOfWork.StageRepository;
    private readonly IDivisionRepository _divisionRepository = unitOfWork.DivisionRepository;
    private readonly IStageTeamMatchRepository _stageTeamMatchRepository = unitOfWork.StageTeamMatchRepository;
    private readonly ITeamRepository _teamRepository = unitOfWork.TeamRepository;
    private readonly IDivisionTeamRegistrationRepository _divisionTeamRegistrationRepository = unitOfWork.DivisionTeamRegistrationRepository;
    private readonly IMatchRepository _matchRepository = unitOfWork.MatchRepository;
    private readonly IMatchSeriesRepository _matchSeriesRepository = unitOfWork.MatchSeriesRepository;

    /// <summary>
    /// Signing secret for the draw-preview token, reusing the app's existing JWT secret rather than a new configuration key.
    /// </summary>
    private readonly string _drawTokenSecret = configuration.GetSection(ConfigurationKeys.Jwt.Key).Value
        ?? throw new ArgumentNullException(nameof(configuration), ErrorMessages.Configuration.JwtMissing);

    /// <summary>
    /// The elimination rounds a wizard-built cup can be made of, in bracket order.
    /// </summary>
    private static readonly StageType[] EliminationProgression =
    [
        StageType.RoundOf16,
        StageType.QuarterFinal,
        StageType.SemiFinal,
        StageType.Final,
    ];

    /// <summary>
    /// The stage type immediately after current in EliminationProgression, or null when there is none.
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
    /// This stage type's position in EliminationProgression, lower meaning earlier in the bracket.
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
    /// Retrieves a stage by id or slug, auto-detecting which one was passed via GUID parsing.
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
    /// Thrown when the stage's tournament has already started and its fixture is generated;
    /// removing a phase then would corrupt the bracket or fixture.
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
    /// Guards manual stage structure edits against the state of the division's tournament.
    /// </summary>
    /// <param name="divisionId">The division whose tournament is checked.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown as a 409 when the tournament has already started or was canceled.
    /// </exception>
    private async Task EnsureDivisionStructureEditableAsync(Guid divisionId)
    {
        Division? division = await _divisionRepository.GetByIdAsync(
            divisionId, includes: [division => division.Tournament]);

        if (division?.Tournament is null)
        {
            return;
        }

        bool structureLocked = division.Tournament.Status
            is TournamentStatus.Ongoing or TournamentStatus.Finished or TournamentStatus.Canceled;

        if (structureLocked)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.StructureLockedTournamentStarted);
        }
    }

    /// <summary>
    /// Updates an existing stage entity.
    /// </summary>
    /// <param name="stageEntity">The stage entity to update.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown as a 409 when the tournament has already started — editing a stage's type or dates
    /// once the fixture is generated could desync it from the matches already built off it.
    /// </exception>
    public async Task UpdateStageAsync(Stage stageEntity)
    {
        await EnsureDivisionStructureEditableAsync(stageEntity.DivisionId);

        await _stageRepository.UpdateAsync(stageEntity);
    }

    /// <summary>
    /// Creates a new stage entity if it does not already exist in the specified division.
    /// </summary>
    /// <param name="stageEntity">The stage entity to create.</param>
    /// <returns>The created stage entity.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a stage with the same name already exists in the division.
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
            await EnsureSubGroupCupCompatibilityAsync(stageEntity.DivisionId);
        }

        stageEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            stageEntity.Name,
            candidate => _stageRepository.ExistsAsync(stage => stage.Slug == candidate));

        await _stageRepository.AddAsync(stageEntity);
        return stageEntity;
    }

    /// <summary>
    /// Rejects a second sub-group in a regular division that already carries a position-range playoff cup, since that cup's range has no defined meaning across independent sub-group tables.
    /// </summary>
    private async Task EnsureSubGroupCupCompatibilityAsync(Guid divisionId)
    {
        Division? division = await _divisionRepository.GetByIdAsync(
            divisionId, includes: [d => d.PlayoffMappings]);

        if (division is null || division.IsCrossDivisionCup || division.PlayoffMappings.Count == 0)
        {
            return;
        }

        bool wouldHaveMultipleSubGroups = await _stageRepository.ExistsAsync(
            s => s.DivisionId == divisionId && s.StageType == StageType.Group);

        if (wouldHaveMultipleSubGroups)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.SubGroupsIncompatibleWithPositionRangeCups);
        }
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
    /// Assigns a unique slug to every stage in a freshly built batch before it is persisted.
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
    /// <exception cref="InvalidOperationException">
    /// Thrown if the stage already has the maximum number of teams, if too many teams are assigned,
    /// or as a 409 if the tournament has already started — adding a team to a zone after the
    /// fixture is generated would leave it without any matches of its own.
    /// </exception>
    public async Task AssignTeamsToStageAsync(Stage stage, List<Guid>? teamIds = null, bool auto = false)
    {
        await EnsureDivisionStructureEditableAsync(stage.DivisionId);

        IEnumerable<StageTeamMatch> existingMatches = await _stageTeamMatchRepository.FindAsync(stageTeamMatch => stageTeamMatch.StageId == stage.Id);

        List<Guid> filteredIds = [];

        if (!auto)
        {
            if (teamIds == null || teamIds.Count == 0)
            {
                return;
            }

            filteredIds = [.. teamIds
                .Distinct()
                .Where(id => !existingMatches.Any(stm => stm.TeamId == id))];

            await EnsureTeamsEnrolledInDivisionAsync(stage.DivisionId, filteredIds);
        }

        // MaxTeams.Group is only the auto-bracket-generator's fixed group size, not a general cap on how many teams a Group-type stage may hold, since a manually built Group stage represents a whole zone's round-robin phase and can need far more, so it is capped at the same ceiling the tournament itself enforces instead of the auto-generator's per-group size.
        int maxTeams = stage.StageType == StageType.Group
            ? MaxTeams.GroupStageCap
            : StageHelper.GetMaxTeamsForStage(stage.StageType);
        int availableSlots = maxTeams - existingMatches.Count();

        if (availableSlots <= 0)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.MaxTeamsReached(maxTeams));
        }

        List<StageTeamMatch> newItems;

        if (!auto)
        {
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
                team => team.DivisionTeamRegistrations.Any(r => r.DivisionId == stage.DivisionId)
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
    /// Throws when any of the given teams has no DivisionTeamRegistration for the division, since a stage placement is a subset of division enrollment, never the reverse.
    /// </summary>
    private async Task EnsureTeamsEnrolledInDivisionAsync(Guid divisionId, List<Guid> teamIds)
    {
        if (teamIds.Count == 0)
        {
            return;
        }

        List<Guid> registeredIds = [.. (await _divisionTeamRegistrationRepository.FindAsync(
            r => r.DivisionId == divisionId && teamIds.Contains(r.TeamId)))
            .Select(r => r.TeamId)];

        List<Guid> missingIds = [.. teamIds.Except(registeredIds)];

        if (missingIds.Count > 0)
        {
            throw new InvalidOperationException(
                ErrorMessages.Stage.TeamNotEnrolledInDivision(string.Join(", ", missingIds)));
        }
    }

    /// <summary>
    /// Throws if any given team is already assigned to a stage in a different division of the same tournament.
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
    /// Returns the subset of teamIds already assigned to a stage in a different division of the same tournament.
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
    /// <exception cref="InvalidOperationException">
    /// Thrown as a 409 when the tournament has already started — removing a team from a zone after
    /// the fixture is generated would leave its already-scheduled matches pointing at a team no
    /// longer in that zone.
    /// </exception>
    public async Task UnassignTeamsFromStageAsync(Stage stage, List<Guid> teamIds)
    {
        if (teamIds == null || teamIds.Count == 0)
        {
            return;
        }

        await EnsureDivisionStructureEditableAsync(stage.DivisionId);

        await _stageTeamMatchRepository.RemoveAsync(stm =>
            stm.StageId == stage.Id && teamIds.Contains(stm.TeamId)
        );
    }

    /// <summary>
    /// Seeds an elimination stage's empty matches from group-stage standings in classic bracket order.
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

        // A cross-division cup with more than one internal group is seeded by pooling the top QualifiersPerGroup teams of every group and ordering them by group-stage strength rather than the teams pre-assigned to this stage; a cross cup with a single group, and every regular division, falls through to the single-standings path below.
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
    /// Seeds a multi-group cross-division cup's first elimination stage.
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
    /// Seeds every playoff cup of a division from its final group-stage standings.
    /// </summary>
    /// <param name="divisionId">The division whose group stage has finished.</param>
    /// <returns>The seeded matches per destination cup, keyed by BracketName.</returns>
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

            // A cup with more than one round has every round unseeded at this point, and Stage.Order is never actually set for wizard-built stages, so it cannot disambiguate which round is first; bracket depth via EliminationProgression can, since the group-stage standings always seed the earliest round of the cup.
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

            // A bye is already decided the moment it is seeded since no match needs to be played, so it is pushed into the next round right away instead of waiting for a result-loading call that will never come.
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

            // Nothing to seed, or an admin already seeded a cup by hand: auto-seed only ever fires from a fully-unseeded state, so it never fights a partial manual seed since SeedPlayoffCupsAsync would throw for whichever cup is already done.
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
    /// Pairs an ordered, best-seed-first list of team ids into a stage's empty matches in classic bracket seed order.
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
    /// Creates and persists a new best-of-N series between two teams at a stage.
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

            // One entry per bracket slot: once a series' second or third game gets added, it lands in this same stage's Matches too, so only that slot's game 1, or its lone match for a bye slot which never gets a GameNumber, represents the slot itself and later games must be filtered out here.
            List<Match> orderedMatches = [.. stage.Matches
                .Where(m => m.GameNumber is null or 1)
                .OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];

            await AdvanceWinnersToNextRoundAsync(stage, orderedMatches);

            // The third-place decider is a side slot, not part of the main advancement line since EliminationProgression skips it; it is populated separately, from the semifinal's losers, once both semifinal slots are decided.
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

        // A target slot that just got its second team and belongs to a series-based round becomes game 1 of a new series, the same treatment a freshly-seeded first round gets.
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
    /// Once a semifinal slot is decided, pushes its loser into the division's third-place decider.
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
    /// The winning team of one bracket slot, a single match or a whole series, or null if undecided.
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
    /// The losing team of one decided bracket slot, the counterpart to ResolveSlotWinner.
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

    /// <inheritdoc/>
    public async Task<DrawPreviewResult> PreviewDrawAsync(Guid stageId, DrawMode mode, List<Guid>? manualOrder = null)
    {
        Stage stage = await _stageRepository.GetByIdAsync(stageId, includes: [s => s.Division])
            ?? throw new InvalidOperationException(ErrorMessages.Stage.NotFoundGeneric);

        await EnsureGrouplessDivisionAsync(stage.DivisionId);

        List<Guid> rosterTeamIds = await GetRosterTeamIdsAsync(stage.DivisionId);
        List<Guid> orderedTeamIds = ResolveOrderedTeamIds(mode, rosterTeamIds, manualOrder);

        List<(Guid HomeTeamId, Guid? VisitorTeamId)> pairs = PlayoffSeeder.SeedPairs(orderedTeamIds);

        return new DrawPreviewResult
        {
            Pairs = [.. pairs.Select(p => new DrawPairPreview { HomeTeamId = p.HomeTeamId, VisitorTeamId = p.VisitorTeamId })],
            DrawToken = SignDrawToken(stageId, orderedTeamIds),
        };
    }

    /// <inheritdoc/>
    public async Task<List<Match>> CommitDrawAsync(Guid stageId, DrawMode mode, string? drawToken = null, List<Guid>? manualOrder = null)
    {
        Stage firstRoundStage = await _stageRepository.GetByIdAsync(stageId, includes: [s => s.Matches, s => s.Division])
            ?? throw new InvalidOperationException(ErrorMessages.Stage.NotFoundGeneric);

        await EnsureGrouplessDivisionAsync(firstRoundStage.DivisionId);

        List<Guid> rosterTeamIds = await GetRosterTeamIdsAsync(firstRoundStage.DivisionId);

        List<Guid> orderedTeamIds = mode == DrawMode.Random
            ? VerifyDrawToken(drawToken, stageId, rosterTeamIds)
            : ResolveOrderedTeamIds(DrawMode.Manual, rosterTeamIds, manualOrder);

        await EnsureBracketDrawableAsync(firstRoundStage);

        List<Stage> bracketStages = [.. await _stageRepository.FindAsync(
            s => s.DivisionId == firstRoundStage.DivisionId && s.BracketName == firstRoundStage.BracketName,
            includes: [s => s.Matches])];

        await ResetBracketSeedingAsync(bracketStages);

        List<Match> orderedMatches = await FillStageWithSeedsAsync(firstRoundStage, orderedTeamIds);
        await _matchRepository.UpdateRangeAsync(orderedMatches);

        firstRoundStage.DrawnAt = DateTime.UtcNow;
        await _stageRepository.UpdateAsync(firstRoundStage);

        await TryAdvanceStageWinnerAsync(firstRoundStage.Id);

        await LogPlayoffDrawAsync(firstRoundStage, mode, orderedTeamIds.Count);

        return orderedMatches;
    }

    /// <summary>
    /// Returns every team id currently enrolled in the division's roster.
    /// </summary>
    private async Task<List<Guid>> GetRosterTeamIdsAsync(Guid divisionId)
    {
        return [.. (await _divisionTeamRegistrationRepository.FindAsync(r => r.DivisionId == divisionId))
            .Select(r => r.TeamId)];
    }

    /// <summary>
    /// Rejects a playoffs-only draw against a division that still has a group phase, since group-standings brackets are seeded by SeedKnockoutStageAsync instead.
    /// </summary>
    private async Task EnsureGrouplessDivisionAsync(Guid divisionId)
    {
        bool hasGroupStage = await _stageRepository.ExistsAsync(
            s => s.DivisionId == divisionId && s.StageType == StageType.Group);

        if (hasGroupStage)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.DrawRequiresGrouplessDivision);
        }
    }

    /// <summary>
    /// Resolves the ordered team list a draw seeds from, shuffling for a random draw or validating a manual order against the roster set.
    /// </summary>
    private static List<Guid> ResolveOrderedTeamIds(DrawMode mode, List<Guid> rosterTeamIds, List<Guid>? manualOrder)
    {
        if (mode == DrawMode.Manual)
        {
            if (manualOrder is null || !IsRosterPermutation(manualOrder, rosterTeamIds))
            {
                throw new InvalidOperationException(ErrorMessages.Stage.ManualOrderNotRosterPermutation);
            }

            return manualOrder;
        }

        List<Guid> shuffled = [.. rosterTeamIds];
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Shared.Next(i + 1);
            (shuffled[swapIndex], shuffled[i]) = (shuffled[i], shuffled[swapIndex]);
        }

        return shuffled;
    }

    /// <summary>
    /// Whether candidate is exactly a reordering of roster, with no team missing, repeated, or foreign to the division.
    /// </summary>
    private static bool IsRosterPermutation(List<Guid> candidate, List<Guid> roster)
    {
        if (candidate.Count != roster.Count)
        {
            return false;
        }

        HashSet<Guid> candidateSet = [.. candidate];
        return candidateSet.Count == candidate.Count && candidateSet.SetEquals(roster);
    }

    /// <summary>
    /// Blocks a bracket draw once any real match in that division and bracket name has been played, excluding byes and still-empty slots so a freshly drawn bracket stays re-drawable.
    /// </summary>
    private async Task EnsureBracketDrawableAsync(Stage firstRoundStage)
    {
        bool anyPlayed = await _matchRepository.ExistsAsync(m =>
            m.Stage.DivisionId == firstRoundStage.DivisionId
            && m.Stage.BracketName == firstRoundStage.BracketName
            && m.HomeTeamId.HasValue && m.VisitorTeamId.HasValue
            && (m.IsFinished || m.HomeScore.HasValue || m.VisitorScore.HasValue || m.Status == MatchStatus.Played));

        if (anyPlayed)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.BracketAlreadyPlayed);
        }
    }

    /// <summary>
    /// Clears every match of the bracket's stages back to unseeded and removes their series, a no-op the first time a bracket is drawn.
    /// </summary>
    private async Task ResetBracketSeedingAsync(List<Stage> bracketStages)
    {
        List<Match> matchesToReset = [.. bracketStages.SelectMany(s => s.Matches)];

        if (matchesToReset.Count == 0)
        {
            return;
        }

        foreach (Match match in matchesToReset)
        {
            match.HomeTeamId = null;
            match.VisitorTeamId = null;
            match.WinningTeamId = null;
            match.HomeScore = null;
            match.VisitorScore = null;
            match.IsFinished = false;
            match.Status = MatchStatus.Scheduled;
            match.SeriesId = null;
            match.GameNumber = null;
        }

        await _matchRepository.UpdateRangeAsync(matchesToReset);

        List<Guid> stageIds = [.. bracketStages.Select(s => s.Id)];
        await _matchSeriesRepository.RemoveAsync(series => stageIds.Contains(series.StageId));
    }

    /// <summary>
    /// Writes the PlayoffDraw audit entry for a committed draw, logging any failure instead of raising it so an audit outage never blocks the draw itself.
    /// </summary>
    private async Task LogPlayoffDrawAsync(Stage firstRoundStage, DrawMode mode, int teamCount)
    {
        try
        {
            string detail = mode == DrawMode.Random
                ? $"Sorteo aleatorio - {teamCount} equipos"
                : $"Sorteo manual - {teamCount} equipos";

            await auditService.LogAsync(
                AuditAction.PlayoffDraw,
                targetType: "Stage",
                targetId: firstRoundStage.Id.ToString(),
                targetName: firstRoundStage.Name,
                detail: detail);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Could not write the PlayoffDraw audit entry for stage {StageId}. The draw itself still succeeded.",
                firstRoundStage.Id);
        }
    }

    /// <summary>
    /// Signs a base64url draw-preview token binding the stage id and exact seeded order, so a later commit call can replay the identical pairing.
    /// </summary>
    private string SignDrawToken(Guid stageId, List<Guid> orderedTeamIds)
    {
        DrawTokenPayload payload = new()
        {
            StageId = stageId,
            OrderedTeamIds = orderedTeamIds,
            IssuedAtUtc = DateTime.UtcNow,
            Nonce = Guid.NewGuid(),
        };

        string payloadPart = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        string signaturePart = Base64UrlEncode(ComputeSignature(payloadPart));

        return $"{payloadPart}.{signaturePart}";
    }

    /// <summary>
    /// Verifies a draw token's signature, stage match, and exact roster-set match, throwing for anything tampered, expired, or mismatched.
    /// </summary>
    private List<Guid> VerifyDrawToken(string? token, Guid expectedStageId, List<Guid> expectedRosterTeamIds)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(ErrorMessages.Stage.InvalidDrawToken);
        }

        string[] parts = token.Split('.');
        if (parts.Length != 2)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.InvalidDrawToken);
        }

        DrawTokenPayload payload;
        try
        {
            byte[] providedSignature = Base64UrlDecode(parts[1]);
            byte[] expectedSignature = ComputeSignature(parts[0]);

            if (!CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
            {
                throw new InvalidOperationException(ErrorMessages.Stage.InvalidDrawToken);
            }

            payload = JsonSerializer.Deserialize<DrawTokenPayload>(Base64UrlDecode(parts[0]))
                ?? throw new InvalidOperationException(ErrorMessages.Stage.InvalidDrawToken);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.InvalidDrawToken);
        }

        if (payload.StageId != expectedStageId || !IsRosterPermutation(payload.OrderedTeamIds, expectedRosterTeamIds))
        {
            throw new InvalidOperationException(ErrorMessages.Stage.InvalidDrawToken);
        }

        return payload.OrderedTeamIds;
    }

    /// <summary>
    /// The HMAC-SHA256 signature of value using the app's JWT signing secret.
    /// </summary>
    private byte[] ComputeSignature(string value)
    {
        using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(_drawTokenSecret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        string padded = value.Replace('-', '+').Replace('_', '/');
        int remainder = padded.Length % 4;
        if (remainder > 0)
        {
            padded += new string('=', 4 - remainder);
        }

        return Convert.FromBase64String(padded);
    }

    /// <inheritdoc/>
    public async Task<List<Stage>> RebuildSubGroupsAsync(Guid divisionId, int subGroupCount)
    {
        if (subGroupCount < 1)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.SubGroupCountMustBePositive);
        }

        await EnsureDivisionStructureEditableAsync(divisionId);

        Division division = await _divisionRepository.GetByIdAsync(
            divisionId, includes: [d => d.PlayoffMappings, d => d.Tournament])
            ?? throw new InvalidOperationException(ErrorMessages.Stage.DivisionNotFound);

        if (subGroupCount >= 2 && !division.IsCrossDivisionCup && division.PlayoffMappings.Count > 0)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.SubGroupsIncompatibleWithPositionRangeCups);
        }

        List<Guid> rosterTeamIds = await GetRosterTeamIdsAsync(divisionId);

        if (rosterTeamIds.Count > 0 && !SubGroupDistributionHelper.MeetsMinimumSize(rosterTeamIds.Count, subGroupCount))
        {
            throw new InvalidOperationException(
                ErrorMessages.Stage.SubGroupTooFewTeams(rosterTeamIds.Count, subGroupCount));
        }

        List<Guid> existingGroupStageIds = [.. (await _stageRepository.FindAsync(
            s => s.DivisionId == divisionId && s.StageType == StageType.Group))
            .Select(s => s.Id)];

        if (existingGroupStageIds.Count > 0)
        {
            await _stageRepository.RemoveAsync(s => existingGroupStageIds.Contains(s.Id));
        }

        List<Stage> newGroupStages = BuildSubGroupStages(division, subGroupCount);
        await AssignStageSlugsAsync(newGroupStages);
        await _stageRepository.AddRangeAsync(newGroupStages);

        if (rosterTeamIds.Count > 0)
        {
            await PlaceRosterIntoSubGroupsAsync(rosterTeamIds, newGroupStages);
        }

        return newGroupStages;
    }

    /// <inheritdoc/>
    public async Task AutoDistributeRosterAsync(Guid divisionId)
    {
        await EnsureDivisionStructureEditableAsync(divisionId);

        List<Guid> rosterTeamIds = await GetRosterTeamIdsAsync(divisionId);

        List<Stage> groupStages = [.. await _stageRepository.FindAsync(
            s => s.DivisionId == divisionId && s.StageType == StageType.Group)];

        if (groupStages.Count == 0)
        {
            return;
        }

        List<Guid> groupStageIds = [.. groupStages.Select(s => s.Id)];
        await _stageTeamMatchRepository.RemoveAsync(stm => groupStageIds.Contains(stm.StageId));

        if (rosterTeamIds.Count > 0)
        {
            await PlaceRosterIntoSubGroupsAsync(rosterTeamIds, groupStages);
        }
    }

    /// <inheritdoc/>
    public async Task ReassignTeamToSubGroupAsync(Guid teamId, Guid fromStageId, Guid toStageId)
    {
        if (fromStageId == toStageId)
        {
            return;
        }

        Stage fromStage = await _stageRepository.GetByIdAsync(fromStageId)
            ?? throw new InvalidOperationException(ErrorMessages.Stage.NotFoundGeneric);
        Stage toStage = await _stageRepository.GetByIdAsync(toStageId)
            ?? throw new InvalidOperationException(ErrorMessages.Stage.NotFoundGeneric);

        if (fromStage.DivisionId != toStage.DivisionId)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.ReassignmentAcrossDivisionsNotAllowed);
        }

        await EnsureDivisionStructureEditableAsync(fromStage.DivisionId);

        StageTeamMatch placement = (await _stageTeamMatchRepository.FindAsync(
            stm => stm.StageId == fromStageId && stm.TeamId == teamId)).FirstOrDefault()
            ?? throw new InvalidOperationException(ErrorMessages.Stage.TeamNotPlacedInSubGroup);

        // The minimum sub-group size is the only hard constraint on a manual move: the organizer
        // may otherwise move a team for any reason, geography, avoiding rivals, or anything else,
        // without the system second-guessing the destination's resulting balance.
        int remainingInSource = await _stageTeamMatchRepository.CountAsync(
            stm => stm.StageId == fromStageId) - 1;

        if (remainingInSource < SubGroupDistributionHelper.MinTeamsPerSubGroup)
        {
            throw new InvalidOperationException(
                ErrorMessages.Stage.SubGroupReassignmentBelowMinimum(remainingInSource));
        }

        placement.StageId = toStageId;
        await _stageTeamMatchRepository.UpdateAsync(placement);
    }

    /// <summary>
    /// Builds subGroupCount fresh Group stages named "Grupo A" onward, ordered, for a division rebuild.
    /// </summary>
    private static List<Stage> BuildSubGroupStages(Division division, int subGroupCount)
    {
        List<Stage> stages = [];
        DateTime startDate = division.Tournament.StartDate;

        for (int i = 0; i < subGroupCount; i++)
        {
            Stage groupStage = BuildStage(StageType.Group, StageTemplate.Group, startDate, division, daysMultiplier: 2);
            groupStage.Name = $"Grupo {(char) ('A' + i)}";
            groupStage.Order = i;
            stages.Add(groupStage);
        }

        return stages;
    }

    /// <summary>
    /// Deals the roster across the given sub-group stages using the balanced distribution rule and persists the resulting placements.
    /// </summary>
    private async Task PlaceRosterIntoSubGroupsAsync(List<Guid> rosterTeamIds, List<Stage> groupStages)
    {
        List<List<Guid>> distribution = SubGroupDistributionHelper.Distribute(rosterTeamIds, groupStages.Count);

        List<StageTeamMatch> newMatches = [];
        for (int i = 0; i < groupStages.Count; i++)
        {
            newMatches.AddRange(distribution[i].Select(teamId => new StageTeamMatch
            {
                StageId = groupStages[i].Id,
                TeamId = teamId,
                CreatedBy = AuditConstants.SystemUser,
                DateCreated = DateTime.UtcNow,
            }));
        }

        if (newMatches.Count > 0)
        {
            await _stageTeamMatchRepository.AddRangeAsync(newMatches);
        }
    }

    /// <summary>
    /// The signed payload carried by a draw-preview token, replayed verbatim by a matching commit call.
    /// </summary>
    private sealed class DrawTokenPayload
    {
        public Guid StageId { get; set; }
        public List<Guid> OrderedTeamIds { get; set; } = [];
        public DateTime IssuedAtUtc { get; set; }
        public Guid Nonce { get; set; }
    }
}
