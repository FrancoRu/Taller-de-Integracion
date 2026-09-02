using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Covers the gap found auditing historias-de-usuario.md against the code
/// (2026-09-02): a real tournament's auto-seed (StageService.SeedPlayoffCupsAsync)
/// used to always drop a single plain Match into a BestOf&gt;1 slot — it never
/// created a MatchSeries, so a configured "mejor de 3" never actually produced
/// 3 games. Only the demo seed builder did this correctly. These tests cover
/// the fix: real MatchSeries creation at seed time, and automatic advancement
/// of a decided slot's winner into the next round (StageService.
/// TryAdvanceStageWinnerAsync), which did not exist in any form before.
/// </summary>
public class PlayoffSeriesAdvancementTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayoffSeriesAdvancementTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedPlayoffCupsAsync_BestOfThreeSemiFinal_FourTeams_CreatesRealSeriesForBothPairings()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null, bestOf: 1);
        await SeedRoundRobinResultsAsync(db, groupStage, teams);

        await SeedMappingAsync(db, division, 1, 4, "Copa Única");
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 3);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);

        await stageService.SeedPlayoffCupsAsync(division.Id);

        List<Match> semiFinalMatches = await db.Matches
            .Where(m => m.StageId == semiFinalStage.Id)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();

        Assert.Equal(2, semiFinalMatches.Count);
        Assert.All(semiFinalMatches, m =>
        {
            Assert.True(m.SeriesId.HasValue);
            Assert.Equal(1, m.GameNumber);
            Assert.False(m.IsFinished);
        });

        List<MatchSeries> series = await db.MatchSeries
            .Where(s => s.StageId == semiFinalStage.Id)
            .ToListAsync();

        Assert.Equal(2, series.Count);
        Assert.All(series, s =>
        {
            Assert.Equal(3, s.BestOf);
            Assert.NotEqual(s.HomeTeamId, s.VisitorTeamId);
            Assert.False(s.WinningTeamId.HasValue);
        });
    }

    [Fact]
    public async Task SeedPlayoffCupsAsync_BestOfThreeStage_ByePairing_StaysAPlainFinishedMatch_NoSeries()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        // 3 qualifiers -> next power of two is 4 -> one bye for the best seed.
        List<Team> teams = await SeedTeamsAsync(db, tournament, 3);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null, bestOf: 1);
        await SeedRoundRobinResultsAsync(db, groupStage, teams);

        await SeedMappingAsync(db, division, 1, 3, "Copa Única");
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 3);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);

        await stageService.SeedPlayoffCupsAsync(division.Id);

        List<Match> semiFinalMatches = await db.Matches
            .Where(m => m.StageId == semiFinalStage.Id)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();

        Match bye = Assert.Single(semiFinalMatches, m => !m.VisitorTeamId.HasValue);
        Assert.True(bye.IsFinished);
        Assert.Equal(bye.HomeTeamId, bye.WinningTeamId);
        Assert.False(bye.SeriesId.HasValue);

        Match realPairing = Assert.Single(semiFinalMatches, m => m.VisitorTeamId.HasValue);
        Assert.True(realPairing.SeriesId.HasValue);
    }

    [Fact]
    public async Task DecidingBothSemiFinalSeries_AdvancesBothWinners_AndStartsARealFinalSeries()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
        IMatchSeriesService matchSeriesService = scope.ServiceProvider.GetRequiredService<IMatchSeriesService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null, bestOf: 1);
        await SeedRoundRobinResultsAsync(db, groupStage, teams);

        await SeedMappingAsync(db, division, 1, 4, "Copa Única");
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 3);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: "Copa Única", bestOf: 3);
        await SeedEmptyMatchAsync(db, finalStage);

        await stageService.SeedPlayoffCupsAsync(division.Id);

        List<Match> semiFinalGame1s = await db.Matches
            .Where(m => m.StageId == semiFinalStage.Id)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();

        // Decide EACH series 2-0 (best of 3, majority = 2): load game 1's
        // result (already exists from seeding), then add and load game 2 —
        // exactly what the missing "add next game" admin action now lets an
        // admin do (MatchSeriesController.AddGameToSeries).
        foreach (Match game1 in semiFinalGame1s)
        {
            Guid seriesId = game1.SeriesId!.Value;
            Guid homeTeamId = game1.HomeTeamId!.Value;

            await DecideSeriesInFavorOfHomeAsync(
                db, matchService, matchSeriesService, stageService, seriesId, homeTeamId, semiFinalStage.Id);
        }

        Match finalMatch = await db.Matches.SingleAsync(m => m.StageId == finalStage.Id);

        Assert.True(finalMatch.HomeTeamId.HasValue);
        Assert.True(finalMatch.VisitorTeamId.HasValue);
        Assert.NotEqual(finalMatch.HomeTeamId, finalMatch.VisitorTeamId);
        Assert.True(finalMatch.SeriesId.HasValue, "the final's slot should have become game 1 of a real series once both semifinal winners arrived");
        Assert.Equal(1, finalMatch.GameNumber);

        MatchSeries finalSeries = await db.MatchSeries.SingleAsync(s => s.Id == finalMatch.SeriesId!.Value);
        Assert.Equal(3, finalSeries.BestOf);
        Assert.Equal(finalMatch.HomeTeamId, finalSeries.HomeTeamId);
        Assert.Equal(finalMatch.VisitorTeamId, finalSeries.VisitorTeamId);

        // Bracket adjacency (PlayoffSeeder's seed order [1,4,2,3]): slot 0
        // (seed 1 vs seed 4) feeds the final's Home; slot 1 (seed 2 vs seed 3)
        // feeds the final's Visitor.
        Match slot0 = semiFinalGame1s[0];
        Match slot1 = semiFinalGame1s[1];
        Assert.Equal(slot0.HomeTeamId, finalMatch.HomeTeamId);
        Assert.Equal(slot1.HomeTeamId, finalMatch.VisitorTeamId);
    }

    [Fact]
    public async Task DecidingBothSemiFinals_PushesBothLosersIntoTheThirdPlaceMatch()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null, bestOf: 1);
        await SeedRoundRobinResultsAsync(db, groupStage, teams);

        await SeedMappingAsync(db, division, 1, 4, "Copa Única");
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, finalStage);
        Stage thirdPlaceStage = await SeedStageAsync(db, division, tournament, StageType.ThirdPlace, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, thirdPlaceStage);

        await stageService.SeedPlayoffCupsAsync(division.Id);

        List<Match> semiFinalMatches = await db.Matches
            .Where(m => m.StageId == semiFinalStage.Id)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();
        // Home always wins here, so slot 0's visitor and slot 1's visitor are
        // the two losers expected to land in the third-place match.
        Guid expectedThirdPlaceHome = semiFinalMatches[0].VisitorTeamId!.Value;
        Guid expectedThirdPlaceVisitor = semiFinalMatches[1].VisitorTeamId!.Value;

        foreach (Match match in semiFinalMatches)
        {
            await matchService.LoadMatchResultAsync(match.Id, 90, 80);
            await stageService.TryAdvanceStageWinnerAsync(semiFinalStage.Id);
        }

        Match thirdPlaceMatch = await db.Matches.SingleAsync(m => m.StageId == thirdPlaceStage.Id);
        Assert.Equal(expectedThirdPlaceHome, thirdPlaceMatch.HomeTeamId);
        Assert.Equal(expectedThirdPlaceVisitor, thirdPlaceMatch.VisitorTeamId);
        Assert.False(thirdPlaceMatch.SeriesId.HasValue, "a BestOf=1 third place decider stays a plain match");

        // The winners still went to the final as usual — third place is an
        // additive side effect, not a replacement for the main advancement.
        Match finalMatch = await db.Matches.SingleAsync(m => m.StageId == finalStage.Id);
        Assert.True(finalMatch.HomeTeamId.HasValue);
        Assert.True(finalMatch.VisitorTeamId.HasValue);
        Assert.False(finalMatch.HomeTeamId == expectedThirdPlaceHome || finalMatch.HomeTeamId == expectedThirdPlaceVisitor);
        Assert.False(finalMatch.VisitorTeamId == expectedThirdPlaceHome || finalMatch.VisitorTeamId == expectedThirdPlaceVisitor);
    }

    [Fact]
    public async Task ThirdPlaceStage_WithBestOfGreaterThanOne_BecomesARealSeriesOnceBothLosersArrive()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null, bestOf: 1);
        await SeedRoundRobinResultsAsync(db, groupStage, teams);

        await SeedMappingAsync(db, division, 1, 4, "Copa Única");
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, finalStage);
        Stage thirdPlaceStage = await SeedStageAsync(db, division, tournament, StageType.ThirdPlace, bracketName: "Copa Única", bestOf: 3);
        await SeedEmptyMatchAsync(db, thirdPlaceStage);

        await stageService.SeedPlayoffCupsAsync(division.Id);

        List<Match> semiFinalMatches = await db.Matches
            .Where(m => m.StageId == semiFinalStage.Id)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();

        foreach (Match match in semiFinalMatches)
        {
            await matchService.LoadMatchResultAsync(match.Id, 90, 80);
            await stageService.TryAdvanceStageWinnerAsync(semiFinalStage.Id);
        }

        Match thirdPlaceMatch = await db.Matches.SingleAsync(m => m.StageId == thirdPlaceStage.Id);
        Assert.True(thirdPlaceMatch.SeriesId.HasValue, "a BestOf>1 third place decider should become game 1 of a real series");
        Assert.Equal(1, thirdPlaceMatch.GameNumber);

        MatchSeries thirdPlaceSeries = await db.MatchSeries.SingleAsync(s => s.Id == thirdPlaceMatch.SeriesId!.Value);
        Assert.Equal(3, thirdPlaceSeries.BestOf);
        Assert.Equal(thirdPlaceMatch.HomeTeamId, thirdPlaceSeries.HomeTeamId);
        Assert.Equal(thirdPlaceMatch.VisitorTeamId, thirdPlaceSeries.VisitorTeamId);
    }

    [Fact]
    public async Task NoThirdPlaceStageConfigured_AdvancingSemiFinalWinners_IsANoOp_NeverThrows()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null, bestOf: 1);
        await SeedRoundRobinResultsAsync(db, groupStage, teams);

        await SeedMappingAsync(db, division, 1, 4, "Copa Única");
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, finalStage);
        // No ThirdPlace stage seeded for this cup — the admin opted out.

        await stageService.SeedPlayoffCupsAsync(division.Id);

        List<Match> semiFinalMatches = await db.Matches
            .Where(m => m.StageId == semiFinalStage.Id)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();

        foreach (Match match in semiFinalMatches)
        {
            await matchService.LoadMatchResultAsync(match.Id, 90, 80);
            await stageService.TryAdvanceStageWinnerAsync(semiFinalStage.Id);
        }

        Match finalMatch = await db.Matches.SingleAsync(m => m.StageId == finalStage.Id);
        Assert.True(finalMatch.HomeTeamId.HasValue);
        Assert.True(finalMatch.VisitorTeamId.HasValue);
    }

    [Fact]
    public async Task BestOfOneElimination_StillAdvancesTheWinnerDirectly_NoSeriesInvolved()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null, bestOf: 1);
        await SeedRoundRobinResultsAsync(db, groupStage, teams);

        await SeedMappingAsync(db, division, 1, 4, "Copa Única");
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, finalStage);

        await stageService.SeedPlayoffCupsAsync(division.Id);

        List<Match> semiFinalMatches = await db.Matches
            .Where(m => m.StageId == semiFinalStage.Id)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();

        Assert.All(semiFinalMatches, m => Assert.False(m.SeriesId.HasValue));

        foreach (Match match in semiFinalMatches)
        {
            Match? updated = await matchService.LoadMatchResultAsync(match.Id, 90, 80);
            Assert.NotNull(updated);
            await stageService.TryAdvanceStageWinnerAsync(semiFinalStage.Id);
        }

        Match finalMatch = await db.Matches.SingleAsync(m => m.StageId == finalStage.Id);
        Assert.True(finalMatch.HomeTeamId.HasValue);
        Assert.True(finalMatch.VisitorTeamId.HasValue);
        Assert.False(finalMatch.SeriesId.HasValue, "a BestOf=1 next round stays a plain match, never a series");
    }

    /// <summary>
    /// Mirrors exactly what MatchController's result-loading actions do after
    /// this session's fix: load a game's result, recalculate the series
    /// winner if the game belongs to one, then try to advance the stage's
    /// winner into the next round. Plays game 1 (already 90-80 for home from
    /// seeding — re-asserted here) then adds and plays game 2, also 90-80 for
    /// home, which reaches the best-of-3 majority (2 games) and decides it.
    /// </summary>
    private static async Task DecideSeriesInFavorOfHomeAsync(
        ApplicationDBContext db,
        IMatchService matchService,
        IMatchSeriesService matchSeriesService,
        IStageService stageService,
        Guid seriesId,
        Guid homeTeamId,
        Guid decidedStageId)
    {
        Match game1 = await db.Matches.SingleAsync(m => m.SeriesId == seriesId && m.GameNumber == 1);
        await matchService.LoadMatchResultAsync(game1.Id, 90, 80);
        await matchSeriesService.RecalculateSeriesWinnerAsync(seriesId);
        await stageService.TryAdvanceStageWinnerAsync(decidedStageId);

        Match game2 = await matchSeriesService.AddGameToSeriesAsync(seriesId, DateTime.UtcNow.AddDays(1), venueId: null);
        await matchService.LoadMatchResultAsync(game2.Id, 90, 80);
        await matchSeriesService.RecalculateSeriesWinnerAsync(seriesId);
        await stageService.TryAdvanceStageWinnerAsync(decidedStageId);

        MatchSeries decided = await db.MatchSeries.SingleAsync(s => s.Id == seriesId);
        Assert.Equal(homeTeamId, decided.WinningTeamId);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);
        Tournament tournament = new()
        {
            Description = "Series-advancement test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
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
            IsCrossDivisionCup = false,
            Stages = [],
            CreatedBy = "test",
        };
        db.Divisions.Add(division);
        await db.SaveChangesAsync();
        return division;
    }

    private static async Task SeedMappingAsync(ApplicationDBContext db, Division division, int from, int to, string destination)
    {
        db.DivisionPlayoffMappings.Add(new DivisionPlayoffMapping
        {
            DivisionId = division.Id,
            FromPosition = from,
            ToPosition = to,
            Destination = destination,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();
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

    private static async Task<Stage> SeedStageAsync(
        ApplicationDBContext db, Division division, Tournament tournament, StageType stageType, string? bracketName, int bestOf)
    {
        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = stageType,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(7),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            BracketName = bracketName,
            BestOf = bestOf,
            CreatedBy = "test",
        };
        db.Stages.Add(stage);
        await db.SaveChangesAsync();
        return stage;
    }

    /// <summary>
    /// Plays a single round-robin among every pair, the lower index always
    /// winning, so final standings are teams[0] &gt; teams[1] &gt; … — enough
    /// for PositionCalculator/PlayoffQualificationResolver to produce a
    /// deterministic seed order for SeedPlayoffCupsAsync.
    /// </summary>
    private static async Task SeedRoundRobinResultsAsync(ApplicationDBContext db, Stage groupStage, List<Team> teams)
    {
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                db.Matches.Add(new Match
                {
                    StageId = groupStage.Id,
                    Type = MatchType.Regular,
                    Slug = $"match-{Guid.NewGuid()}",
                    MatchDate = groupStage.StartDate,
                    HomeTeamId = teams[i].Id,
                    VisitorTeamId = teams[j].Id,
                    HomeScore = 90,
                    VisitorScore = 80,
                    IsFinished = true,
                    WinningTeamId = teams[i].Id,
                    CreatedBy = "test",
                });
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedEmptyMatchAsync(ApplicationDBContext db, Stage stage)
    {
        int existingCount = await db.Matches.CountAsync(m => m.StageId == stage.Id);
        db.Matches.Add(new Match
        {
            StageId = stage.Id,
            Type = MatchType.Playoff,
            Slug = $"match-{Guid.NewGuid()}",
            MatchDate = stage.StartDate.AddMinutes(existingCount),
            IsFinished = false,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();
    }
}
