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
/// Covers StageService's stage creation, structural edit guards, and
/// team-assignment logic (IStageService.AssignTeamsToStageAsync). Also holds
/// characterization tests for behavior the roster and D2 changes deliberately
/// preserved or relaxed.
/// </summary>
public class StageServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StageServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// D2 relaxes the old one-Group-stage-per-division invariant so a regular
    /// division can hold multiple sub-group Group stages, as long as each has
    /// a distinct name — sub-groups need this to legally coexist.
    /// </summary>
    [Fact]
    public async Task CreateStageAsync_RegularDivision_AllowsSecondGroupStageWithDistinctName()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament, withStages: true);

        Stage secondGroupStage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Grupo B-{Guid.NewGuid()}",
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
    /// HU-125 scope fence: a position-range cup reads a single combined
    /// standings table, which has no defined meaning across independent
    /// sub-groups, so a second sub-group is rejected outright rather than
    /// letting the cup silently compute qualifiers from the wrong table.
    /// </summary>
    [Fact]
    public async Task CreateStageAsync_SecondGroupStage_RejectedWhenDivisionHasPositionRangeCup()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament, withStages: true);

        db.DivisionPlayoffMappings.Add(new DivisionPlayoffMapping
        {
            DivisionId = division.Id,
            FromPosition = 1,
            ToPosition = 2,
            Destination = "Copa de Oro",
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        Stage secondGroupStage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Grupo B-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CreateStageAsync(secondGroupStage));

        Assert.Equal(1, await db.Stages.CountAsync(s => s.DivisionId == division.Id && s.StageType == StageType.Group));
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
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 4, stage.Division);
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
        Guid divisionId;
        using (IServiceScope seedScope = _factory.Services.CreateScope())
        {
            ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            (Stage stage, Tournament tournament, _) = await SeedStageWithSlotsAsync(seedDb, StageType.SemiFinal, existingAssignmentCount: 0);
            stageId = stage.Id;
            tournamentId = tournament.Id;
            divisionId = stage.DivisionId;
        }

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament2 = await db.Tournaments.FirstAsync(t => t.Id == tournamentId);
        Division division2 = await db.Divisions.FirstAsync(d => d.Id == divisionId);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament2, 4, division2);
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
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 3, stage.Division);
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
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 2, stage.Division);
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
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 1, stage.Division);
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
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 2, stage.Division);

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
        await SeedTeamsAsync(db, tournament, 5, stage.Division);

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

        List<Team> teamsA = await SeedTeamsAsync(db, tournamentA, 2, divisionA);

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

    /// <summary>
    /// A team with no DivisionTeamRegistration for the stage's division can no longer
    /// be placed directly into a StageTeamMatch, manually or automatically — placement
    /// is a subset of division enrollment, never the reverse.
    /// </summary>
    [Fact]
    public async Task AssignTeamsToStageAsync_TeamWithNoDivisionRegistration_RejectsThatTeam_CreatesNoStageTeamMatch()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Stage stage, Tournament tournament, _) = await SeedStageWithSlotsAsync(db, StageType.SemiFinal, existingAssignmentCount: 0);
        List<Team> unregisteredTeams = await SeedTeamsAsync(db, tournament, 1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.AssignTeamsToStageAsync(stage, [unregisteredTeams[0].Id], auto: false));

        int recordCount = await db.StageTeamMatches.CountAsync(stm => stm.StageId == stage.Id);
        Assert.Equal(0, recordCount);
    }

    /// <summary>
    /// Auto-fill now pulls from the division roster, not every team registered to
    /// the stage's tournament: a team registered only to a different division of
    /// the same tournament must never be auto-assigned into this stage.
    /// </summary>
    [Fact]
    public async Task AssignTeamsToStageAsync_AutoMode_OnlyDrawsFromDivisionRoster_NotAllTournamentTeams()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division divisionA = await SeedDivisionAsync(db, tournament);
        Division divisionOther = await SeedDivisionAsync(db, tournament);

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.SemiFinal,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = divisionA.Id,
            Division = divisionA,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        List<Team> rosterTeams = await SeedTeamsAsync(db, tournament, 2, divisionA);
        List<Team> otherDivisionTeams = await SeedTeamsAsync(db, tournament, 2, divisionOther);

        await stageService.AssignTeamsToStageAsync(stage, null, auto: true);

        List<StageTeamMatch> records = await db.StageTeamMatches.Where(stm => stm.StageId == stage.Id).ToListAsync();

        Assert.Equal(2, records.Count);

        List<Guid> assignedTeamIds = [.. records.Select(r => r.TeamId)];
        List<Guid> rosterTeamIds = [.. rosterTeams.Select(t => t.Id)];
        List<Guid> otherDivisionTeamIds = [.. otherDivisionTeams.Select(t => t.Id)];

        Assert.All(assignedTeamIds, id => Assert.Contains(id, rosterTeamIds));
        Assert.DoesNotContain(assignedTeamIds, otherDivisionTeamIds.Contains);
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

    /// <summary>
    /// Seeds teams, optionally also registering each one to registerToDivision's
    /// roster so a subsequent AssignTeamsToStageAsync call finds them enrolled.
    /// </summary>
    private static async Task<List<Team>> SeedTeamsAsync(
        ApplicationDBContext db, Tournament tournament, int count, Division? registerToDivision = null)
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

        if (registerToDivision is not null)
        {
            foreach (Team team in teams)
            {
                db.DivisionTeamRegistrations.Add(new DivisionTeamRegistration
                {
                    TeamId = team.Id,
                    DivisionId = registerToDivision.Id,
                    CreatedBy = "test",
                });
            }

            await db.SaveChangesAsync();
        }

        return teams;
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
            List<Team> existingTeams = await SeedTeamsAsync(db, tournament, existingAssignmentCount, division);

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
