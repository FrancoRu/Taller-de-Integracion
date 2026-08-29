using Application.DTOs.Champions.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// End-to-end proof that the seed builder's tournaments feed the Campeones page
/// correctly: a tournament built the way the startup DataSeeder builds the
/// Apertura (playoffs + cross cup, <see cref="TournamentStatus.Finished"/>)
/// resolves real champions through <see cref="IChampionService"/>, while a
/// Clausura-style ONGOING tournament never appears in the champions history and
/// keeps its upcoming (unplayed) fixture. Persisted to the shared SQLite harness
/// and queried through the real service, so it exercises the exact path
/// <c>GET api/champions</c> uses.
/// </summary>
public class SeedChampionsResolutionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SeedChampionsResolutionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static List<Venue> BuildVenues() =>
    [
        new() { Slug = $"venue-{Guid.NewGuid()}", CreatedBy = "test", Name = "Cancha Uno", Address = "Calle 1" },
        new() { Slug = $"venue-{Guid.NewGuid()}", CreatedBy = "test", Name = "Cancha Dos", Address = "Calle 2" },
    ];

    private static string[] UniqueNames(int count) =>
        [.. Enumerable.Range(0, count).Select(_ => $"Club {Guid.NewGuid()}")];

    private static string[] Codes(int count) =>
        [.. Enumerable.Range(0, count).Select(i => $"C{i:00}")];

    private static string[] Colors(int count) =>
        [.. Enumerable.Range(0, count).Select(_ => "#1E3A8A")];

    [Fact]
    public async Task FinishedTournament_LikeSeededApertura_ResolvesChampionsInHistory()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IChampionService championService = scope.ServiceProvider.GetRequiredService<IChampionService>();

        // Division/cup names are Guid-suffixed so the shared SQLite harness (one
        // DB per test class, reused across facts) never collides on the global
        // Division.Slug unique index.
        string zoneName = $"Primera División {Guid.NewGuid()}";
        string cupName = $"Copa Club 12 {Guid.NewGuid()}";
        SampleTournamentBuilder.TournamentDefinition definition = new(
            Name: $"Apertura Finished {Guid.NewGuid()}",
            Description: "Torneo finalizado con playoffs y copa.",
            TeamRegistrationDeadline: new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new(zoneName, UniqueNames(8), Codes(8), Colors(8)),
            ],
            CrossCup: new(cupName, GroupCount: 2, QualifiersPerGroup: 1),
            Status: TournamentStatus.Finished,
            Category: TournamentCategory.Masculine);

        // Distinct counter base so player document numbers (and thus slugs) never
        // collide with the other fact's players in the shared class DB.
        int playerCounter = 100_000;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, BuildVenues(), ref playerCounter, includePlayoffs: true);

        db.Tournaments.Add(result.Tournament);
        db.PlayerSanctions.AddRange(result.Sanctions);
        await db.SaveChangesAsync();

        List<ChampionHistoryResponse> history = await championService.GetChampionsHistoryAsync(seasonId: null);
        List<ChampionHistoryResponse> entries = [.. history.Where(h => h.TournamentId == result.Tournament.Id)];

        // The finished tournament resolves at least one champion, and the zone
        // division ("Primera División") is crowned via its playoff Final.
        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.NotNull(e.ChampionTeam));
        Assert.Contains(entries, e => e.DivisionName == zoneName);
        Assert.All(entries, e => Assert.Equal(TournamentCategory.Masculine.ToString(), e.Category));
    }

    [Fact]
    public async Task OngoingTournament_LikeSeededClausura_HasNoChampionsButKeepsUpcomingFixture()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IChampionService championService = scope.ServiceProvider.GetRequiredService<IChampionService>();

        SampleTournamentBuilder.TournamentDefinition definition = new(
            Name: $"Clausura Ongoing {Guid.NewGuid()}",
            Description: "Torneo en curso a dos ruedas.",
            TeamRegistrationDeadline: new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new($"Primera División {Guid.NewGuid()}", UniqueNames(8), Codes(8), Colors(8)),
            ],
            Status: TournamentStatus.Ongoing,
            Category: TournamentCategory.Masculine,
            RoundRobinLegs: 2,
            PlayedRoundsPerZone: 8);

        // Distinct counter base so player document numbers (and thus slugs) never
        // collide with the other fact's players in the shared class DB.
        int playerCounter = 500_000;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, BuildVenues(), ref playerCounter, includePlayoffs: false);

        db.Tournaments.Add(result.Tournament);
        db.PlayerSanctions.AddRange(result.Sanctions);
        await db.SaveChangesAsync();

        List<ChampionHistoryResponse> history = await championService.GetChampionsHistoryAsync(seasonId: null);
        Assert.DoesNotContain(history, h => h.TournamentId == result.Tournament.Id);

        // The in-progress tournament persists both finished (played) and upcoming
        // (unplayed) matches so its standings and "Próximos" fixture have data.
        List<Guid> stageIds = [.. result.Tournament.Divisions.SelectMany(d => d.Stages).Select(s => s.Id)];
        List<Match> matches = await db.Matches.Where(m => stageIds.Contains(m.StageId)).ToListAsync();
        Assert.Contains(matches, m => m.IsFinished);
        Assert.Contains(matches, m => !m.IsFinished);
    }
}
