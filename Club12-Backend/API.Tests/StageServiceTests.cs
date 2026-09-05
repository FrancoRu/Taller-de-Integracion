using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Constants.Stage;
using Application.Utils.Helper.StageHelper;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Characterization (approval) tests for StageService's
/// automated stage-chain generation (IStageService.CreateAutomatedStagesAsync) and
/// team-assignment logic (IStageService.AssignTeamsToStageAsync). These tests pin
/// the currently-correct behavior of the existing, unmodified service — they are pure test
/// additions and assert no production code changes.
/// </summary>
public class StageServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StageServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Both groups run in parallel with identical start/end dates. The chain
    /// gaps asserted below (e.g. groupA.EndDate.AddDays(2) for semiFinal.StartDate)
    /// are the ones documented in StageService.CreateAutomatedStagesAsync.
    /// </summary>
    [Fact]
    public async Task CreateAutomatedStagesAsync_EightTeams_CreatesTwoGroupsWithoutQuarterFinal()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Tournament tournament, _) = await SeedTournamentWithTeamsAsync(db, 8);
        Division division = await SeedDivisionAsync(db, tournament);

        List<Stage> stages = await stageService.CreateAutomatedStagesAsync(division.Id);

        Assert.Equal(5, stages.Count);
        Assert.DoesNotContain(stages, s => s.StageType == StageType.QuarterFinal);

        Stage groupA = Assert.Single(stages, s => s.Name == $"{StageTemplate.Group.Name} - Grupo A");
        Stage groupB = Assert.Single(stages, s => s.Name == $"{StageTemplate.Group.Name} - Grupo B");
        Stage semiFinal = Assert.Single(stages, s => s.StageType == StageType.SemiFinal);
        Stage thirdPlace = Assert.Single(stages, s => s.StageType == StageType.ThirdPlace);
        Stage final = Assert.Single(stages, s => s.StageType == StageType.Final);

        Assert.Equal(tournament.StartDate, groupA.StartDate);
        Assert.Equal(tournament.StartDate, groupB.StartDate);
        Assert.Equal(tournament.StartDate.AddDays(StageTemplate.DurationDays * 2), groupA.EndDate);
        Assert.Equal(groupA.EndDate, groupB.EndDate);

        Assert.Equal(groupA.EndDate.AddDays(2), semiFinal.StartDate);
        Assert.Equal(semiFinal.StartDate.AddDays(StageTemplate.DurationDays), semiFinal.EndDate);
        Assert.Equal(semiFinal.EndDate.AddDays(1), thirdPlace.StartDate);
        Assert.Equal(thirdPlace.StartDate.AddDays(StageTemplate.DurationDays), thirdPlace.EndDate);
        Assert.Equal(thirdPlace.EndDate.AddDays(2), final.StartDate);
        Assert.Equal(final.StartDate.AddDays(StageTemplate.DurationDays), final.EndDate);

        Assert.Equal(0, groupA.Order);
        Assert.Equal(1, groupB.Order);
        Assert.Equal(2, semiFinal.Order);
        Assert.Equal(3, thirdPlace.Order);
        Assert.Equal(4, final.Order);
    }

    public static readonly TheoryData<int, int> ValidSizesWithQuarterFinal = new()
    {
        { 16, 4 },
        { 32, 8 },
        { 64, 16 },
    };

    [Theory]
    [MemberData(nameof(ValidSizesWithQuarterFinal))]
    public async Task CreateAutomatedStagesAsync_ValidSizesWithQuarterFinal_CreatesExpectedGroupsAndChain(
        int teamCount, int expectedGroupCount)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Tournament tournament, _) = await SeedTournamentWithTeamsAsync(db, teamCount);
        Division division = await SeedDivisionAsync(db, tournament);

        List<Stage> stages = await stageService.CreateAutomatedStagesAsync(division.Id);

        Assert.Equal(expectedGroupCount + 4, stages.Count);

        List<Stage> groupStages = [.. stages.Where(s => s.StageType == StageType.Group).OrderBy(s => s.Order)];
        Assert.Equal(expectedGroupCount, groupStages.Count);

        for (int i = 0; i < expectedGroupCount; i++)
        {
            char expectedLetter = (char) ('A' + i);
            Assert.Equal($"{StageTemplate.Group.Name} - Grupo {expectedLetter}", groupStages[i].Name);
            Assert.Equal(i, groupStages[i].Order);
            Assert.Equal(tournament.StartDate, groupStages[i].StartDate);
        }

        Stage quarterFinal = Assert.Single(stages, s => s.StageType == StageType.QuarterFinal);
        Stage semiFinal = Assert.Single(stages, s => s.StageType == StageType.SemiFinal);
        Stage thirdPlace = Assert.Single(stages, s => s.StageType == StageType.ThirdPlace);
        Stage final = Assert.Single(stages, s => s.StageType == StageType.Final);

        Assert.True(quarterFinal.Order < semiFinal.Order);
        Assert.True(semiFinal.Order < thirdPlace.Order);
        Assert.True(thirdPlace.Order < final.Order);

        DateTime groupEnd = groupStages[0].EndDate;
        Assert.Equal(groupEnd.AddDays(2), quarterFinal.StartDate);
        Assert.Equal(quarterFinal.StartDate.AddDays(StageTemplate.DurationDays), quarterFinal.EndDate);
        Assert.Equal(quarterFinal.EndDate.AddDays(2), semiFinal.StartDate);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(12)]
    public async Task CreateAutomatedStagesAsync_InvalidTeamCount_ThrowsAndCreatesNoStages(int teamCount)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Tournament tournament, _) = await SeedTournamentWithTeamsAsync(db, teamCount);
        Division division = await SeedDivisionAsync(db, tournament);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CreateAutomatedStagesAsync(division.Id));

        int stageCount = await db.Stages.CountAsync(s => s.DivisionId == division.Id);
        Assert.Equal(0, stageCount);
    }

    [Fact]
    public async Task CreateAutomatedStagesAsync_DivisionNotFound_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CreateAutomatedStagesAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAutomatedStagesAsync_DivisionAlreadyHasStages_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Tournament tournament, _) = await SeedTournamentWithTeamsAsync(db, 8);
        Division division = await SeedDivisionAsync(db, tournament, withStages: true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CreateAutomatedStagesAsync(division.Id));
    }

    /// <summary>
    /// Regression test for a real bug found driving the manual "Nueva Fase"
    /// admin form live: creating a second Group-type stage in a division
    /// that already has one was silently allowed as long as the new stage's
    /// name differed from the existing one's (CreateStageAsync only ever
    /// checked for an exact name collision). A division's Group stage
    /// represents its whole round-robin phase, so a second one is an
    /// orphaned, ambiguous fixture — this must be rejected regardless of
    /// the new stage's name.
    /// </summary>
    [Fact]
    public async Task CreateStageAsync_DivisionAlreadyHasGroupStage_ThrowsEvenWithDifferentName()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament, withStages: true);

        Stage secondGroupStage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Totally-Different-Name-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CreateStageAsync(secondGroupStage));

        Assert.Equal(ErrorMessages.Stage.GroupStageAlreadyExistsInDivision, exception.Message);
    }

    /// <summary>
    /// Non-Group stage types (elimination rounds) are unaffected by the
    /// one-Group-stage-per-division rule: a division can have many.
    /// </summary>
    [Fact]
    public async Task CreateStageAsync_DivisionAlreadyHasGroupStage_StillAllowsNonGroupStage()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament, withStages: true);

        Stage semiFinalStage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Semifinal-{Guid.NewGuid()}",
            StageType = StageType.SemiFinal,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        Stage created = await stageService.CreateStageAsync(semiFinalStage);

        Assert.Equal(StageType.SemiFinal, created.StageType);
    }

    /// <summary>
    /// HU-110: a cross-division cup is seeded by pooling the top teams of
    /// SEVERAL internal group stages, so — unlike a regular division — it may
    /// hold more than one Group stage.
    /// </summary>
    [Fact]
    public async Task CreateStageAsync_CrossDivisionCup_AllowsSecondGroupStage()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament, withStages: true, isCrossDivisionCup: true);

        Stage secondGroupStage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Grupo B - {Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        Stage created = await stageService.CreateStageAsync(secondGroupStage);

        Assert.Equal(StageType.Group, created.StageType);
        Assert.Equal(2, await db.Stages.CountAsync(s => s.DivisionId == division.Id && s.StageType == StageType.Group));
    }

    /// <summary>
    /// A phase (stage) cannot be added to a division once its tournament has
    /// started (fixture generated) or was canceled: Ongoing, Finished and
    /// Canceled all lock the phase structure — Ongoing/Finished because the
    /// matches already reference the existing stage set and a new stage would
    /// corrupt the bracket, Canceled because a dead tournament's structure must
    /// stay frozen regardless of how far it got.
    /// </summary>
    [Theory]
    [InlineData(TournamentStatus.Ongoing)]
    [InlineData(TournamentStatus.Finished)]
    [InlineData(TournamentStatus.Canceled)]
    public async Task CreateStageAsync_TournamentStartedOrCanceled_ThrowsAndCreatesNoStage(TournamentStatus status)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db, status);
        Division division = await SeedDivisionAsync(db, tournament);

        Stage newStage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Semifinal-{Guid.NewGuid()}",
            StageType = StageType.SemiFinal,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CreateStageAsync(newStage));

        Assert.Equal(ErrorMessages.Stage.StructureLockedTournamentStarted, exception.Message);
        Assert.Equal(0, await db.Stages.CountAsync(s => s.DivisionId == division.Id));
    }

    /// <summary>
    /// While the tournament structure is still editable (no fixture yet) a
    /// phase can be added. RegistrationClosed is the last editable state before
    /// the tournament starts.
    /// </summary>
    [Theory]
    [InlineData(TournamentStatus.OpenForRegistration)]
    [InlineData(TournamentStatus.RegistrationClosed)]
    public async Task CreateStageAsync_TournamentStructureEditable_CreatesStage(TournamentStatus status)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db, status);
        Division division = await SeedDivisionAsync(db, tournament);

        Stage newStage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Semifinal-{Guid.NewGuid()}",
            StageType = StageType.SemiFinal,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        Stage created = await stageService.CreateStageAsync(newStage);

        Assert.Equal(StageType.SemiFinal, created.StageType);
        Assert.Equal(1, await db.Stages.CountAsync(s => s.DivisionId == division.Id));
    }

    /// <summary>
    /// Removing a phase once the tournament has started is likewise blocked,
    /// and the stage is left untouched.
    /// </summary>
    [Fact]
    public async Task DeleteStageAsync_TournamentStarted_ThrowsAndKeepsStage()
    {
        Guid stageId;
        Guid divisionId;

        using (IServiceScope seedScope = _factory.Services.CreateScope())
        {
            ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(seedDb, TournamentStatus.Ongoing);
            Division division = await SeedDivisionAsync(seedDb, tournament, withStages: true);
            divisionId = division.Id;
            stageId = await seedDb.Stages.Where(s => s.DivisionId == division.Id).Select(s => s.Id).FirstAsync();
        }

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.DeleteStageAsync(stageId));

        Assert.Equal(ErrorMessages.Stage.StructureLockedTournamentStarted, exception.Message);
        Assert.Equal(1, await db.Stages.CountAsync(s => s.DivisionId == divisionId));
    }

    /// <summary>
    /// Editing a stage's own fields (dates, type, ...) once the tournament has
    /// started is likewise blocked — a prior gap let this through even though
    /// creating/deleting a stage was already guarded.
    /// </summary>
    [Fact]
    public async Task UpdateStageAsync_TournamentStarted_ThrowsAndKeepsOriginalDates()
    {
        Guid stageId;
        DateTime originalEndDate;

        using (IServiceScope seedScope = _factory.Services.CreateScope())
        {
            ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(seedDb, TournamentStatus.Ongoing);
            Division division = await SeedDivisionAsync(seedDb, tournament, withStages: true);
            Stage seededStage = await seedDb.Stages.FirstAsync(s => s.DivisionId == division.Id);
            stageId = seededStage.Id;
            originalEndDate = seededStage.EndDate;
        }

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Stage toUpdate = await db.Stages.FirstAsync(s => s.Id == stageId);
        toUpdate.EndDate = toUpdate.EndDate.AddDays(30);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.UpdateStageAsync(toUpdate));

        Assert.Equal(ErrorMessages.Stage.StructureLockedTournamentStarted, exception.Message);

        Stage reloaded = await db.Stages.AsNoTracking().FirstAsync(s => s.Id == stageId);
        Assert.Equal(originalEndDate, reloaded.EndDate);
    }

    /// <summary>
    /// Assigning a new team to a zone once the tournament has started is
    /// blocked — it would sit in the zone's standings without ever getting a
    /// fixture, since matches are only generated once, at tournament start.
    /// </summary>
    [Fact]
    public async Task AssignTeamsToStageAsync_TournamentStarted_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Group-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };
        db.Stages.Add(stage);
        await db.SaveChangesAsync();
        List<Team> teams = await SeedTeamsAsync(db, tournament, 1);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.AssignTeamsToStageAsync(stage, [teams[0].Id]));

        Assert.Equal(ErrorMessages.Stage.StructureLockedTournamentStarted, exception.Message);
        Assert.Equal(0, await db.StageTeamMatches.CountAsync(stm => stm.StageId == stage.Id));
    }

    /// <summary>
    /// Removing a team from a zone once the tournament has started is
    /// blocked — its already-generated matches would keep referencing a team
    /// no longer assigned to that zone's standings.
    /// </summary>
    [Fact]
    public async Task UnassignTeamsFromStageAsync_TournamentStarted_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Group-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };
        db.Stages.Add(stage);
        await db.SaveChangesAsync();
        List<Team> teams = await SeedTeamsAsync(db, tournament, 1);
        db.StageTeamMatches.Add(new StageTeamMatch
        {
            StageId = stage.Id,
            TeamId = teams[0].Id,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.UnassignTeamsFromStageAsync(stage, [teams[0].Id]));

        Assert.Equal(ErrorMessages.Stage.StructureLockedTournamentStarted, exception.Message);
        Assert.Equal(1, await db.StageTeamMatches.CountAsync(stm => stm.StageId == stage.Id));
    }

    [Fact]
    public async Task AssignTeamsToStageAsync_ExactSlotMatch_AssignsAllTeams()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Stage stage, Tournament tournament, _) = await SeedStageWithSlotsAsync(db, StageType.SemiFinal, existingAssignmentCount: 0);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 4);
        List<Guid> teamIds = [.. poolTeams.Select(t => t.Id)];

        await stageService.AssignTeamsToStageAsync(stage, teamIds, auto: false);

        List<StageTeamMatch> records = await db.StageTeamMatches.Where(stm => stm.StageId == stage.Id).ToListAsync();
        Assert.Equal(4, records.Count);

        List<Guid> expectedIds = [.. teamIds.OrderBy(id => id)];
        List<Guid> actualIds = [.. records.Select(r => r.TeamId).OrderBy(id => id)];
        Assert.Equal(expectedIds, actualIds);
    }

    /// <summary>
    /// Regression test for a NullReferenceException at
    /// StageService.EnsureNoCrossDivisionConflictAsync (stage.Division.IsCrossDivisionCup)
    /// that only reproduced when the stage came from GetStageByIdAsync's real EF Core
    /// round-trip in a fresh scope — every other test seeds a Stage with Division set
    /// directly in-memory, which masked the missing repository include.
    /// </summary>
    [Fact]
    public async Task AssignTeamsToStageAsync_StageFetchedByIdInFreshScope_DoesNotThrow()
    {
        Guid stageId;
        Guid tournamentId;
        using (IServiceScope seedScope = _factory.Services.CreateScope())
        {
            ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            (Stage stage, Tournament tournament, _) = await SeedStageWithSlotsAsync(seedDb, StageType.SemiFinal, existingAssignmentCount: 0);
            stageId = stage.Id;
            tournamentId = tournament.Id;
        }

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament2 = await db.Tournaments.FirstAsync(t => t.Id == tournamentId);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament2, 4);
        List<Guid> teamIds = [.. poolTeams.Select(t => t.Id)];

        Stage? fetchedStage = await stageService.GetStageByIdAsync(stageId);
        Assert.NotNull(fetchedStage);

        await stageService.AssignTeamsToStageAsync(fetchedStage, teamIds, auto: false);

        int recordCount = await db.StageTeamMatches.CountAsync(stm => stm.StageId == stageId);
        Assert.Equal(4, recordCount);
    }

    /// <summary>
    /// ThirdPlace capacity is 2; 1 existing assignment leaves exactly 1 available slot.
    /// </summary>
    [Fact]
    public async Task AssignTeamsToStageAsync_TooManyTeamsForSlots_ThrowsAndCreatesNoRecords()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Stage stage, Tournament tournament, _) = await SeedStageWithSlotsAsync(db, StageType.ThirdPlace, existingAssignmentCount: 1);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 3);
        List<Guid> teamIds = [.. poolTeams.Select(t => t.Id)];

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.AssignTeamsToStageAsync(stage, teamIds, auto: false));

        int recordCount = await db.StageTeamMatches.CountAsync(stm => stm.StageId == stage.Id);
        Assert.Equal(1, recordCount);
    }

    [Fact]
    public async Task AssignTeamsToStageAsync_FewerTeamsThanSlots_LeavesSlotsAvailable()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Stage stage, Tournament tournament, _) = await SeedStageWithSlotsAsync(db, StageType.SemiFinal, existingAssignmentCount: 0);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 2);
        List<Guid> teamIds = [.. poolTeams.Select(t => t.Id)];

        await stageService.AssignTeamsToStageAsync(stage, teamIds, auto: false);

        int recordCount = await db.StageTeamMatches.CountAsync(stm => stm.StageId == stage.Id);
        Assert.Equal(2, recordCount);

        int maxTeams = StageHelper.GetMaxTeamsForStage(stage.StageType);
        Assert.Equal(2, maxTeams - recordCount);
    }

    /// <summary>
    /// ThirdPlace capacity is 2; 2 existing assignments fill the stage.
    /// </summary>
    [Fact]
    public async Task AssignTeamsToStageAsync_StageAlreadyAtCapacity_ThrowsForManualAndAuto()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Stage stage, Tournament tournament, _) = await SeedStageWithSlotsAsync(db, StageType.ThirdPlace, existingAssignmentCount: 2);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 1);
        List<Guid> teamIds = [.. poolTeams.Select(t => t.Id)];

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.AssignTeamsToStageAsync(stage, teamIds, auto: false));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.AssignTeamsToStageAsync(stage, null, auto: true));

        int recordCount = await db.StageTeamMatches.CountAsync(stm => stm.StageId == stage.Id);
        Assert.Equal(2, recordCount);
    }

    /// <summary>
    /// SemiFinal capacity is 4; 1 existing assignment leaves 3 available slots.
    /// </summary>
    [Fact]
    public async Task AssignTeamsToStageAsync_DuplicateAndAlreadyAssignedIds_AreFilteredOut()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Stage stage, Tournament tournament, List<Guid> existingIds) = await SeedStageWithSlotsAsync(db, StageType.SemiFinal, existingAssignmentCount: 1);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 2);

        List<Guid> requestIds = [poolTeams[0].Id, poolTeams[0].Id, poolTeams[1].Id, existingIds[0]];

        await stageService.AssignTeamsToStageAsync(stage, requestIds, auto: false);

        List<StageTeamMatch> records = await db.StageTeamMatches.Where(stm => stm.StageId == stage.Id).ToListAsync();
        Assert.Equal(3, records.Count);
        Assert.Contains(records, r => r.TeamId == poolTeams[0].Id);
        Assert.Contains(records, r => r.TeamId == poolTeams[1].Id);
        Assert.Single(records, r => r.TeamId == existingIds[0]);
    }

    /// <summary>
    /// SemiFinal capacity is 4; 1 existing assignment leaves 3 available slots.
    /// The eligible pool of 5 seeded teams is kept larger than the available
    /// slots so auto-assignment has more candidates than it can place.
    /// </summary>
    [Fact]
    public async Task AssignTeamsToStageAsync_AutoMode_AssignsUpToAvailableSlots()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Stage stage, Tournament tournament, List<Guid> existingIds) = await SeedStageWithSlotsAsync(db, StageType.SemiFinal, existingAssignmentCount: 1);
        await SeedTeamsAsync(db, tournament, 5);

        await stageService.AssignTeamsToStageAsync(stage, null, auto: true);

        List<StageTeamMatch> records = await db.StageTeamMatches.Where(stm => stm.StageId == stage.Id).ToListAsync();
        Assert.Equal(4, records.Count);

        List<Guid> newlyAssigned = [.. records.Select(r => r.TeamId).Except(existingIds)];
        Assert.Equal(3, newlyAssigned.Count);
        Assert.DoesNotContain(existingIds[0], newlyAssigned);
    }

    [Fact]
    public async Task AssignTeamsToStageAsync_AutoMode_OnlyAssignsTeamsFromStagesTournament()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournamentA = await SeedTournamentAsync(db);
        Division divisionA = await SeedDivisionAsync(db, tournamentA);

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.SemiFinal,
            IsActive = true,
            StartDate = tournamentA.StartDate,
            EndDate = tournamentA.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = divisionA.Id,
            Division = divisionA,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        List<Team> teamsA = await SeedTeamsAsync(db, tournamentA, 2);

        Tournament tournamentB = await SeedTournamentAsync(db);
        List<Team> teamsB = await SeedTeamsAsync(db, tournamentB, 5);

        await stageService.AssignTeamsToStageAsync(stage, null, auto: true);

        List<StageTeamMatch> records = await db.StageTeamMatches.Where(stm => stm.StageId == stage.Id).ToListAsync();

        Assert.Equal(2, records.Count);

        List<Guid> assignedTeamIds = [.. records.Select(r => r.TeamId)];
        List<Guid> teamAIds = [.. teamsA.Select(t => t.Id)];
        List<Guid> teamBIds = [.. teamsB.Select(t => t.Id)];

        Assert.All(assignedTeamIds, id => Assert.Contains(id, teamAIds));
        Assert.DoesNotContain(assignedTeamIds, teamBIds.Contains);
    }

    private static async Task<Tournament> SeedTournamentAsync(
        ApplicationDBContext db, TournamentStatus status = TournamentStatus.Scheduled)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Characterization test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Status = status,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }

    private static async Task<List<Team>> SeedTeamsAsync(ApplicationDBContext db, Tournament tournament, int count)
    {
        List<Team> teams = [];

        for (int i = 0; i < count; i++)
        {
            Team team = new()
            {
                Name = $"Team-{Guid.NewGuid()}",
                Slug = $"team-{Guid.NewGuid()}",
                ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
                LogoUrl = "http://example.com/logo.png",
                ShirtColor = "Red",
                TournamentId = tournament.Id,
                Players = [],
                CreatedBy = "test",
            };

            db.Teams.Add(team);
            teams.Add(team);
        }

        await db.SaveChangesAsync();

        return teams;
    }

    private static async Task<(Tournament tournament, List<Team> teams)> SeedTournamentWithTeamsAsync(ApplicationDBContext db, int teamCount)
    {
        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, teamCount);

        return (tournament, teams);
    }

    private static async Task<Division> SeedDivisionAsync(
        ApplicationDBContext db, Tournament tournament, bool withStages = false, bool isCrossDivisionCup = false)
    {
        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            IsCrossDivisionCup = isCrossDivisionCup,
            Stages = [],
            CreatedBy = "test",
        };

        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        if (withStages)
        {
            Stage existingStage = new()
            {
                Slug = $"stage-{Guid.NewGuid()}",
                Name = $"Existing-{Guid.NewGuid()}",
                StageType = StageType.Group,
                IsActive = true,
                StartDate = tournament.StartDate,
                EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
                DivisionId = division.Id,
                Division = division,
                Matches = [],
                CreatedBy = "test",
            };

            db.Stages.Add(existingStage);
            await db.SaveChangesAsync();
        }

        return division;
    }

    private static async Task<(Stage stage, Tournament tournament, List<Guid> existingTeamIds)> SeedStageWithSlotsAsync(
        ApplicationDBContext db, StageType stageType, int existingAssignmentCount)
    {
        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = stageType,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        List<Guid> existingTeamIds = [];

        if (existingAssignmentCount > 0)
        {
            List<Team> existingTeams = await SeedTeamsAsync(db, tournament, existingAssignmentCount);

            foreach (Team team in existingTeams)
            {
                db.StageTeamMatches.Add(new StageTeamMatch
                {
                    StageId = stage.Id,
                    TeamId = team.Id,
                    CreatedBy = "test",
                });
                existingTeamIds.Add(team.Id);
            }

            await db.SaveChangesAsync();
        }

        return (stage, tournament, existingTeamIds);
    }
}
