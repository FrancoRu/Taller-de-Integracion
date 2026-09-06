using Application.Interfaces.Services;
using Application.Utils.Constants.Stage;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Covers HU-121/122/123: balanced sub-group distribution, manual
/// reassignment bounded only by the minimum sub-group size, and rebuilding
/// the sub-group stage layer after a count change without ever touching the
/// underlying DivisionTeamRegistration roster.
/// </summary>
public class SubGroupRebuildTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SubGroupRebuildTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RebuildSubGroupsAsync_RosterUnchanged_AcrossCountChange()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedRosterAsync(db, tournament, division, 16);

        await stageService.RebuildSubGroupsAsync(division.Id, 3);
        List<Guid> registrationIdsBefore = [.. (await db.DivisionTeamRegistrations
            .Where(r => r.DivisionId == division.Id)
            .Select(r => r.Id)
            .ToListAsync())
            .OrderBy(id => id)];

        await stageService.RebuildSubGroupsAsync(division.Id, 4);
        List<Guid> registrationIdsAfter = [.. (await db.DivisionTeamRegistrations
            .Where(r => r.DivisionId == division.Id)
            .Select(r => r.Id)
            .ToListAsync())
            .OrderBy(id => id)];

        Assert.Equal(16, registrationIdsAfter.Count);
        Assert.Equal(registrationIdsBefore, registrationIdsAfter);

        List<Guid> placedTeamIds = [.. await db.StageTeamMatches
            .Where(stm => stm.Stage!.DivisionId == division.Id)
            .Select(stm => stm.TeamId)
            .Distinct()
            .ToListAsync()];
        Assert.Equal([.. teams.Select(t => t.Id).OrderBy(id => id)], [.. placedTeamIds.OrderBy(id => id)]);
    }

    [Fact]
    public async Task RebuildSubGroupsAsync_OldStageStructureFullyReplaced_NotMerged()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        await SeedRosterAsync(db, tournament, division, 16);

        List<Stage> firstBuild = await stageService.RebuildSubGroupsAsync(division.Id, 3);
        List<Guid> firstStageIds = [.. firstBuild.Select(s => s.Id)];

        List<Stage> secondBuild = await stageService.RebuildSubGroupsAsync(division.Id, 2);

        Assert.Equal(2, secondBuild.Count);
        Assert.Equal(2, await db.Stages.CountAsync(s => s.DivisionId == division.Id && s.StageType == StageType.Group));
        Assert.False(await db.Stages.AnyAsync(s => firstStageIds.Contains(s.Id)));
    }

    [Fact]
    public async Task RebuildSubGroupsAsync_TooFewTeamsPerGroup_RejectedNoChange()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        await SeedRosterAsync(db, tournament, division, 10);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.RebuildSubGroupsAsync(division.Id, 3));

        Assert.Equal(0, await db.Stages.CountAsync(s => s.DivisionId == division.Id));
        Assert.Equal(10, await db.DivisionTeamRegistrations.CountAsync(r => r.DivisionId == division.Id));
    }

    [Fact]
    public async Task RebuildSubGroupsAsync_TournamentOngoing_Rejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Division division = await SeedDivisionAsync(db, tournament);
        await SeedRosterAsync(db, tournament, division, 12);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.RebuildSubGroupsAsync(division.Id, 3));
    }

    [Fact]
    public async Task RebuildSubGroupsAsync_EmptyRoster_SkipsMinCheck_CreatesEmptyGroups()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);

        List<Stage> stages = await stageService.RebuildSubGroupsAsync(division.Id, 3);

        Assert.Equal(3, stages.Count);
        Assert.Equal(0, await db.StageTeamMatches.CountAsync(stm => stm.Stage!.DivisionId == division.Id));
    }

    [Fact]
    public async Task RebuildSubGroupsAsync_MultipleSubGroups_RejectedWhenDivisionHasPositionRangeCup()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        await SeedRosterAsync(db, tournament, division, 16);

        db.DivisionPlayoffMappings.Add(new DivisionPlayoffMapping
        {
            DivisionId = division.Id,
            FromPosition = 1,
            ToPosition = 2,
            Destination = "Copa de Oro",
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.RebuildSubGroupsAsync(division.Id, 2));

        Assert.Equal(0, await db.Stages.CountAsync(s => s.DivisionId == division.Id));
    }

    [Fact]
    public async Task AutoDistributeRosterAsync_ClearsThenRedistributes_AlwaysBalanced()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedRosterAsync(db, tournament, division, 16);

        List<Stage> stages = await stageService.RebuildSubGroupsAsync(division.Id, 4);

        // Manually collapse every team into the first group, an imbalance auto-distribute must undo.
        List<StageTeamMatch> current = await db.StageTeamMatches
            .Where(stm => stm.Stage!.DivisionId == division.Id)
            .ToListAsync();
        foreach (StageTeamMatch stm in current)
        {
            stm.StageId = stages[0].Id;
        }
        await db.SaveChangesAsync();

        await stageService.AutoDistributeRosterAsync(division.Id);

        List<int> sizes = [];
        foreach (Stage stage in stages)
        {
            sizes.Add(await db.StageTeamMatches.CountAsync(stm => stm.StageId == stage.Id));
        }

        Assert.Equal(16, sizes.Sum());
        Assert.All(sizes, size => Assert.Equal(4, size));

        List<Guid> placedTeamIds = [.. await db.StageTeamMatches
            .Where(stm => stm.Stage!.DivisionId == division.Id)
            .Select(stm => stm.TeamId)
            .Distinct()
            .ToListAsync()];
        Assert.Equal([.. teams.Select(t => t.Id).OrderBy(id => id)], [.. placedTeamIds.OrderBy(id => id)]);
    }

    [Fact]
    public async Task ReassignTeamToSubGroupAsync_MovesTeam_OtherPlacementsUnchanged()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedRosterAsync(db, tournament, division, 15);

        // 3 groups of 5 each, so moving one team away leaves the source at exactly the minimum of 4.
        List<Stage> stages = await stageService.RebuildSubGroupsAsync(division.Id, 3);

        StageTeamMatch moved = await db.StageTeamMatches.FirstAsync(stm => stm.Stage!.DivisionId == division.Id);
        Guid fromStageId = moved.StageId;
        Guid toStageId = stages.First(s => s.Id != fromStageId).Id;
        Guid teamId = moved.TeamId;

        List<Guid> otherTeamIdsBefore = [.. await db.StageTeamMatches
            .Where(stm => stm.Stage!.DivisionId == division.Id && stm.TeamId != teamId)
            .Select(stm => new { stm.TeamId, stm.StageId })
            .Select(x => x.TeamId)
            .ToListAsync()];

        await stageService.ReassignTeamToSubGroupAsync(teamId, fromStageId, toStageId);

        StageTeamMatch? afterMove = await db.StageTeamMatches
            .FirstOrDefaultAsync(stm => stm.TeamId == teamId && stm.Stage!.DivisionId == division.Id);

        Assert.NotNull(afterMove);
        Assert.Equal(toStageId, afterMove!.StageId);

        List<Guid> otherTeamIdsAfter = [.. await db.StageTeamMatches
            .Where(stm => stm.Stage!.DivisionId == division.Id && stm.TeamId != teamId)
            .Select(stm => stm.TeamId)
            .ToListAsync()];
        Assert.Equal([.. otherTeamIdsBefore.OrderBy(id => id)], [.. otherTeamIdsAfter.OrderBy(id => id)]);
        Assert.Equal(15, teams.Count);
    }

    [Fact]
    public async Task ReassignTeamToSubGroupAsync_WouldDropSourceBelowMinimum_Rejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        await SeedRosterAsync(db, tournament, division, 8);

        // 2 groups of 4 each — the minimum — so any move away from either group drops it below 4.
        List<Stage> stages = await stageService.RebuildSubGroupsAsync(division.Id, 2);

        StageTeamMatch moved = await db.StageTeamMatches.FirstAsync(stm => stm.StageId == stages[0].Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.ReassignTeamToSubGroupAsync(moved.TeamId, stages[0].Id, stages[1].Id));

        Assert.Equal(4, await db.StageTeamMatches.CountAsync(stm => stm.StageId == stages[0].Id));
    }

    [Fact]
    public async Task ReassignTeamToSubGroupAsync_ArbitraryMoveAboveMinimum_Allowed()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        await SeedRosterAsync(db, tournament, division, 12);

        // 2 groups of 6 each, well above the minimum-4 floor.
        List<Stage> stages = await stageService.RebuildSubGroupsAsync(division.Id, 2);

        StageTeamMatch moved = await db.StageTeamMatches.FirstAsync(stm => stm.StageId == stages[0].Id);

        // Nothing but geography/rivalry motivates this move; the organizer needs it to succeed
        // even though it makes the destination group larger than the source, since the only
        // hard constraint is the minimum-4 floor on the source, not staying balanced.
        await stageService.ReassignTeamToSubGroupAsync(moved.TeamId, stages[0].Id, stages[1].Id);

        Assert.Equal(5, await db.StageTeamMatches.CountAsync(stm => stm.StageId == stages[0].Id));
        Assert.Equal(7, await db.StageTeamMatches.CountAsync(stm => stm.StageId == stages[1].Id));
    }

    // ---- seeding helpers ----

    private static async Task<Tournament> SeedTournamentAsync(
        ApplicationDBContext db, TournamentStatus status = TournamentStatus.Scheduled)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Sub-group rebuild test tournament",
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

    private static async Task<Division> SeedDivisionAsync(ApplicationDBContext db, Tournament tournament)
    {
        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        };

        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        return division;
    }

    private static async Task<List<Team>> SeedRosterAsync(
        ApplicationDBContext db, Tournament tournament, Division division, int count)
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

        foreach (Team team in teams)
        {
            db.DivisionTeamRegistrations.Add(new DivisionTeamRegistration
            {
                TeamId = team.Id,
                DivisionId = division.Id,
                CreatedBy = "test",
            });
        }

        await db.SaveChangesAsync();

        return teams;
    }
}
