# Admin Test Data Tools Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give Admin users a "Test" panel tab with two buttons — "Borrar DB" (wipes tournament-domain data, leaves Identity untouched) and "Cargar Datos de prueba" (seeds 2 complete, realistic tournaments) — so the Supabase dev database can be reset to a demo-ready state on demand.

**Architecture:** Extract the entity-construction logic already in `DataSeeder` into a reusable `SampleTournamentBuilder`, so both the existing 1-tournament startup seeder and a new on-demand 2-tournament admin seeder share it without duplication. A new `IDataMaintenanceService`/`DataMaintenanceService` (Infrastructure layer, same pattern as `DataSeeder`, direct `ApplicationDBContext` access) exposes wipe and seed, behind a new Admin-only `DataMaintenanceController`. The frontend adds one nav tab (already Admin-gated by its position in `TABS_BY_ROLE`) and one view reusing the existing `confirmDialog`/`sendPost` utilities — no new UI infrastructure.

**Tech Stack:** ASP.NET Core 8 / EF Core 8 (Npgsql in prod, SQLite in-memory in tests via `CustomWebApplicationFactory`), xUnit v3, React + TypeScript + MUI + axios.

**Spec:** `docs/superpowers/specs/2026-08-18-admin-test-data-tools-design.md`

## Global Constraints

- Wipe touches **only** tournament-domain tables — Identity (users, roles) is never touched. (spec: Purpose, Guardrails)
- Wipe runs inside a single DB transaction — no partially-wiped state on failure. (spec: Architecture)
- Seed refuses to run against a non-empty tournament-domain database (`409 Conflict`) instead of duplicating data. (spec: Architecture)
- Both endpoints are `[Authorize(Roles = Roles.Admin)]` — exact existing constant, no new role/policy. (spec: Architecture, Guardrails)
- The existing startup `DataSeeder` behavior (1 tournament, `Seed:Enabled`-gated, skips if any team exists) must not change. (spec: Non-goals)
- Every new/changed backend behavior gets an integration test via `CustomWebApplicationFactory`, written before the implementation (TDD). (spec: Testing)

## Planning correction (found during file-level investigation, not in the written spec)

The spec's wipe order didn't account for `MatchSeries` (`Domain/Entities/Models/MatchSeries.cs`) — a best-of-N playoff series table with `Match.SeriesId` pointing to it and `MatchSeries.StageId`/`HomeTeamId`/`VisitorTeamId`/`WinningTeamId` pointing out from it. It must be deleted after `Match` (which references it) and before `Stage`/`Team` (which it references). The corrected, verified order used throughout this plan is:

```
Scorer → PlayerStatistic → PlayerSanction → StageTeamMatch → PlayerTeamRegistration
→ Match → MatchSeries → Player → Stage → Team → Division → Tournament → Venue → BlogPost
```

Also: the current `DataSeeder.SeedAsync()` never explicitly calls `db.Venues.AddRange(...)` — venues are picked up automatically by EF Core's change tracker because they're reachable from the tracked `Tournament` graph via `Match.Venue`. Task 2 preserves this (no explicit `AddRange` for venues).

The spec named the new view `Club12-WebClient/src/views/test/testDataPage.tsx`. Investigation found the actual convention every other Admin panel page follows is `Club12-WebClient/src/views/panel/<PageName>.tsx` in PascalCase (e.g. `StatisticsPage.tsx`, imported and registered in `App.tsx`'s `ADMIN_ROUTES`) — Task 7 uses `views/panel/TestDataPage.tsx` to match that, not the spec's literal path.

---

### Task 1: Extract `SampleTournamentBuilder` from `DataSeeder`

**Files:**
- Create: `Club12-Backend/Infrastructure/Persistance/SampleTournamentBuilder.cs`
- Modify: `Club12-Backend/Infrastructure/Persistance/DataSeeder.cs`
- Test: existing `Club12-Backend/API.Tests/*` (no new test file — this is a pure refactor; the check is that the full existing suite still passes with identical seeded output)

**Interfaces:**
- Produces: `SampleTournamentBuilder.TournamentDefinition` (record), `SampleTournamentBuilder.DivisionDefinition` (record), `SampleTournamentBuilder.BuildResult` (record: `Tournament Tournament, List<PlayerSanction> Sanctions`), `SampleTournamentBuilder.Build(TournamentDefinition definition, List<Venue> venues, ref int playerCounter) -> BuildResult` (static method) — Task 2 consumes all of these.

- [ ] **Step 1: Create `SampleTournamentBuilder.cs` with the extracted, parameterized logic**

```csharp
using Application.Utils.Helper.Slug;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Persistance;

/// <summary>
/// Builds one fully-populated sample Tournament (divisions, teams, players,
/// a group stage, round-robin matches with scores/scorers/statistics, and
/// sanctions) from a declarative definition. Shared by the startup
/// DataSeeder (one call, fixed definition) and DataMaintenanceService (two
/// calls, two distinct definitions) so the construction logic exists once.
/// </summary>
public static class SampleTournamentBuilder
{
    private const string CreatedBy = AuditConstants.SystemUser;

    private static readonly string[] FirstNames =
    [
        "Juan", "Carlos", "Martín", "Diego", "Facundo", "Lucas", "Nicolás", "Matías",
        "Franco", "Ezequiel", "Agustín", "Bruno", "Iván", "Santiago", "Tomás", "Gonzalo",
    ];

    private static readonly string[] LastNames =
    [
        "González", "Rodríguez", "Fernández", "López", "Díaz", "Pérez", "Sánchez", "Romero",
        "Álvarez", "Torres", "Ruiz", "Ramírez", "Flores", "Acosta", "Benítez", "Medina",
    ];

    public sealed record DivisionDefinition(
        string DivisionName,
        string[] TeamNames,
        string[] TeamCodes,
        string[] TeamColors);

    public sealed record TournamentDefinition(
        string Name,
        string Description,
        DateTime TeamRegistrationDeadline,
        DateTime StartDate,
        DateTime StageStartDate,
        DateTime StageEndDate,
        DateTime FinishedMatchesStart,
        DateTime UpcomingMatchesStart,
        int MinTeams,
        int MaxTeams,
        DivisionDefinition[] Divisions);

    public sealed record BuildResult(Tournament Tournament, List<PlayerSanction> Sanctions);

    /// <summary>
    /// Builds one Tournament with every division in <paramref name="definition"/>.
    /// <paramref name="playerCounter"/> is threaded through (and must keep
    /// incrementing) across multiple calls so player names/document numbers
    /// never collide between tournaments built in the same seeding run.
    /// </summary>
    public static BuildResult Build(TournamentDefinition definition, List<Venue> venues, ref int playerCounter)
    {
        Tournament tournament = new()
        {
            CreatedBy = CreatedBy,
            Name = definition.Name,
            Slug = SlugGenerator.GenerateSlug(definition.Name),
            Description = definition.Description,
            TeamRegistrationDeadline = definition.TeamRegistrationDeadline,
            StartDate = definition.StartDate,
            MinTeams = definition.MinTeams,
            MaxTeams = definition.MaxTeams,
            Status = TournamentStatus.Ongoing,
            Divisions = [],
            Teams = [],
        };

        List<PlayerSanction> sanctions = [];

        foreach (DivisionDefinition divisionDef in definition.Divisions)
        {
            (Division division, List<Team> teams) = BuildDivisionWithTeams(
                tournament,
                divisionDef.DivisionName,
                divisionDef.TeamNames,
                divisionDef.TeamCodes,
                divisionDef.TeamColors,
                ref playerCounter);

            tournament.Divisions.Add(division);
            foreach (Team team in teams)
            {
                tournament.Teams.Add(team);
            }

            Stage stage = new()
            {
                CreatedBy = CreatedBy,
                Name = "Fase de Grupos",
                StageType = StageType.Group,
                IsActive = true,
                StartDate = definition.StageStartDate,
                EndDate = definition.StageEndDate,
                DivisionId = Guid.Empty,
                Division = division,
                Matches = [],
                Order = 0,
            };
            division.Stages.Add(stage);

            foreach (Team team in teams)
            {
                stage.StageTeamMatches.Add(new StageTeamMatch
                {
                    CreatedBy = CreatedBy,
                    StageId = Guid.Empty,
                    Stage = stage,
                    TeamId = Guid.Empty,
                    Team = team,
                });
            }

            sanctions.AddRange(SeedRoundRobinMatches(
                stage, teams, venues, definition.FinishedMatchesStart, definition.UpcomingMatchesStart));
        }

        return new BuildResult(tournament, sanctions);
    }

    private static (Division Division, List<Team> Teams) BuildDivisionWithTeams(
        Tournament tournament,
        string divisionName,
        string[] teamNames,
        string[] teamCodes,
        string[] teamColors,
        ref int playerCounter)
    {
        Division division = new()
        {
            CreatedBy = CreatedBy,
            Name = divisionName,
            Tournament = tournament,
            Stages = [],
        };

        List<Team> teams = [];

        for (int i = 0; i < teamNames.Length; i++)
        {
            Team team = new()
            {
                CreatedBy = CreatedBy,
                Name = teamNames[i],
                Slug = SlugGenerator.GenerateSlug(teamNames[i]),
                ThreeLetterCode = teamCodes[i],
                LogoUrl = $"https://placehold.co/128x128?text={teamCodes[i]}",
                ShirtColor = teamColors[i],
                Tournament = tournament,
                Players = [],
            };

            for (int p = 0; p < 8; p++)
            {
                playerCounter++;

                string firstName = FirstNames[playerCounter % FirstNames.Length];
                string lastName = LastNames[(playerCounter * 3) % LastNames.Length];
                string documentNumber = (30000000 + playerCounter).ToString();

                Player player = new()
                {
                    CreatedBy = CreatedBy,
                    FirstName = firstName,
                    LastName = lastName,
                    DocumentNumber = documentNumber,
                    IsSanctioned = false,
                    BirthDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc)
                        .AddYears(-(18 + (playerCounter % 20)))
                        .AddDays(playerCounter % 27),
                    SocialSecurity = $"20-{documentNumber}-3",
                    Team = team,
                };

                team.Players.Add(player);

                team.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
                {
                    CreatedBy = CreatedBy,
                    PlayerId = Guid.Empty,
                    Player = player,
                    TeamId = Guid.Empty,
                    Team = team,
                    TournamentId = Guid.Empty,
                    Tournament = tournament,
                });
            }

            teams.Add(team);
        }

        return (division, teams);
    }

    private static List<PlayerSanction> SeedRoundRobinMatches(
        Stage stage, List<Team> teams, List<Venue> venues, DateTime finishedMatchesStart, DateTime upcomingMatchesStart)
    {
        List<PlayerSanction> sanctions = [];
        List<(Team Home, Team Visitor)> pairings = [];

        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                pairings.Add((teams[i], teams[j]));
            }
        }

        int finishedCount = pairings.Count / 2;

        for (int i = 0; i < pairings.Count; i++)
        {
            (Team home, Team visitor) = pairings[i];
            bool isFinished = i < finishedCount;
            Venue venue = venues[i % venues.Count];

            Match match = new()
            {
                CreatedBy = CreatedBy,
                MatchDate = isFinished
                    ? finishedMatchesStart.AddDays(i * 7)
                    : upcomingMatchesStart.AddDays((i - finishedCount) * 7),
                Type = MatchType.Regular,
                Slug = SlugGenerator.GenerateSlug($"{home.Name}-vs-{visitor.Name}-{i}-{stage.Division!.Tournament!.Slug}"),
                HomeTeam = home,
                VisitorTeam = visitor,
                IsFinished = isFinished,
                Stage = stage,
                Venue = venue,
                PlayerStatistics = [],
                Scorers = [],
            };

            if (isFinished)
            {
                int homeScore = 1 + ((i * 2) % 4);
                int visitorScore = (i % 3);
                match.HomeScore = homeScore;
                match.VisitorScore = visitorScore;
                if (homeScore != visitorScore)
                {
                    match.WinningTeam = homeScore > visitorScore ? home : visitor;
                }

                Player homeScorer = home.Players.ElementAt(i % home.Players.Count);
                match.Scorers.Add(new Scorer
                {
                    CreatedBy = CreatedBy,
                    PlayerId = Guid.Empty,
                    Player = homeScorer,
                    Points = homeScore,
                    MatchId = Guid.Empty,
                    Match = match,
                });
                match.PlayerStatistics.Add(new PlayerStatistic
                {
                    CreatedBy = CreatedBy,
                    Value = 1,
                    PlayerId = Guid.Empty,
                    Player = homeScorer,
                    MatchId = Guid.Empty,
                    Match = match,
                    Type = StatisticType.Assists,
                });

                if (visitorScore > 0)
                {
                    Player visitorScorer = visitor.Players.ElementAt(i % visitor.Players.Count);
                    match.Scorers.Add(new Scorer
                    {
                        CreatedBy = CreatedBy,
                        PlayerId = Guid.Empty,
                        Player = visitorScorer,
                        Points = visitorScore,
                        MatchId = Guid.Empty,
                        Match = match,
                    });
                }

                if (i == 0 || i == finishedCount - 1)
                {
                    Team losingTeam;
                    if (match.WinningTeam is null)
                    {
                        losingTeam = visitor;
                    }
                    else
                    {
                        losingTeam = match.WinningTeam == home ? visitor : home;
                    }

                    Player sanctionedPlayer = losingTeam.Players.ElementAt((i + 1) % losingTeam.Players.Count);
                    sanctionedPlayer.IsSanctioned = true;

                    sanctions.Add(new PlayerSanction
                    {
                        CreatedBy = CreatedBy,
                        Duration = 2,
                        IssuedDate = match.MatchDate,
                        Description = "Expulsión por doble amonestación.",
                        Player = sanctionedPlayer,
                        PlayerId = Guid.Empty,
                        Match = match,
                        MatchId = Guid.Empty,
                        Slug = SlugGenerator.GenerateSlug(
                            $"{sanctionedPlayer.FirstName}-{sanctionedPlayer.LastName}-{match.Slug}"),
                        AppealStatus = i == 0 ? SanctionAppealStatus.Pending : SanctionAppealStatus.None,
                    });
                }
            }

            stage.Matches.Add(match);
        }

        return sanctions;
    }
}
```

Note the one intentional behavior tweak versus the original private method: the match slug now includes the tournament's slug (`{stage.Division!.Tournament!.Slug}`) so two tournaments seeded in the same run never produce colliding match slugs (e.g. two "Atlético Central-vs-Deportivo Norte-0" matches would otherwise both hash to the same slug). This only changes generated slugs, not any test assertion — no existing test asserts an exact match slug string (verified: `PlayerSanctionSlugTests.cs` and `PlayerSanctionServiceTests.cs` assert sanction slugs, which already include the match slug as a suffix and are unaffected by what the match slug itself contains).

- [ ] **Step 2: Rewrite `DataSeeder.cs` to delegate to `SampleTournamentBuilder`**

Replace the full file with:

```csharp
using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistance;

/// <summary>
/// Seeds sample tournament data (venues, a tournament, divisions, teams,
/// players, a group stage with matches, sanctions and blog posts) so the
/// application has something to look at in a fresh database. Controlled by
/// configuration (Seed:Enabled) and runs only once — skips silently if any
/// team already exists.
/// </summary>
public sealed class DataSeeder(ApplicationDBContext db, ILogger<DataSeeder> logger)
{
    private static readonly string[] PrimeraTeamNames =
        ["Atlético Central", "Deportivo Norte", "Club Belgrano", "Unión del Sur"];

    private static readonly string[] PrimeraTeamCodes = ["ATC", "DNO", "CBE", "UDS"];

    private static readonly string[] PrimeraTeamColors =
        ["#1E3A8A", "#DC2626", "#16A34A", "#EA580C"];

    private static readonly string[] ReservaTeamNames =
        ["Juventud Unida", "Sportivo Oeste", "Estrella Azul", "Náutico River"];

    private static readonly string[] ReservaTeamCodes = ["JUN", "SPO", "EAZ", "NRV"];

    private static readonly string[] ReservaTeamColors =
        ["#7C3AED", "#0891B2", "#CA8A04", "#4338CA"];

    public async Task SeedAsync()
    {
        if (await db.Teams.AnyAsync())
        {
            logger.LogInformation("Sample data already present — skipping data seeding.");
            return;
        }

        List<Venue> venues =
        [
            new() { CreatedBy = Domain.Constants.AuditConstants.SystemUser, Name = "Polideportivo Municipal", Address = "Av. Siempre Viva 1234" },
            new() { CreatedBy = Domain.Constants.AuditConstants.SystemUser, Name = "Cancha Norte", Address = "Calle Los Andes 850" },
            new() { CreatedBy = Domain.Constants.AuditConstants.SystemUser, Name = "Estadio Club12", Address = "Ruta 5 km 12" },
        ];

        SampleTournamentBuilder.TournamentDefinition definition = new(
            Name: "Torneo Apertura 2026",
            Description: "Torneo Apertura de la Liga Club12, temporada 2026.",
            TeamRegistrationDeadline: new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 6, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
            MinTeams: 4,
            MaxTeams: 16,
            Divisions:
            [
                new(  "Primera División", PrimeraTeamNames, PrimeraTeamCodes, PrimeraTeamColors),
                new("Reserva", ReservaTeamNames, ReservaTeamCodes, ReservaTeamColors),
            ]);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result = SampleTournamentBuilder.Build(definition, venues, ref playerCounter);

        db.Tournaments.Add(result.Tournament);
        db.PlayerSanctions.AddRange(result.Sanctions);
        db.BlogPosts.AddRange(BuildBlogPosts());

        await db.SaveChangesAsync();

        int teamCount = result.Tournament.Teams.Count;
        int playerCount = result.Tournament.Teams.Sum(t => t.Players.Count);
        logger.LogInformation(
            "Sample data seeded: 1 tournament, 2 divisions, {TeamCount} teams, {PlayerCount} players, {SanctionCount} sanctions.",
            teamCount, playerCount, result.Sanctions.Count);
    }

    private static List<BlogPost> BuildBlogPosts()
    {
        (string Title, string Body)[] posts =
        [
            (
                "Arrancó el Torneo Apertura 2026",
                "La Liga Club12 dio el puntapié inicial al Torneo Apertura 2026, con ocho equipos " +
                "distribuidos en Primera División y Reserva. Los primeros partidos ya se jugaron y " +
                "prometen una temporada muy pareja."
            ),
            (
                "Se viene la fecha 4",
                "Con la fase de grupos en marcha, los equipos se preparan para una nueva fecha. " +
                "Repasá los resultados y las próximas fechas en la sección de partidos."
            ),
        ];

        List<BlogPost> blogPosts = [];

        foreach ((string title, string body) in posts)
        {
            blogPosts.Add(new BlogPost
            {
                CreatedBy = Domain.Constants.AuditConstants.SystemUser,
                Author = "Redacción Club12",
                Title = title,
                Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug(title),
                MarkdownText = body,
                Views = 0,
            });
        }

        return blogPosts;
    }
}
```

- [ ] **Step 3: Run the full existing backend test suite to confirm no regression**

Run: `dotnet test Club12-Backend/Solution/Club12.sln`
Expected: PASS, same pass count as before this change (this is a pure refactor — no new tests here).

- [ ] **Step 4: Commit**

```bash
git add Club12-Backend/Infrastructure/Persistance/SampleTournamentBuilder.cs Club12-Backend/Infrastructure/Persistance/DataSeeder.cs
git commit -m "refactor(backend): extract SampleTournamentBuilder from DataSeeder"
```

---

### Task 2: `DataMaintenanceService.WipeSampleDataAsync()`

**Order note (deviation from the spec, found in pre-flight review):** the spec presented Seed before Wipe, and the original draft of this plan built them in that order. Executing the plan revealed a real test-isolation bug: `CustomWebApplicationFactory` is `IClassFixture`-shared across every `[Fact]` in a test class (one SQLite in-memory database per class, not per fact), and xUnit does not guarantee fact execution order. A "seed on empty database" test and a "seed on non-empty database" test sharing that one database will flake depending on which runs first, because each assumes it starts from empty. Wipe has no such precondition — "assert everything is zero after wiping" holds regardless of what was there before — so Wipe is built and tested first here; every Seed test in Task 3 then calls the now-real `WipeSampleDataAsync()` as its own deterministic setup step, making every fact order-independent. Functionally this task delivers exactly what the spec's Guardrails and Testing sections describe for the wipe endpoint; only the build order changed.

**Files:**
- Create: `Club12-Backend/Application/DTOs/DataMaintenance/Response/DataSeedResult.cs`
- Create: `Club12-Backend/Application/DTOs/DataMaintenance/Response/DataWipeResult.cs`
- Create: `Club12-Backend/Application/Interfaces/Services/IDataMaintenanceService.cs`
- Create: `Club12-Backend/Infrastructure/Persistance/DataMaintenanceService.cs`
- Test: `Club12-Backend/API.Tests/DataMaintenanceServiceTests.cs`

**Interfaces:**
- Consumes: `SampleTournamentBuilder.TournamentDefinition`, `SampleTournamentBuilder.DivisionDefinition`, `SampleTournamentBuilder.BuildResult`, `SampleTournamentBuilder.Build(definition, venues, ref playerCounter) -> BuildResult` (Task 1) — used only by this task's own test fixture helper, not by production code yet.
- Produces: `IDataMaintenanceService.WipeSampleDataAsync(CancellationToken ct = default) -> Task<DataWipeResult>` (fully working), `IDataMaintenanceService.SeedSampleDataAsync(CancellationToken ct = default) -> Task<DataSeedResult>` (signature only — implemented in Task 3), `DataWipeResult(int Tournaments, int Divisions, int Teams, int Players, int Matches, int MatchSeries, int PlayerSanctions, int PlayerStatistics, int Scorers, int StageTeamMatches, int PlayerTeamRegistrations, int Stages, int Venues, int BlogPosts)`, `DataSeedResult(int Tournaments, int Divisions, int Teams, int Players, int Matches, int PlayerSanctions, int BlogPosts)` — Task 3 and Task 4 consume all of these.

- [ ] **Step 1: Write the failing tests**

```csharp
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
}
```

(Verify the exact namespace for `IdentityAppDbContext` with `grep -n "^namespace" Club12-Backend/Infrastructure/Identity/IdentityAppDbContext.cs` before writing this — it is very likely `Infrastructure.Identity` matching the folder, matching every other `Infrastructure.*` class seen so far, but confirm rather than assume.)

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test Club12-Backend/API.Tests --filter DataMaintenanceServiceTests`
Expected: FAIL to compile — `IDataMaintenanceService`, `DataWipeResult` don't exist yet.

- [ ] **Step 3: Create the result DTOs**

`Club12-Backend/Application/DTOs/DataMaintenance/Response/DataSeedResult.cs`:

```csharp
namespace Application.DTOs.DataMaintenance.Response;

/// <summary>
/// Row counts created by DataMaintenanceService.SeedSampleDataAsync, for
/// the admin UI's success summary.
/// </summary>
public sealed record DataSeedResult(
    int Tournaments,
    int Divisions,
    int Teams,
    int Players,
    int Matches,
    int PlayerSanctions,
    int BlogPosts
);
```

`Club12-Backend/Application/DTOs/DataMaintenance/Response/DataWipeResult.cs`:

```csharp
namespace Application.DTOs.DataMaintenance.Response;

/// <summary>
/// Row counts removed by DataMaintenanceService.WipeSampleDataAsync, for
/// the admin UI's success summary. Identity (users, roles) is never
/// touched by the wipe, so it has no counters here.
/// </summary>
public sealed record DataWipeResult(
    int Tournaments,
    int Divisions,
    int Teams,
    int Players,
    int Matches,
    int MatchSeries,
    int PlayerSanctions,
    int PlayerStatistics,
    int Scorers,
    int StageTeamMatches,
    int PlayerTeamRegistrations,
    int Stages,
    int Venues,
    int BlogPosts
);
```

- [ ] **Step 4: Create `IDataMaintenanceService`**

`Club12-Backend/Application/Interfaces/Services/IDataMaintenanceService.cs`:

```csharp
using Application.DTOs.DataMaintenance.Response;

using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Admin-only tools for resetting the tournament-domain data to a clean,
/// realistic sample state (Supabase dev database use case) — see
/// docs/superpowers/specs/2026-08-18-admin-test-data-tools-design.md.
/// Never touches Identity (users, roles).
/// </summary>
public interface IDataMaintenanceService
{
    /// <summary>
    /// Deletes every tournament-domain row (tournaments, divisions, teams,
    /// players, matches, sanctions, statistics, venues, blog posts) inside
    /// one transaction. Identity is untouched.
    /// </summary>
    Task<DataWipeResult> WipeSampleDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Seeds 2 complete, distinct sample tournaments. Throws
    /// <see cref="System.InvalidOperationException"/> if any tournament
    /// already exists — call <see cref="WipeSampleDataAsync"/> first.
    /// </summary>
    Task<DataSeedResult> SeedSampleDataAsync(CancellationToken ct = default);
}
```

- [ ] **Step 5: Implement `DataMaintenanceService` (wipe only — seed throws NotImplementedException for now)**

`Club12-Backend/Infrastructure/Persistance/DataMaintenanceService.cs`:

```csharp
using Application.DTOs.DataMaintenance.Response;
using Application.Interfaces.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistance;

/// <inheritdoc cref="IDataMaintenanceService"/>
public sealed class DataMaintenanceService(ApplicationDBContext db, ILogger<DataMaintenanceService> logger)
    : IDataMaintenanceService
{
    public async Task<DataWipeResult> WipeSampleDataAsync(CancellationToken ct = default)
    {
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await db.Database.BeginTransactionAsync(ct);

        try
        {
            int scorers = await db.Scorers.ExecuteDeleteAsync(ct);
            int playerStatistics = await db.PlayersStatistics.ExecuteDeleteAsync(ct);
            int playerSanctions = await db.PlayerSanctions.ExecuteDeleteAsync(ct);
            int stageTeamMatches = await db.StageTeamMatches.ExecuteDeleteAsync(ct);
            int playerTeamRegistrations = await db.PlayerTeamRegistrations.ExecuteDeleteAsync(ct);
            int matches = await db.Matches.ExecuteDeleteAsync(ct);
            int matchSeries = await db.MatchSeries.ExecuteDeleteAsync(ct);
            int players = await db.Players.ExecuteDeleteAsync(ct);
            int stages = await db.Stages.ExecuteDeleteAsync(ct);
            int teams = await db.Teams.ExecuteDeleteAsync(ct);
            int divisions = await db.Divisions.ExecuteDeleteAsync(ct);
            int tournaments = await db.Tournaments.ExecuteDeleteAsync(ct);
            int venues = await db.Venues.ExecuteDeleteAsync(ct);
            int blogPosts = await db.BlogPosts.ExecuteDeleteAsync(ct);

            await transaction.CommitAsync(ct);

            logger.LogInformation(
                "Sample data wiped: {TournamentCount} tournaments, {DivisionCount} divisions, " +
                "{TeamCount} teams, {PlayerCount} players, {MatchCount} matches, {BlogPostCount} blog posts.",
                tournaments, divisions, teams, players, matches, blogPosts);

            return new DataWipeResult(
                Tournaments: tournaments,
                Divisions: divisions,
                Teams: teams,
                Players: players,
                Matches: matches,
                MatchSeries: matchSeries,
                PlayerSanctions: playerSanctions,
                PlayerStatistics: playerStatistics,
                Scorers: scorers,
                StageTeamMatches: stageTeamMatches,
                PlayerTeamRegistrations: playerTeamRegistrations,
                Stages: stages,
                Venues: venues,
                BlogPosts: blogPosts);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public Task<DataSeedResult> SeedSampleDataAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("Implemented in Task 3.");
}
```

- [ ] **Step 6: Register the new service in DI so the test can resolve it**

In `Club12-Backend/API/Utils/StartupExtensions.cs`, inside `AddDbContextConfig` (the method containing `services.AddScoped<DataSeeder>();` at line 78), add immediately after it:

```csharp
        services.AddScoped<IDataMaintenanceService, DataMaintenanceService>();
```

Add `using Application.Interfaces.Services;` and `using Infrastructure.Persistance;` to the top of `StartupExtensions.cs` if not already present (both are very likely already imported — check before adding, to avoid a duplicate-using compile warning).

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test Club12-Backend/API.Tests --filter DataMaintenanceServiceTests`
Expected: PASS (2 tests).

- [ ] **Step 8: Commit**

```bash
git add Club12-Backend/Application/DTOs/DataMaintenance Club12-Backend/Application/Interfaces/Services/IDataMaintenanceService.cs Club12-Backend/Infrastructure/Persistance/DataMaintenanceService.cs Club12-Backend/API/Utils/StartupExtensions.cs Club12-Backend/API.Tests/DataMaintenanceServiceTests.cs
git commit -m "feat(backend): wipe tournament-domain data via DataMaintenanceService"
```

---

### Task 3: `DataMaintenanceService.SeedSampleDataAsync()` — 2 tournaments

**Files:**
- Modify: `Club12-Backend/Infrastructure/Persistance/DataMaintenanceService.cs`
- Test: `Club12-Backend/API.Tests/DataMaintenanceServiceTests.cs`

**Interfaces:**
- Consumes: `SampleTournamentBuilder.TournamentDefinition`, `SampleTournamentBuilder.DivisionDefinition`, `SampleTournamentBuilder.Build(definition, venues, ref playerCounter) -> BuildResult` (Task 1), `IDataMaintenanceService.WipeSampleDataAsync` (Task 2 — every fact below calls it first as a deterministic setup step, since it now shares `DataMaintenanceServiceTests.cs` with Task 2's facts under one `IClassFixture`-shared database).
- Produces: working `SeedSampleDataAsync` — Task 4's controller and its tests depend on this being correct.

- [ ] **Step 1: Write the failing tests**

Add to `Club12-Backend/API.Tests/DataMaintenanceServiceTests.cs` (as new `[Fact]` methods inside the existing `DataMaintenanceServiceTests` class, alongside Task 2's two facts and its private `BuildAndSaveSampleFixtureAsync` helper):

```csharp
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
    }

    [Fact]
    public async Task SeedSampleDataAsync_OnNonEmptyDatabase_ThrowsInvalidOperationException()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SeedSampleDataAsync());
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
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Club12-Backend/API.Tests --filter DataMaintenanceServiceTests`
Expected: FAIL — `SeedSampleDataAsync` throws `NotImplementedException`.

- [ ] **Step 3: Implement `SeedSampleDataAsync`**

Replace the `SeedSampleDataAsync` stub in `Club12-Backend/Infrastructure/Persistance/DataMaintenanceService.cs`, and add the private team-roster fields the implementation needs (place the fields above the constructor-injected members, following the same layout `DataSeeder` uses):

```csharp
    private static readonly string[] Tournament1PrimeraNames =
        ["Atlético Central", "Deportivo Norte", "Club Belgrano", "Unión del Sur"];
    private static readonly string[] Tournament1PrimeraCodes = ["ATC", "DNO", "CBE", "UDS"];
    private static readonly string[] Tournament1PrimeraColors =
        ["#1E3A8A", "#DC2626", "#16A34A", "#EA580C"];

    private static readonly string[] Tournament1ReservaNames =
        ["Juventud Unida", "Sportivo Oeste", "Estrella Azul", "Náutico River"];
    private static readonly string[] Tournament1ReservaCodes = ["JUN", "SPO", "EAZ", "NRV"];
    private static readonly string[] Tournament1ReservaColors =
        ["#7C3AED", "#0891B2", "#CA8A04", "#4338CA"];

    private static readonly string[] Tournament2PrimeraNames =
        ["Independiente Rural", "Ferroviario Central", "Atlético Cordillera", "Deportivo Litoral"];
    private static readonly string[] Tournament2PrimeraCodes = ["IRU", "FCE", "ACO", "DLI"];
    private static readonly string[] Tournament2PrimeraColors =
        ["#0D9488", "#B91C1C", "#4D7C0F", "#9333EA"];

    private static readonly string[] Tournament2ReservaNames =
        ["Newell's Barrial", "Talleres del Oeste", "Huracán del Valle", "Vélez Serrano"];
    private static readonly string[] Tournament2ReservaCodes = ["NBA", "TDO", "HDV", "VSE"];
    private static readonly string[] Tournament2ReservaColors =
        ["#0284C7", "#65A30D", "#C026D3", "#B45309"];

    public async Task<DataSeedResult> SeedSampleDataAsync(CancellationToken ct = default)
    {
        if (await db.Tournaments.AnyAsync(ct))
        {
            throw new InvalidOperationException(
                "The database already has tournament data — call WipeSampleDataAsync first, then seed again.");
        }

        List<Venue> venues =
        [
            new() { CreatedBy = "system", Name = "Polideportivo Municipal", Address = "Av. Siempre Viva 1234" },
            new() { CreatedBy = "system", Name = "Cancha Norte", Address = "Calle Los Andes 850" },
            new() { CreatedBy = "system", Name = "Estadio Club12", Address = "Ruta 5 km 12" },
        ];

        SampleTournamentBuilder.TournamentDefinition tournament1 = new(
            Name: "Torneo Apertura 2026",
            Description: "Torneo Apertura de la Liga Club12, temporada 2026 — dato de muestra.",
            TeamRegistrationDeadline: new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 6, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
            MinTeams: 4,
            MaxTeams: 16,
            Divisions:
            [
                new("Primera División", Tournament1PrimeraNames, Tournament1PrimeraCodes, Tournament1PrimeraColors),
                new("Reserva", Tournament1ReservaNames, Tournament1ReservaCodes, Tournament1ReservaColors),
            ]);

        SampleTournamentBuilder.TournamentDefinition tournament2 = new(
            Name: "Torneo Clausura 2026",
            Description: "Torneo Clausura de la Liga Club12, temporada 2026 — dato de muestra.",
            TeamRegistrationDeadline: new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 12, 14, 0, 0, 0, DateTimeKind.Utc),
            MinTeams: 4,
            MaxTeams: 16,
            Divisions:
            [
                new("Primera División", Tournament2PrimeraNames, Tournament2PrimeraCodes, Tournament2PrimeraColors),
                new("Reserva", Tournament2ReservaNames, Tournament2ReservaCodes, Tournament2ReservaColors),
            ]);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result1 = SampleTournamentBuilder.Build(tournament1, venues, ref playerCounter);
        SampleTournamentBuilder.BuildResult result2 = SampleTournamentBuilder.Build(tournament2, venues, ref playerCounter);

        db.Tournaments.Add(result1.Tournament);
        db.Tournaments.Add(result2.Tournament);
        db.PlayerSanctions.AddRange(result1.Sanctions);
        db.PlayerSanctions.AddRange(result2.Sanctions);

        List<BlogPost> blogPosts = BuildBlogPosts(tournament1.Name, tournament2.Name);
        db.BlogPosts.AddRange(blogPosts);

        await db.SaveChangesAsync(ct);

        int teamCount = result1.Tournament.Teams.Count + result2.Tournament.Teams.Count;
        int playerCount = result1.Tournament.Teams.Sum(t => t.Players.Count)
            + result2.Tournament.Teams.Sum(t => t.Players.Count);
        int divisionCount = result1.Tournament.Divisions.Count + result2.Tournament.Divisions.Count;
        int matchCount = result1.Tournament.Divisions.Sum(d => d.Stages.Sum(s => s.Matches.Count))
            + result2.Tournament.Divisions.Sum(d => d.Stages.Sum(s => s.Matches.Count));
        int sanctionCount = result1.Sanctions.Count + result2.Sanctions.Count;

        logger.LogInformation(
            "Sample data seeded: 2 tournaments, {DivisionCount} divisions, {TeamCount} teams, " +
            "{PlayerCount} players, {MatchCount} matches, {SanctionCount} sanctions, {BlogPostCount} blog posts.",
            divisionCount, teamCount, playerCount, matchCount, sanctionCount, blogPosts.Count);

        return new DataSeedResult(
            Tournaments: 2,
            Divisions: divisionCount,
            Teams: teamCount,
            Players: playerCount,
            Matches: matchCount,
            PlayerSanctions: sanctionCount,
            BlogPosts: blogPosts.Count);
    }

    private static List<BlogPost> BuildBlogPosts(string tournament1Name, string tournament2Name)
    {
        (string Title, string Body)[] posts =
        [
            (
                $"Arrancó el {tournament1Name}",
                $"La Liga Club12 dio el puntapié inicial al {tournament1Name}, con dos divisiones " +
                "y ocho equipos en cada una. Los primeros partidos ya se jugaron y prometen una " +
                "temporada muy pareja."
            ),
            (
                $"Se define el calendario del {tournament2Name}",
                $"Con el {tournament1Name} en marcha, la Liga Club12 ya confirmó las fechas del " +
                $"{tournament2Name}. Repasá los equipos inscriptos y el fixture completo en la " +
                "sección de torneos."
            ),
        ];

        List<BlogPost> blogPosts = [];

        foreach ((string title, string body) in posts)
        {
            blogPosts.Add(new BlogPost
            {
                CreatedBy = "system",
                Author = "Redacción Club12",
                Title = title,
                Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug(title),
                MarkdownText = body,
                Views = 0,
            });
        }

        return blogPosts;
    }
```

Add `using Domain.Entities.Models;`, `using System.Collections.Generic;`, and `using System.Linq;` to `DataMaintenanceService.cs` if not already present (Task 2 only needed `System` — this step's body needs the rest). `CreatedBy = "system"` matches `Domain.Constants.AuditConstants.SystemUser`'s actual value (verify with `grep -n "SystemUser" Club12-Backend/Domain/Constants/AuditConstants.cs` before writing this for real — if the constant's value differs from the literal `"system"`, use `Domain.Constants.AuditConstants.SystemUser` directly instead of the literal, exactly as `DataSeeder` does).

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Club12-Backend/API.Tests --filter DataMaintenanceServiceTests`
Expected: PASS (5 tests total for this file: Task 2's 2 wipe facts + this task's 3 seed facts).

- [ ] **Step 5: Commit**

```bash
git add Club12-Backend/Infrastructure/Persistance/DataMaintenanceService.cs Club12-Backend/API.Tests/DataMaintenanceServiceTests.cs
git commit -m "feat(backend): seed 2 sample tournaments on demand via DataMaintenanceService"
```

---

### Task 4: `DataMaintenanceController` — Admin-only endpoints

**Files:**
- Create: `Club12-Backend/API/Controllers/DataMaintenanceController.cs`
- Test: `Club12-Backend/API.Tests/DataMaintenanceAuthorizationTests.cs`

**Interfaces:**
- Consumes: `IDataMaintenanceService.SeedSampleDataAsync`, `IDataMaintenanceService.WipeSampleDataAsync` (Task 2, Task 3), `Roles.Admin` (`Domain/Enums/Roles.cs:13`, already exists).
- Produces: `POST api/data-maintenance/wipe`, `POST api/data-maintenance/seed` — Task 7 (frontend service) consumes these exact routes.

- [ ] **Step 1: Write the failing authorization tests**

```csharp
using Domain.Enums;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Proves both data-maintenance endpoints are Admin-only, mirroring the
/// pattern in AuthorizationGatingTests.cs.
/// </summary>
public class DataMaintenanceAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DataMaintenanceAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seed_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/seed", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.TournamentManager)]
    [InlineData(Roles.TeamManager)]
    [InlineData(Roles.Guest)]
    public async Task Seed_NonAdminRole_ReturnsForbidden(string role)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/seed", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Seed_AdminRole_Succeeds()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        // Wipe first: this class shares one CustomWebApplicationFactory (and
        // therefore one database) across every [Fact] via IClassFixture, and
        // xUnit does not guarantee execution order — another fact in this
        // class may have already seeded, which would make this seed call
        // return 409 instead of 200.
        await client.PostAsync("api/data-maintenance/wipe", null);

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/seed", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Wipe_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/wipe", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.TournamentManager)]
    [InlineData(Roles.TeamManager)]
    [InlineData(Roles.Guest)]
    public async Task Wipe_NonAdminRole_ReturnsForbidden(string role)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/wipe", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Wipe_AdminRole_Succeeds()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/wipe", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Seed_OnNonEmptyDatabase_ReturnsConflict()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        // Wipe first, same reasoning as Seed_AdminRole_Succeeds above —
        // guarantees the first seed call below actually starts empty
        // regardless of what other facts in this class already did.
        await client.PostAsync("api/data-maintenance/wipe", null);
        await client.PostAsync("api/data-maintenance/seed", null);

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/seed", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
```

Verify `Roles.TournamentManager`, `Roles.TeamManager`, `Roles.Guest` are the exact constant names in `Domain/Enums/Roles.cs` (`Roles.Admin` and `Roles.Owner` are confirmed; the others were referenced in `AuthorizationGatingTests.cs` — `Roles.Guest`, `Roles.TeamManager` — confirm `Roles.TournamentManager` exists the same way with `grep -n "public const string" Club12-Backend/Domain/Enums/Roles.cs` before running this step for real).

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Club12-Backend/API.Tests --filter DataMaintenanceAuthorizationTests`
Expected: FAIL — 404 (no route registered yet) instead of the expected status codes.

- [ ] **Step 3: Implement the controller**

```csharp
using Application.DTOs.DataMaintenance.Response;
using Application.Interfaces.Services;

using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Admin-only tools for resetting tournament-domain data to a clean,
/// realistic sample state. See
/// docs/superpowers/specs/2026-08-18-admin-test-data-tools-design.md.
/// Identity (users, roles) is never touched by either endpoint.
/// </summary>
[Route("api/data-maintenance")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
public class DataMaintenanceController(IDataMaintenanceService dataMaintenanceService) : ControllerBase
{
    /// <summary>
    /// Deletes every tournament-domain row. Identity is untouched.
    /// </summary>
    [HttpPost("wipe")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataWipeResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DataWipeResult>> Wipe(CancellationToken ct)
    {
        DataWipeResult result = await dataMaintenanceService.WipeSampleDataAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Seeds 2 complete sample tournaments. Returns 409 if the database
    /// already has tournament data — wipe first.
    /// </summary>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DataSeedResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DataSeedResult>> Seed(CancellationToken ct)
    {
        DataSeedResult result = await dataMaintenanceService.SeedSampleDataAsync(ct);
        return Ok(result);
    }
}
```

The 409 on a duplicate seed comes from `GlobalExceptionHandler` mapping `InvalidOperationException` → `409 Conflict` automatically (`Club12-Backend/API/Utils/GlobalExceptionHandler.cs:89`) — no try/catch needed in the controller.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Club12-Backend/API.Tests --filter DataMaintenanceAuthorizationTests`
Expected: PASS (11 tests: 1 anonymous + 4 wrong-role + 1 admin-succeeds, ×2 endpoints, minus the extra conflict test = 9 + 1 conflict = matches the file above).

- [ ] **Step 5: Run the entire backend test suite one more time**

Run: `dotnet test Club12-Backend/Solution/Club12.sln`
Expected: PASS, no regressions anywhere else in the suite.

- [ ] **Step 6: Commit**

```bash
git add Club12-Backend/API/Controllers/DataMaintenanceController.cs Club12-Backend/API.Tests/DataMaintenanceAuthorizationTests.cs
git commit -m "feat(backend): expose wipe/seed as Admin-only endpoints"
```

---

### Task 5: Frontend — routes, icon, nav tab

**Files:**
- Modify: `Club12-WebClient/src/modules/core/constants/routes.ts`
- Modify: `Club12-WebClient/src/modules/core/constants/appRoutes.ts`
- Modify: `Club12-WebClient/src/views/core/MUI/icons/icons.tsx`
- Modify: `Club12-WebClient/src/views/core/components/SidebarLayout.tsx`

**Interfaces:**
- Produces: `routes.dataMaintenance` (API path segment), `APP_ROUTES.panelTest` (frontend route path) — Task 6 and Task 7 consume both.

No backend interfaces are touched by this task, and there is no meaningful unit test for a route-constant/nav-array addition in this codebase's existing test style (no test file covers `TABS_BY_ROLE` or `routes.ts` today) — this task is verified by a TypeScript compile check instead of a new test file.

- [ ] **Step 1: Add the API route segment**

In `Club12-WebClient/src/modules/core/constants/routes.ts`, add one line inside the object (alphabetical-ish placement, matching the existing loose ordering — after `blogposts`, before `divisions`):

```ts
  blogposts: 'blogposts',
  dataMaintenance: 'data-maintenance',
  divisions: 'divisions',
```

- [ ] **Step 2: Add the frontend page route**

In `Club12-WebClient/src/modules/core/constants/appRoutes.ts`, add inside `APP_ROUTES`, right after `panelStatistics`:

```ts
  panelStatistics: '/panel/estadisticas',
  panelTest: '/panel/test',
```

(Confirm the exact existing value of `panelStatistics` with `grep -n "panelStatistics" Club12-WebClient/src/modules/core/constants/appRoutes.ts` before editing — this plan assumes `/panel/estadisticas` based on the Spanish-label convention every other panel route in this file follows; if the file's actual current lines differ, insert `panelTest` as a new line immediately after whatever the real `panelStatistics` line is, without altering it.)

- [ ] **Step 3: Add the new icon**

In `Club12-WebClient/src/views/core/MUI/icons/icons.tsx`, add the import (alongside the other `@mui/icons-material` imports):

```ts
import ScienceIcon from '@mui/icons-material/Science';
```

And add `ScienceIcon` to the `export { ... }` block.

- [ ] **Step 4: Add the nav tab**

In `Club12-WebClient/src/views/core/components/SidebarLayout.tsx`:

Add `ScienceIcon` to the icon import block (alongside `BarChartIcon` etc.).

Add to `TAB_ICONS` (after the `Estadisticas` entry):

```ts
  Estadisticas: <BarChartIcon />,
  Test: <ScienceIcon />,
```

Add to `TABS_BY_ROLE[UserRolesType.Admin]` (after the `Estadisticas` tab entry, so it renders directly below it as requested):

```ts
    {
      label: 'Estadisticas',
      path: APP_ROUTES.panelStatistics,
      icon: TAB_ICONS['Estadisticas'],
    },
    {
      label: 'Test',
      path: APP_ROUTES.panelTest,
      icon: TAB_ICONS['Test'],
    },
```

- [ ] **Step 5: Verify the frontend still compiles**

Run: `cd Club12-WebClient && npm run build`
Expected: build succeeds with no TypeScript errors (a missing `ScienceIcon` export or a typo'd `APP_ROUTES.panelTest` reference would fail here).

- [ ] **Step 6: Commit**

```bash
git add Club12-WebClient/src/modules/core/constants/routes.ts Club12-WebClient/src/modules/core/constants/appRoutes.ts Club12-WebClient/src/views/core/MUI/icons/icons.tsx Club12-WebClient/src/views/core/components/SidebarLayout.tsx
git commit -m "feat(frontend): add Admin-only Test nav tab"
```

---

### Task 6: Frontend — `dataMaintenance` service and types

**Files:**
- Create: `Club12-WebClient/src/modules/dataMaintenance/type/dataMaintenance.d.ts`
- Create: `Club12-WebClient/src/modules/dataMaintenance/service/dataMaintenance.service.ts`

**Interfaces:**
- Consumes: `routes.dataMaintenance` (Task 5), `sendPost` from `@/modules/core/utils/axiosUtils`.
- Produces: `dataMaintenanceService.wipeSampleData()`, `dataMaintenanceService.seedSampleData()` — Task 8 (the view) consumes both.

- [ ] **Step 1: Add the response types**

`Club12-WebClient/src/modules/dataMaintenance/type/dataMaintenance.d.ts`:

```ts
export interface IDataWipeResult {
  tournaments: number;
  divisions: number;
  teams: number;
  players: number;
  matches: number;
  matchSeries: number;
  playerSanctions: number;
  playerStatistics: number;
  scorers: number;
  stageTeamMatches: number;
  playerTeamRegistrations: number;
  stages: number;
  venues: number;
  blogPosts: number;
}

export interface IDataSeedResult {
  tournaments: number;
  divisions: number;
  teams: number;
  players: number;
  matches: number;
  playerSanctions: number;
  blogPosts: number;
}
```

- [ ] **Step 2: Add the service**

`Club12-WebClient/src/modules/dataMaintenance/service/dataMaintenance.service.ts`:

```ts
import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { sendPost } from '@/modules/core/utils/axiosUtils';
import {
  IDataSeedResult,
  IDataWipeResult,
} from '@/modules/dataMaintenance/type/dataMaintenance';

/**
 * Admin-only tools for resetting tournament-domain data to a clean,
 * realistic sample state.
 */
export const dataMaintenanceService = {
  /**
   * Deletes every tournament-domain row. Identity is untouched.
   * @returns {Promise<AxiosResponse<IDataWipeResult>>} Row counts removed.
   */
  wipeSampleData: async (): Promise<AxiosResponse<IDataWipeResult>> =>
    await sendPost(`${routes.dataMaintenance}/wipe`),

  /**
   * Seeds 2 complete sample tournaments. Rejects with a 409 response if
   * the database already has tournament data.
   * @returns {Promise<AxiosResponse<IDataSeedResult>>} Row counts created.
   */
  seedSampleData: async (): Promise<AxiosResponse<IDataSeedResult>> =>
    await sendPost(`${routes.dataMaintenance}/seed`),
};
```

- [ ] **Step 3: Verify the frontend still compiles**

Run: `cd Club12-WebClient && npm run build`
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add Club12-WebClient/src/modules/dataMaintenance
git commit -m "feat(frontend): add dataMaintenance service"
```

---

### Task 7: Frontend — `TestDataPage` view and route registration

**Files:**
- Create: `Club12-WebClient/src/views/panel/TestDataPage.tsx`
- Modify: `Club12-WebClient/src/App.tsx`

**Interfaces:**
- Consumes: `dataMaintenanceService.wipeSampleData`, `dataMaintenanceService.seedSampleData` (Task 6), `confirmDelete`, `notifySuccess`, `notifyError` from `@/modules/core/utils/confirmDialog` (existing), `APP_ROUTES.panelTest` (Task 5).

- [ ] **Step 1: Write the view**

`Club12-WebClient/src/views/panel/TestDataPage.tsx`:

```tsx
import { useState } from 'react';
import { Box, Button, Card, CardContent, Stack, Typography } from '@mui/material';
import ScienceIcon from '@mui/icons-material/Science';
import DeleteSweepIcon from '@mui/icons-material/DeleteSweep';
import { dataMaintenanceService } from '@/modules/dataMaintenance/service/dataMaintenance.service';
import {
  confirmDelete,
  notifyError,
  notifySuccess,
} from '@/modules/core/utils/confirmDialog';

const TestDataPage: React.FC = () => {
  const [isWiping, setIsWiping] = useState(false);
  const [isSeeding, setIsSeeding] = useState(false);

  const handleWipe = async (): Promise<void> => {
    const confirmed = await confirmDelete({
      title: '¿Borrar todos los datos de prueba?',
      text: 'Se eliminan todos los torneos, equipos, jugadores, partidos, sanciones y estadísticas. Los usuarios no se ven afectados. Esta acción no se puede deshacer.',
      confirmButtonText: 'Sí, borrar todo',
    });

    if (!confirmed) {
      return;
    }

    setIsWiping(true);
    try {
      const response = await dataMaintenanceService.wipeSampleData();
      await notifySuccess({
        title: 'Base de datos vaciada',
        text: `${response.data.tournaments} torneos, ${response.data.teams} equipos y ${response.data.players} jugadores eliminados.`,
      });
    } catch {
      await notifyError({
        title: 'No se pudo borrar la base de datos',
        text: 'Volvé a intentar en unos segundos.',
      });
    } finally {
      setIsWiping(false);
    }
  };

  const handleSeed = async (): Promise<void> => {
    setIsSeeding(true);
    try {
      const response = await dataMaintenanceService.seedSampleData();
      await notifySuccess({
        title: 'Datos de prueba cargados',
        text: `${response.data.tournaments} torneos, ${response.data.teams} equipos y ${response.data.players} jugadores creados.`,
      });
    } catch {
      await notifyError({
        title: 'No se pudieron cargar los datos de prueba',
        text: 'Si la base ya tiene datos, borrala primero con "Borrar DB".',
      });
    } finally {
      setIsSeeding(false);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Herramientas de datos de prueba
      </Typography>
      <Card>
        <CardContent>
          <Typography variant="body2" sx={{ mb: 3 }}>
            Estas herramientas afectan solo torneos, equipos, jugadores, partidos,
            sanciones y estadísticas. Los usuarios y roles nunca se tocan.
          </Typography>
          <Stack direction="row" spacing={2}>
            <Button
              variant="outlined"
              color="error"
              startIcon={<DeleteSweepIcon />}
              disabled={isWiping || isSeeding}
              onClick={handleWipe}
            >
              Borrar DB
            </Button>
            <Button
              variant="contained"
              startIcon={<ScienceIcon />}
              disabled={isWiping || isSeeding}
              onClick={handleSeed}
            >
              Cargar Datos de prueba
            </Button>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
};

export default TestDataPage;
```

`DeleteSweepIcon` is imported directly from `@mui/icons-material` here rather than through `views/core/MUI/icons/icons.tsx` because that wrapper module is specifically for sidebar nav icons (confirmed by its only other consumer, `SidebarLayout.tsx`) — every other panel view (`StatisticsPage.tsx` included) imports MUI icons directly, not through that wrapper.

- [ ] **Step 2: Register the route**

In `Club12-WebClient/src/App.tsx`, add the import (alongside `import StatisticsPage from './views/panel/StatisticsPage';`):

```ts
import TestDataPage from './views/panel/TestDataPage';
```

Add to `ADMIN_ROUTES` (right after the `panelStatistics` entry):

```ts
  {
    path: APP_ROUTES.panelStatistics,
    allowedRoles: [UserRolesType.Admin],
    element: <StatisticsPage />,
  },
  {
    path: APP_ROUTES.panelTest,
    allowedRoles: [UserRolesType.Admin],
    element: <TestDataPage />,
  },
```

- [ ] **Step 3: Verify the frontend still compiles**

Run: `cd Club12-WebClient && npm run build`
Expected: build succeeds.

- [ ] **Step 4: Manual smoke test**

Run: `cd Club12-WebClient && npm run dev`, log in as an Admin user (locally, against whatever backend this frontend is pointed at), confirm:
- The "Test" tab appears in the sidebar below "Estadisticas", only for the Admin role.
- Clicking it navigates to `/panel/test` and renders the two buttons.
- "Borrar DB" shows the confirmation dialog before doing anything.
- Both buttons show a success or error toast after the backend responds.

- [ ] **Step 5: Commit**

```bash
git add Club12-WebClient/src/views/panel/TestDataPage.tsx Club12-WebClient/src/App.tsx
git commit -m "feat(frontend): add Test panel page for wipe/seed sample data"
```

---

## Post-implementation checklist (not a task — a reminder for whoever merges this)

- Run `detect_changes()` (GitNexus) before committing the final task, per this repo's CLAUDE.md, to confirm the affected-symbol set matches what this plan intended (`DataSeeder`, the new `DataMaintenance*` symbols, `SidebarLayout`, `App.tsx` — nothing else).
- The spec's Guardrails section flags a follow-up not covered by this plan: nothing currently stops these endpoints from being used once the Supabase database holds real data. Track that separately before this feature is exercised against a database with real users.
