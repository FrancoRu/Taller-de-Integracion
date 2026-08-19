using Application.DTOs.DataMaintenance.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Linq;

namespace API.Tests;

/// <summary>
/// Verifies DataMaintenanceService.WipeSampleDataAsync and
/// SeedSampleDataAsync (Task 3 adds the seed-side facts to this same
/// class). CustomWebApplicationFactory shares one SQLite in-memory
/// database across every [Fact] in this class via IClassFixture, and
/// xUnit does not guarantee fact execution order — every fact below
/// establishes its own precondition explicitly (calling WipeSampleDataAsync
/// first) instead of assuming the database starts empty, so the suite
/// passes regardless of run order.
/// </summary>
public class DataMaintenanceServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DataMaintenanceServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WipeSampleDataAsync_RemovesAllTournamentDomainData()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        // Clear whatever other facts in this class may have left behind
        // before establishing this fact's own known fixture.
        await service.WipeSampleDataAsync();

        (int tournaments, int divisions, int teams, int players, int matches, int sanctions) =
            await BuildAndSaveSampleFixtureAsync(db);

        DataWipeResult result = await service.WipeSampleDataAsync();

        Assert.Equal(tournaments, result.Tournaments);
        Assert.Equal(divisions, result.Divisions);
        Assert.Equal(teams, result.Teams);
        Assert.Equal(players, result.Players);
        Assert.Equal(matches, result.Matches);
        Assert.Equal(1, result.MatchSeries);
        Assert.Equal(sanctions, result.PlayerSanctions);
        Assert.Equal(1, result.BlogPosts);
        Assert.Equal(1, result.Venues);

        Assert.Equal(0, await db.Tournaments.CountAsync());
        Assert.Equal(0, await db.Divisions.CountAsync());
        Assert.Equal(0, await db.Teams.CountAsync());
        Assert.Equal(0, await db.Players.CountAsync());
        Assert.Equal(0, await db.Matches.CountAsync());
        Assert.Equal(0, await db.MatchSeries.CountAsync());
        Assert.Equal(0, await db.PlayerSanctions.CountAsync());
        Assert.Equal(0, await db.PlayersStatistics.CountAsync());
        Assert.Equal(0, await db.Scorers.CountAsync());
        Assert.Equal(0, await db.Venues.CountAsync());
        Assert.Equal(0, await db.BlogPosts.CountAsync());
        Assert.Equal(0, await db.StageTeamMatches.CountAsync());
        Assert.Equal(0, await db.PlayerTeamRegistrations.CountAsync());
        Assert.Equal(0, await db.Stages.CountAsync());
    }

    [Fact]
    public async Task WipeSampleDataAsync_LeavesIdentityUntouched()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        Infrastructure.Identity.IdentityAppDbContext identityDb =
            scope.ServiceProvider.GetRequiredService<Infrastructure.Identity.IdentityAppDbContext>();

        int usersBefore = await identityDb.Users.CountAsync();

        await service.WipeSampleDataAsync();

        int usersAfter = await identityDb.Users.CountAsync();

        Assert.Equal(usersBefore, usersAfter);
    }

    /// <summary>
    /// Builds one small tournament directly via SampleTournamentBuilder
    /// (Task 1) plus one BlogPost and one MatchSeries row that the builder
    /// itself doesn't create, so this task's wipe tests don't need
    /// DataMaintenanceService.SeedSampleDataAsync — which doesn't exist
    /// yet (Task 3) — to have something real to delete.
    /// </summary>
    private static async Task<(int Tournaments, int Divisions, int Teams, int Players, int Matches, int PlayerSanctions)>
        BuildAndSaveSampleFixtureAsync(ApplicationDBContext db)
    {
        List<Venue> venues =
        [
            new() { CreatedBy = "test", Name = "Cancha de prueba", Address = "Calle Falsa 123" },
        ];

        SampleTournamentBuilder.TournamentDefinition definition = new(
            Name: "Torneo de prueba",
            Description: "Fixture de prueba para WipeSampleDataAsync.",
            TeamRegistrationDeadline: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc),
            MinTeams: 4,
            MaxTeams: 4,
            Divisions:
            [
                new(
                    "Primera",
                    ["Equipo A", "Equipo B", "Equipo C", "Equipo D"],
                    ["EQA", "EQB", "EQC", "EQD"],
                    ["#111111", "#222222", "#333333", "#444444"]),
            ]);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result = SampleTournamentBuilder.Build(definition, venues, ref playerCounter);

        db.Tournaments.Add(result.Tournament);
        db.PlayerSanctions.AddRange(result.Sanctions);

        Team homeTeam = result.Tournament.Teams.ElementAt(0);
        Team visitorTeam = result.Tournament.Teams.ElementAt(1);
        Stage stage = result.Tournament.Divisions.ElementAt(0).Stages.ElementAt(0);

        db.MatchSeries.Add(new MatchSeries
        {
            CreatedBy = "test",
            StageId = Guid.Empty,
            Stage = stage,
            HomeTeamId = Guid.Empty,
            HomeTeam = homeTeam,
            VisitorTeamId = Guid.Empty,
            VisitorTeam = visitorTeam,
            BestOf = 3,
        });

        db.BlogPosts.Add(new BlogPost
        {
            CreatedBy = "test",
            Author = "Test",
            Title = "Post de prueba",
            Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug($"Post de prueba {Guid.NewGuid()}"),
            MarkdownText = "Contenido de prueba.",
            Views = 0,
        });

        await db.SaveChangesAsync();

        int teamCount = result.Tournament.Teams.Count;
        int playerCount = result.Tournament.Teams.Sum(t => t.Players.Count);
        int matchCount = result.Tournament.Divisions.Sum(d => d.Stages.Sum(s => s.Matches.Count));

        return (1, 1, teamCount, playerCount, matchCount, result.Sanctions.Count);
    }

    [Fact]
    public async Task SeedSampleDataAsync_OnEmptyDatabase_Creates2DistinctTournaments()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        await service.WipeSampleDataAsync();

        DataSeedResult result = await service.SeedSampleDataAsync();

        Assert.Equal(2, result.Tournaments);
        Assert.Equal(4, result.Divisions);
        Assert.Equal(16, result.Teams);
        Assert.Equal(128, result.Players);
        Assert.Equal(2, result.BlogPosts);
        Assert.True(result.Matches > 0);
        Assert.True(result.PlayerSanctions > 0);

        List<Tournament> tournaments = await db.Tournaments.ToListAsync();
        Assert.Equal(2, tournaments.Count);
        Assert.NotEqual(tournaments[0].Name, tournaments[1].Name);
        Assert.NotEqual(tournaments[0].Slug, tournaments[1].Slug);

        Assert.True(await db.Scorers.CountAsync() > 0);
        Assert.True(await db.PlayersStatistics.CountAsync() > 0);
        Assert.Equal(16, await db.StageTeamMatches.CountAsync());
        Assert.True(await db.Matches.AnyAsync(m => m.IsFinished && m.HomeScore != null));
    }

    [Fact]
    public async Task SeedSampleDataAsync_OnNonEmptyDatabase_ThrowsInvalidOperationException()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SeedSampleDataAsync());
        Assert.Equal(2, await db.Tournaments.CountAsync());
    }

    [Fact]
    public async Task WipeSampleDataAsync_ThenSeedSampleDataAsync_SucceedsAgain()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();
        await service.WipeSampleDataAsync();

        DataSeedResult result = await service.SeedSampleDataAsync();

        Assert.Equal(2, result.Tournaments);
    }
}
