using Application.DTOs.Scorer.Request;
using Application.DTOs.Scorer.Response;
using Application.DTOs.Statistics.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Historical statistics across seasons (HU-85 / HU-87 / HU-88).
///
/// "Season" is defined as the calendar year of a tournament's StartDate — the
/// simplest value derivable from the existing model with no schema change. A
/// person is joined across seasons by their stable PlayerId: Player.DocumentNumber
/// is uniquely indexed, so one real person is exactly one Player row reused
/// across seasons (season scoping lives in PlayerTeamRegistration, not in
/// duplicate Player rows). These tests seed the same Player scoring in two
/// tournaments of different years and assert the season, all-time, card, and
/// history aggregations.
///
/// Every query is scoped by PlayerId (or by the player's own tournaments) so the
/// class-shared SQLite database (IClassFixture) never leaks another test's rows.
/// </summary>
public class HistoricalStatisticsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HistoricalStatisticsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeasonRanking_GroupsOnlyThatYearsTournaments()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        SeededPlayer seed = await SeedPlayerAcrossTwoSeasonsAsync(db, points2026: 10, points2027: 7);

        (IEnumerable<ScorerByPlayerResponse> items2026, int total2026) =
            await scorerRepository.GetPlayerScoresAsync(new GetScorerFilteredRequest { PlayerId = seed.PlayerId, Season = 2026 });
        ScorerByPlayerResponse in2026 = Assert.Single(items2026);
        Assert.Equal(1, total2026);
        Assert.Equal(10, in2026.Points);

        (IEnumerable<ScorerByPlayerResponse> items2027, _) =
            await scorerRepository.GetPlayerScoresAsync(new GetScorerFilteredRequest { PlayerId = seed.PlayerId, Season = 2027 });
        ScorerByPlayerResponse in2027 = Assert.Single(items2027);
        Assert.Equal(7, in2027.Points);
    }

    [Fact]
    public async Task AllTimeRanking_SumsSamePersonAcrossSeasons()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        SeededPlayer seed = await SeedPlayerAcrossTwoSeasonsAsync(db, points2026: 10, points2027: 7);

        // No TournamentId and no Season -> all-time: the same PlayerId aggregates
        // every point across both seasons (10 + 7).
        (IEnumerable<ScorerByPlayerResponse> allTime, _) =
            await scorerRepository.GetPlayerScoresAsync(new GetScorerFilteredRequest { PlayerId = seed.PlayerId });
        ScorerByPlayerResponse person = Assert.Single(allTime);
        Assert.Equal(17, person.Points);
    }

    [Fact]
    public async Task PerTournamentRanking_StillWorks()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        SeededPlayer seed = await SeedPlayerAcrossTwoSeasonsAsync(db, points2026: 10, points2027: 7);

        (IEnumerable<ScorerByPlayerResponse> items, int total) =
            await scorerRepository.GetPlayerScoresAsync(new GetScorerFilteredRequest { TournamentId = seed.Tournament2026Id });
        ScorerByPlayerResponse only = Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal(seed.PlayerId, only.PlayerId);
        Assert.Equal(10, only.Points);
    }

    [Fact]
    public async Task PlayerCard_ReturnsPerSeasonAndOverallTotalsAndAverages()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStatisticsService statisticsService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();

        // 2026: two matches (6 + 4 = 10 over 2 games). 2027: one match (7 over 1 game).
        SeededPlayer seed = await SeedPlayerAcrossTwoSeasonsAsync(db, points2026: 6, points2027: 7);
        await AddPointsAsync(db, seed.PlayerId, await SeedMatchInSeasonAsync(db, seed, season2026: true), points: 4);

        PlayerStatisticCardResponse? card = await statisticsService.GetPlayerCardAsync(seed.PlayerId);

        Assert.NotNull(card);
        Assert.Equal(17, card!.TotalPoints);
        Assert.Equal(3, card.GamesPlayed);
        Assert.Equal(5.67, card.AveragePoints);

        List<SeasonStatLineResponse> seasons = [.. card.Seasons];
        Assert.Equal(2, seasons.Count);
        // Most recent season first.
        Assert.Equal(2027, seasons[0].Season);
        Assert.Equal(7, seasons[0].TotalPoints);
        Assert.Equal(1, seasons[0].GamesPlayed);
        Assert.Equal(7.0, seasons[0].AveragePoints);

        Assert.Equal(2026, seasons[1].Season);
        Assert.Equal(10, seasons[1].TotalPoints);
        Assert.Equal(2, seasons[1].GamesPlayed);
        Assert.Equal(5.0, seasons[1].AveragePoints);
    }

    [Fact]
    public async Task PlayerHistory_ReturnsPerSeasonTeamStatsAndSanctions()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStatisticsService statisticsService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();

        SeededPlayer seed = await SeedPlayerAcrossTwoSeasonsAsync(db, points2026: 10, points2027: 7);
        await AddSanctionAsync(db, seed.PlayerId, seed.Match2026Id, "Technical foul", duration: 1);

        PlayerHistoryResponse? history = await statisticsService.GetPlayerHistoryAsync(seed.PlayerId);

        Assert.NotNull(history);
        List<PlayerHistorySeasonResponse> seasons = [.. history!.Seasons];
        Assert.Equal(2, seasons.Count);

        PlayerHistorySeasonResponse recent = seasons[0];
        Assert.Equal(2027, recent.Season);
        Assert.Equal(seed.Team2027Id, recent.TeamId);
        Assert.Equal(7, recent.TotalPoints);
        Assert.Equal(1, recent.GamesPlayed);
        Assert.Empty(recent.Sanctions);

        PlayerHistorySeasonResponse older = seasons[1];
        Assert.Equal(2026, older.Season);
        Assert.Equal(seed.Team2026Id, older.TeamId);
        Assert.Equal(10, older.TotalPoints);
        Assert.Equal(1, older.GamesPlayed);
        PlayerHistorySanctionResponse sanction = Assert.Single(older.Sanctions);
        Assert.Equal("Technical foul", sanction.Description);
        Assert.Equal(seed.Match2026Id, sanction.MatchId);
    }

    // ---- seeding helpers ----

    private sealed record SeededPlayer(
        Guid PlayerId,
        Guid Tournament2026Id,
        Guid Tournament2027Id,
        Guid Team2026Id,
        Guid Team2027Id,
        Guid Match2026Id,
        Guid Match2027Id);

    /// <summary>
    /// Seeds ONE player (a single Player row, stable PlayerId) registered to a
    /// team in a 2026 tournament and another team in a 2027 tournament, scoring
    /// the given points in one match of each season.
    /// </summary>
    private static async Task<SeededPlayer> SeedPlayerAcrossTwoSeasonsAsync(
        ApplicationDBContext db, int points2026, int points2027)
    {
        Tournament t2026 = await SeedTournamentAsync(db, 2026);
        Tournament t2027 = await SeedTournamentAsync(db, 2027);

        Team team2026 = await SeedTeamAsync(db, t2026.Id);
        Team team2027 = await SeedTeamAsync(db, t2027.Id);

        Player player = await SeedPlayerAsync(db, team2026);
        await SeedRegistrationAsync(db, player.Id, team2026.Id, t2026.Id);
        await SeedRegistrationAsync(db, player.Id, team2027.Id, t2027.Id);

        Match match2026 = await SeedMatchAsync(db, await SeedStageAsync(db, t2026), team2026.Id);
        Match match2027 = await SeedMatchAsync(db, await SeedStageAsync(db, t2027), team2027.Id);

        await AddPointsAsync(db, player.Id, match2026.Id, points2026);
        await AddPointsAsync(db, player.Id, match2027.Id, points2027);

        return new SeededPlayer(player.Id, t2026.Id, t2027.Id, team2026.Id, team2027.Id, match2026.Id, match2027.Id);
    }

    private static async Task<Guid> SeedMatchInSeasonAsync(ApplicationDBContext db, SeededPlayer seed, bool season2026)
    {
        Guid tournamentId = season2026 ? seed.Tournament2026Id : seed.Tournament2027Id;
        Guid teamId = season2026 ? seed.Team2026Id : seed.Team2027Id;
        Tournament tournament = await db.Tournaments.FindAsync(tournamentId)
            ?? throw new InvalidOperationException("seed tournament missing");
        Match match = await SeedMatchAsync(db, await SeedStageAsync(db, tournament), teamId);
        return match.Id;
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, int year)
    {
        DateTime startDate = new(year, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        Tournament tournament = new()
        {
            Description = $"Season {year} tournament",
            Name = $"Tournament-{year}-{Guid.NewGuid()}",
            Slug = $"tournament-{year}-{Guid.NewGuid()}",
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

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db, Tournament tournament)
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

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(14),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
    }

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, Guid? tournamentId)
    {
        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            TournamentId = tournamentId,
            Players = [],
            CreatedBy = "test",
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        return team;
    }

    private static async Task<Player> SeedPlayerAsync(ApplicationDBContext db, Team team)
    {
        Player player = new()
        {
            Slug = $"player-{Guid.NewGuid()}",
            FirstName = "Cross",
            LastName = "Season",
            SecondName = null,
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = team.Id,
            CreatedBy = "test",
        };

        db.Players.Add(player);
        await db.SaveChangesAsync();

        return player;
    }

    private static async Task SeedRegistrationAsync(ApplicationDBContext db, Guid playerId, Guid teamId, Guid tournamentId)
    {
        db.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
        {
            PlayerId = playerId,
            TeamId = teamId,
            TournamentId = tournamentId,
            CreatedBy = "test",
        });

        await db.SaveChangesAsync();
    }

    private static async Task<Match> SeedMatchAsync(ApplicationDBContext db, Stage stage, Guid homeTeamId)
    {
        Match match = new()
        {
            MatchDate = stage.StartDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            IsFinished = true,
            Stage = stage,
            StageId = stage.Id,
            HomeTeamId = homeTeamId,
            VisitorTeamId = null,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return match;
    }

    private static async Task AddPointsAsync(ApplicationDBContext db, Guid playerId, Guid matchId, int points)
    {
        db.PlayersStatistics.Add(new PlayerStatistic
        {
            PlayerId = playerId,
            MatchId = matchId,
            Value = points,
            Type = StatisticType.Points,
            CreatedBy = "test",
        });

        await db.SaveChangesAsync();
    }

    private static async Task AddSanctionAsync(ApplicationDBContext db, Guid playerId, Guid matchId, string description, int duration)
    {
        db.PlayerSanctions.Add(new PlayerSanction
        {
            PlayerId = playerId,
            MatchId = matchId,
            Match = null!,
            Description = description,
            Duration = duration,
            IssuedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Slug = $"sanction-{Guid.NewGuid()}",
            SubjectType = SanctionSubjectType.Player,
            CreatedBy = "test",
        });

        await db.SaveChangesAsync();
    }
}
