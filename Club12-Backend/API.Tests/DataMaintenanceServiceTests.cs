using Application.DTOs.DataMaintenance.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

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
            new() { Slug = $"venue-{Guid.NewGuid()}", CreatedBy = "test", Name = "Cancha de prueba", Address = "Calle Falsa 123" },
        ];

        SampleTournamentBuilder.TournamentDefinition definition = new(
            Name: "Torneo de prueba",
            Description: "Fixture de prueba para WipeSampleDataAsync.",
            TeamRegistrationDeadline: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
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

        // Main "Apertura": Primera(8) + Segunda(8) + Copa Club 12 (cross cup) = 3 divisions,
        // 16 teams, 128 players. Historical "Clausura": Primera(4) + Reserva(4) = 2 divisions,
        // 8 teams, 64 players. Totals: 5 divisions, 24 teams, 192 players.
        Assert.Equal(2, result.Tournaments);
        Assert.Equal(5, result.Divisions);
        Assert.Equal(24, result.Teams);
        Assert.Equal(192, result.Players);
        Assert.Equal(6, result.BlogPosts);
        Assert.True(result.Matches > 0);
        Assert.True(result.PlayerSanctions >= 5);

        List<Tournament> tournaments = await db.Tournaments.ToListAsync();
        Assert.Equal(2, tournaments.Count);
        Assert.NotEqual(tournaments[0].Name, tournaments[1].Name);
        Assert.NotEqual(tournaments[0].Slug, tournaments[1].Slug);

        Assert.True(await db.Scorers.CountAsync() > 0);
        Assert.True(await db.PlayersStatistics.CountAsync() > 0);
        Assert.Equal(6, await db.Venues.CountAsync());
        Assert.True(await db.Matches.AnyAsync(m => m.IsFinished && m.HomeScore != null));
    }

    [Fact]
    public async Task SeedSampleDataAsync_MainDivisions_AreNamedAndHaveEightTeamsEach()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();

        foreach (string divisionName in new[] { "Primera División", "Segunda División" })
        {
            Division division = await db.Divisions
                .Include(d => d.Stages).ThenInclude(s => s.StageTeamMatches)
                .SingleAsync(d => d.Name == divisionName);

            Stage groupStage = Assert.Single(division.Stages, s => s.StageType == StageType.Group);
            Assert.Equal(8, groupStage.StageTeamMatches.Count);
        }
    }

    [Fact]
    public async Task SeedSampleDataAsync_MainDivisions_HaveCopaOroAndCopaPlataMappingsAndBrackets()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();

        foreach (string divisionName in new[] { "Primera División", "Segunda División" })
        {
            Division division = await db.Divisions
                .Include(d => d.Stages)
                .Include(d => d.PlayoffMappings)
                .SingleAsync(d => d.Name == divisionName);

            // Position-range mappings (HU-45): 1-4 -> Copa Oro, 5-8 -> Copa Plata.
            DivisionPlayoffMapping oro = Assert.Single(division.PlayoffMappings, m => m.Destination == "Copa Oro");
            Assert.Equal(1, oro.FromPosition);
            Assert.Equal(4, oro.ToPosition);
            DivisionPlayoffMapping plata = Assert.Single(division.PlayoffMappings, m => m.Destination == "Copa Plata");
            Assert.Equal(5, plata.FromPosition);
            Assert.Equal(8, plata.ToPosition);

            // Each cup has a best-of-3 SemiFinal + Final bracket carrying its BracketName.
            foreach (string cup in new[] { "Copa Oro", "Copa Plata" })
            {
                List<Stage> cupStages = [.. division.Stages.Where(s => s.BracketName == cup)];
                Assert.Contains(cupStages, s => s.StageType == StageType.SemiFinal);
                Assert.Contains(cupStages, s => s.StageType == StageType.Final);
                Assert.All(cupStages, s => Assert.Equal(3, s.BestOf));
            }
        }
    }

    [Fact]
    public async Task SeedSampleDataAsync_FinalStages_AreFinishedWithAWinner()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();

        List<Stage> finalStages = await db.Stages
            .Where(s => s.StageType == StageType.Final)
            .Include(s => s.Matches)
            .Include(s => s.MatchSeries).ThenInclude(ms => ms.Matches)
            .ToListAsync();

        // 2 cups x 2 main divisions + 1 cross cup + 2 historical divisions = 7 finals.
        Assert.Equal(7, finalStages.Count);

        foreach (Stage finalStage in finalStages)
        {
            if (finalStage.BestOf > 1)
            {
                // A BestOf > 1 final is a REAL MatchSeries (Copa Oro/Copa
                // Plata for both main divisions), decided with between the
                // minimum-to-clinch and BestOf finished games — not one
                // collapsed match.
                MatchSeries series = Assert.Single(finalStage.MatchSeries);
                Assert.NotNull(series.WinningTeamId);
                Assert.Equal(
                    series.WinningTeamId,
                    Application.Utils.Helper.Series.SeriesDecisionCalculator.DetermineWinner(series));

                int gamesToWin = (series.BestOf / 2) + 1;
                Assert.InRange(series.Matches.Count, gamesToWin, series.BestOf);
                Assert.All(series.Matches, m => Assert.True(m.IsFinished));
                Assert.Equal(series.Matches.Count, finalStage.Matches.Count);
            }
            else
            {
                Match finalMatch = Assert.Single(finalStage.Matches);
                Assert.True(finalMatch.IsFinished);
                Assert.NotNull(finalMatch.WinningTeamId);
            }
        }
    }

    [Fact]
    public async Task SeedSampleDataAsync_CrossDivisionCup_ExistsWithFourGroupsAndQualifiersPerGroup()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();

        Division crossCup = await db.Divisions
            .Include(d => d.Stages).ThenInclude(s => s.StageTeamMatches)
            .SingleAsync(d => d.IsCrossDivisionCup);

        Assert.Equal("Copa Club 12", crossCup.Name);
        Assert.Equal(1, crossCup.QualifiersPerGroup);

        List<Stage> groups = [.. crossCup.Stages.Where(s => s.StageType == StageType.Group)];
        Assert.Equal(4, groups.Count);
        Assert.All(groups, g => Assert.Equal(4, g.StageTeamMatches.Count));

        // The pooled bracket exists (SemiFinal + Final).
        Assert.Contains(crossCup.Stages, s => s.StageType == StageType.SemiFinal);
        Assert.Contains(crossCup.Stages, s => s.StageType == StageType.Final);
    }

    [Fact]
    public async Task SeedSampleDataAsync_GroupMatches_HaveJornadasWithEveryTeamOncePerRound()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();

        List<Match> groupMatches = await db.Matches
            .Where(m => m.Stage.StageType == StageType.Group)
            .ToListAsync();

        Assert.NotEmpty(groupMatches);
        // Every group match belongs to a jornada.
        Assert.All(groupMatches, m => Assert.NotNull(m.Round));

        // For the Primera División group: 7 jornadas, each with every one of the
        // 8 teams appearing exactly once (4 matches, 8 distinct team slots).
        Division primera = await db.Divisions
            .Include(d => d.Stages).ThenInclude(s => s.Matches)
            .SingleAsync(d => d.Name == "Primera División");
        Stage primeraGroup = primera.Stages.Single(s => s.StageType == StageType.Group);

        List<IGrouping<int, Match>> rounds = [.. primeraGroup.Matches.GroupBy(m => m.Round!.Value)];
        Assert.Equal(7, rounds.Count);
        foreach (IGrouping<int, Match> round in rounds)
        {
            List<Guid?> teamSlots =
            [
                .. round.Select(m => m.HomeTeamId),
                .. round.Select(m => m.VisitorTeamId),
            ];
            Assert.Equal(8, teamSlots.Count);
            Assert.Equal(8, teamSlots.Distinct().Count());
        }
    }

    [Fact]
    public async Task SeedSampleDataAsync_ZoneAndCrossCupJornadas_FallOnDifferentWeekdays()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();

        // Regular zone group matches are played on Sundays.
        Division primera = await db.Divisions
            .Include(d => d.Stages).ThenInclude(s => s.Matches)
            .SingleAsync(d => d.Name == "Primera División");
        Stage primeraGroup = primera.Stages.Single(s => s.StageType == StageType.Group);
        Assert.All(primeraGroup.Matches, m => Assert.Equal(DayOfWeek.Sunday, m.MatchDate.DayOfWeek));

        // Cross-cup group matches are played on Wednesdays (HU-111), so they can
        // never collide with the zone Sundays.
        Division crossCup = await db.Divisions
            .Include(d => d.Stages).ThenInclude(s => s.Matches)
            .SingleAsync(d => d.IsCrossDivisionCup);
        List<Match> crossGroupMatches = [.. crossCup.Stages
            .Where(s => s.StageType == StageType.Group)
            .SelectMany(s => s.Matches)];
        Assert.NotEmpty(crossGroupMatches);
        Assert.All(crossGroupMatches, m => Assert.Equal(DayOfWeek.Wednesday, m.MatchDate.DayOfWeek));
    }

    [Fact]
    public async Task SeedSampleDataAsync_SeedsVariedSanctionsAndSeveralSpanishNews()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();

        List<PlayerSanction> sanctions = await db.PlayerSanctions.ToListAsync();
        Assert.True(sanctions.Count >= 5);
        // Variety: at least one Team sanction, one appeal pending, one served/rejected.
        Assert.Contains(sanctions, s => s.SubjectType == SanctionSubjectType.Team && s.TeamId != null);
        Assert.Contains(sanctions, s => s.SubjectType == SanctionSubjectType.Player && s.PlayerId != null);
        Assert.Contains(sanctions, s => s.AppealStatus == SanctionAppealStatus.Pending);
        // Active sanctions actually flag their player as sanctioned.
        Assert.True(await db.Players.AnyAsync(p => p.IsSanctioned));

        // News: several posts, all with Spanish content, mixing published and draft.
        List<BlogPost> posts = await db.BlogPosts.ToListAsync();
        Assert.True(posts.Count >= 5);
        Assert.Contains(posts, p => p.IsPublished);
        Assert.Contains(posts, p => !p.IsPublished);
    }

    [Fact]
    public async Task SeedSampleDataAsync_ProducesCoherentStandingsBracketsAndOneTeamOneZone()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();

        // Standings over the finished group matches are complete: every one of
        // the 8 teams has played all 7 of its games.
        Division primera = await db.Divisions
            .Include(d => d.Stages).ThenInclude(s => s.Matches).ThenInclude(m => m.HomeTeam)
            .Include(d => d.Stages).ThenInclude(s => s.Matches).ThenInclude(m => m.VisitorTeam)
            .Include(d => d.Stages).ThenInclude(s => s.MatchSeries)
            .SingleAsync(d => d.Name == "Primera División");
        Stage primeraGroup = primera.Stages.Single(s => s.StageType == StageType.Group);

        List<Domain.Entities.Models.Position> standings =
            Application.Utils.Helper.Standings.PositionCalculator.CalculatePositions(primeraGroup.Matches);
        Assert.Equal(8, standings.Count);
        foreach (Domain.Entities.Models.Position position in standings)
        {
            Assert.Equal(7, position.MatchesPlayed);
        }

        // Copa Oro's semifinal teams are exactly the top 4 of those standings.
        // The SemiFinal is BestOf=3, so it's a real MatchSeries per pairing
        // (one row per pairing) rather than Matches (which now holds every
        // individual game — 2 or 3 per pairing — so team ids repeat there).
        List<Guid> top4 = [.. standings.Take(4).Select(p => p.TeamId)];
        Stage oroSemi = primera.Stages.Single(s => s.BracketName == "Copa Oro" && s.StageType == StageType.SemiFinal);
        Assert.Equal(2, oroSemi.MatchSeries.Count);
        List<Guid> oroSemiTeams =
        [
            .. oroSemi.MatchSeries.Select(s => s.HomeTeamId),
            .. oroSemi.MatchSeries.Select(s => s.VisitorTeamId),
        ];
        Assert.Equal([.. top4.OrderBy(x => x)], [.. oroSemiTeams.OrderBy(x => x)]);

        // Every main-tournament team belongs to exactly one regular zone (one
        // StageTeamMatch across the two divisions' group stages) AND to exactly
        // one cross-cup group.
        Tournament apertura = await db.Tournaments.SingleAsync(t => t.Name == "Torneo Apertura 2026");
        List<Guid> divisionIds = await db.Divisions
            .Where(d => d.TournamentId == apertura.Id && !d.IsCrossDivisionCup)
            .Select(d => d.Id).ToListAsync();
        Guid crossCupId = await db.Divisions
            .Where(d => d.TournamentId == apertura.Id && d.IsCrossDivisionCup)
            .Select(d => d.Id).SingleAsync();

        List<Guid> teamIds = await db.Teams.Where(t => t.TournamentId == apertura.Id).Select(t => t.Id).ToListAsync();
        Assert.Equal(16, teamIds.Count);

        foreach (Guid teamId in teamIds)
        {
            int zoneGroupCount = await db.StageTeamMatches.CountAsync(stm =>
                stm.TeamId == teamId
                && stm.Stage!.StageType == StageType.Group
                && divisionIds.Contains(stm.Stage.DivisionId));
            Assert.Equal(1, zoneGroupCount);

            int crossGroupCount = await db.StageTeamMatches.CountAsync(stm =>
                stm.TeamId == teamId
                && stm.Stage!.StageType == StageType.Group
                && stm.Stage.DivisionId == crossCupId);
            Assert.Equal(1, crossGroupCount);
        }

        // Season registrations exist for every team and player of the season.
        Assert.Equal(16, await db.Set<TeamTournamentRegistration>().CountAsync(r => r.TournamentId == apertura.Id));
        Assert.Equal(128, await db.Set<PlayerTeamRegistration>().CountAsync(r => r.TournamentId == apertura.Id));
    }

    [Fact]
    public async Task SeedSampleDataAsync_OnEmptyDatabase_EveryGroupStageMatchIsFinished()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        await service.WipeSampleDataAsync();
        await service.SeedSampleDataAsync();

        List<Match> groupMatches = await db.Matches
            .Where(m => m.Stage.StageType == StageType.Group)
            .ToListAsync();

        Assert.NotEmpty(groupMatches);
        Assert.All(groupMatches, m => Assert.True(m.IsFinished));
    }

    [Fact]
    public void Build_WithoutPlayoffs_ProducesOnlyGroupStagePerDivision()
    {
        List<Venue> venues =
        [
            new() { Slug = $"venue-{Guid.NewGuid()}", CreatedBy = "test", Name = "Cancha de prueba", Address = "Calle Falsa 123" },
        ];

        SampleTournamentBuilder.TournamentDefinition definition = new(
            Name: "Torneo sin playoffs",
            Description: "Fixture de prueba para el camino de arranque (DataSeeder) sin playoffs.",
            TeamRegistrationDeadline: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new(
                    "Primera",
                    ["Equipo A", "Equipo B", "Equipo C", "Equipo D"],
                    ["EQA", "EQB", "EQC", "EQD"],
                    ["#111111", "#222222", "#333333", "#444444"]),
                new(
                    "Reserva",
                    ["Equipo E", "Equipo F", "Equipo G", "Equipo H"],
                    ["EQE", "EQF", "EQG", "EQH"],
                    ["#555555", "#666666", "#777777", "#888888"]),
            ]);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result = SampleTournamentBuilder.Build(definition, venues, ref playerCounter);

        foreach (Division division in result.Tournament.Divisions)
        {
            Stage stage = Assert.Single(division.Stages);
            Assert.Equal(StageType.Group, stage.StageType);
        }
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
