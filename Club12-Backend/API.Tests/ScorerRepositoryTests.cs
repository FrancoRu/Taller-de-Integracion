using Application.DTOs.Scorer.Request;
using Application.DTOs.Scorer.Response;
using Application.Interfaces.Repositories;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Characterization tests for ScorerRepository.GetPlayerScoresAsync
/// (ScorerRepository.cs:17-66), the EF aggregation query that produces the
/// top-scorer ranking (per-player point totals, name formatting, ordering,
/// pagination, and filters). These tests pin the currently-correct behavior
/// of the existing, unmodified repository — pure test additions, no
/// production code changes.
///
/// Known test-harness limitation: the query uses `ToUpper()`, null-coalescing
/// string concatenation, and a correlated `Sum` subquery, all EF-translated to
/// SQL. SQLite (test provider) may translate these differently than Npgsql
/// (production). All seed data below is ASCII-only so SQLite's `upper()`
/// matches Npgsql `UPPER`, and every scenario in this file executed and
/// asserted successfully against the SQLite-backed harness — no runtime
/// translation failure was observed for `ToUpper`, the null-coalescing
/// concatenation, or the correlated `Sum` subquery.
///
/// Each test scopes its query with `TournamentId` or `PlayerId` so that the
/// class-shared CustomWebApplicationFactory database (via IClassFixture) never
/// leaks another test's seeded rows into this test's assertions.
/// </summary>
public class ScorerRepositoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ScorerRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPlayerScoresAsync_MultipleScores_SumsPoints()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        (Player player, Match match, _) = await SeedPlayerWithMatchAsync(db, "Sums", "Player", secondName: null);
        await AddScorerAsync(db, player.Id, match.Id, points: 2);
        await AddScorerAsync(db, player.Id, match.Id, points: 3);

        GetScorerFilteredRequest filter = new() { PlayerId = player.Id };
        (IEnumerable<ScorerByPlayerResponse> items, int totalCount) = await scorerRepository.GetPlayerScoresAsync(filter);

        ScorerByPlayerResponse result = Assert.Single(items);
        Assert.Equal(5, result.Points);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    public async Task GetPlayerScoresAsync_NoScores_DefaultsToZero()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        (Player player, _, _) = await SeedPlayerWithMatchAsync(db, "Scoreless", "Player", secondName: null);

        GetScorerFilteredRequest filter = new() { PlayerId = player.Id };
        (IEnumerable<ScorerByPlayerResponse> items, _) = await scorerRepository.GetPlayerScoresAsync(filter);

        ScorerByPlayerResponse result = Assert.Single(items);
        Assert.Equal(0, result.Points);
    }

    [Fact]
    public async Task GetPlayerScoresAsync_NoSecondName_FormatsWithoutTrailingSeparator()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        (Player player, _, _) = await SeedPlayerWithMatchAsync(db, "Smith", "John", secondName: null);

        GetScorerFilteredRequest filter = new() { PlayerId = player.Id };
        (IEnumerable<ScorerByPlayerResponse> items, _) = await scorerRepository.GetPlayerScoresAsync(filter);

        ScorerByPlayerResponse result = Assert.Single(items);
        Assert.Equal("SMITH John", result.FullName);
    }

    [Fact]
    public async Task GetPlayerScoresAsync_WithSecondName_AppendsSecondName()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        (Player player, _, _) = await SeedPlayerWithMatchAsync(db, "Doe", "Jane", secondName: "Marie");

        GetScorerFilteredRequest filter = new() { PlayerId = player.Id };
        (IEnumerable<ScorerByPlayerResponse> items, _) = await scorerRepository.GetPlayerScoresAsync(filter);

        ScorerByPlayerResponse result = Assert.Single(items);
        Assert.Equal("DOE Jane Marie", result.FullName);
    }

    [Fact]
    public async Task GetPlayerScoresAsync_DistinctTotals_OrdersDescending()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        Tournament tournament = await SeedTournamentAsync(db);
        (Player low, Match match, _) = await SeedPlayerInTournamentAsync(db, tournament, "Low", "Scorer");
        (Player mid, _, _) = await SeedPlayerInTournamentAsync(db, tournament, "Mid", "Scorer", match);
        (Player high, _, _) = await SeedPlayerInTournamentAsync(db, tournament, "High", "Scorer", match);

        await AddScorerAsync(db, low.Id, match.Id, points: 1);
        await AddScorerAsync(db, mid.Id, match.Id, points: 5);
        await AddScorerAsync(db, high.Id, match.Id, points: 9);

        GetScorerFilteredRequest filter = new() { TournamentId = tournament.Id };
        (IEnumerable<ScorerByPlayerResponse> items, int totalCount) = await scorerRepository.GetPlayerScoresAsync(filter);

        List<ScorerByPlayerResponse> ordered = [.. items];
        Assert.Equal(3, totalCount);
        Assert.Equal(3, ordered.Count);
        Assert.Equal(high.Id, ordered[0].PlayerId);
        Assert.Equal(mid.Id, ordered[1].PlayerId);
        Assert.Equal(low.Id, ordered[2].PlayerId);
        Assert.Equal(9, ordered[0].Points);
        Assert.Equal(5, ordered[1].Points);
        Assert.Equal(1, ordered[2].Points);
    }

    [Fact]
    public async Task GetPlayerScoresAsync_PageTwo_ReturnsNextSliceAndFullTotalCount()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        Tournament tournament = await SeedTournamentAsync(db);

        int[] pointsByRank = [50, 40, 30, 20, 10];
        List<Player> players = [];
        Match? sharedMatch = null;

        for (int i = 0; i < pointsByRank.Length; i++)
        {
            (Player player, Match match, _) = await SeedPlayerInTournamentAsync(db, tournament, $"Rank{i}", "Scorer", sharedMatch);
            sharedMatch ??= match;
            players.Add(player);
            await AddScorerAsync(db, player.Id, match.Id, points: pointsByRank[i]);
        }

        GetScorerFilteredRequest filter = new() { TournamentId = tournament.Id, PageNumber = 2, PageSize = 2 };
        (IEnumerable<ScorerByPlayerResponse> items, int totalCount) = await scorerRepository.GetPlayerScoresAsync(filter);

        List<ScorerByPlayerResponse> page = [.. items];
        Assert.Equal(5, totalCount);
        Assert.Equal(2, page.Count);
        ScorerByPlayerResponse thirdPlace = page[0];
        ScorerByPlayerResponse fourthPlace = page[1];
        Assert.Equal(30, thirdPlace.Points);
        Assert.Equal(20, fourthPlace.Points);
    }

    [Fact]
    public async Task GetPlayerScoresAsync_TournamentFilter_RestrictsPlayersAndScorers()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        Tournament tournamentA = await SeedTournamentAsync(db);
        Tournament tournamentB = await SeedTournamentAsync(db);

        (Player playerA, Match matchA, _) = await SeedPlayerInTournamentAsync(db, tournamentA, "Alpha", "Player");
        await AddScorerAsync(db, playerA.Id, matchA.Id, points: 7);

        (Player playerB, Match matchB, _) = await SeedPlayerInTournamentAsync(db, tournamentB, "Bravo", "Player");
        await AddScorerAsync(db, playerB.Id, matchB.Id, points: 4);

        GetScorerFilteredRequest filter = new() { TournamentId = tournamentA.Id };
        (IEnumerable<ScorerByPlayerResponse> items, int totalCount) = await scorerRepository.GetPlayerScoresAsync(filter);

        List<ScorerByPlayerResponse> result = [.. items];
        Assert.Equal(1, totalCount);
        ScorerByPlayerResponse single = Assert.Single(result);
        Assert.Equal(playerA.Id, single.PlayerId);
        Assert.Equal(7, single.Points);
    }

    [Fact]
    public async Task GetPlayerScoresAsync_MatchTeamOrPlayerFilter_NarrowsResultSet()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        Tournament tournament = await SeedTournamentAsync(db);
        (_, Stage stage) = await SeedDivisionAndStageAsync(db, tournament);

        Team homeTeam = await SeedTeamAsync(db, tournament.Id);
        Team visitorTeam = await SeedTeamAsync(db, tournament.Id);
        Team unrelatedTeam = await SeedTeamAsync(db, tournament.Id);

        Player homePlayer = await SeedPlayerAsync(db, homeTeam, "Home", "Player", null);
        Player visitorPlayer = await SeedPlayerAsync(db, visitorTeam, "Visitor", "Player", null);
        Player unrelatedPlayer = await SeedPlayerAsync(db, unrelatedTeam, "Unrelated", "Player", null);

        Match sharedMatch = await SeedMatchAsync(db, stage, homeTeamId: homeTeam.Id, visitorTeamId: visitorTeam.Id);
        Match otherMatch = await SeedMatchAsync(db, stage, homeTeamId: unrelatedTeam.Id, visitorTeamId: null);

        await AddScorerAsync(db, homePlayer.Id, sharedMatch.Id, points: 3);
        await AddScorerAsync(db, visitorPlayer.Id, sharedMatch.Id, points: 6);
        await AddScorerAsync(db, unrelatedPlayer.Id, otherMatch.Id, points: 9);

        GetScorerFilteredRequest matchFilter = new() { MatchId = sharedMatch.Id };
        (IEnumerable<ScorerByPlayerResponse> matchItems, int matchTotal) = await scorerRepository.GetPlayerScoresAsync(matchFilter);
        List<Guid> matchPlayerIds = [.. matchItems.Select(i => i.PlayerId)];
        Assert.Equal(2, matchTotal);
        Assert.Contains(homePlayer.Id, matchPlayerIds);
        Assert.Contains(visitorPlayer.Id, matchPlayerIds);
        Assert.DoesNotContain(unrelatedPlayer.Id, matchPlayerIds);

        GetScorerFilteredRequest teamFilter = new() { TeamId = homeTeam.Id };
        (IEnumerable<ScorerByPlayerResponse> teamItems, int teamTotal) = await scorerRepository.GetPlayerScoresAsync(teamFilter);
        ScorerByPlayerResponse teamResult = Assert.Single(teamItems);
        Assert.Equal(1, teamTotal);
        Assert.Equal(homePlayer.Id, teamResult.PlayerId);

        GetScorerFilteredRequest playerFilter = new() { PlayerId = visitorPlayer.Id };
        (IEnumerable<ScorerByPlayerResponse> playerItems, int playerTotal) = await scorerRepository.GetPlayerScoresAsync(playerFilter);
        ScorerByPlayerResponse playerResult = Assert.Single(playerItems);
        Assert.Equal(1, playerTotal);
        Assert.Equal(visitorPlayer.Id, playerResult.PlayerId);
        Assert.Equal(6, playerResult.Points);
    }

    [Fact]
    public async Task GetPlayerScoresAsync_DivisionFilter_RestrictsToThatDivisionsScorers()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        Tournament tournament = await SeedTournamentAsync(db);
        (Division divisionA, Stage stageA) = await SeedDivisionAndStageAsync(db, tournament);
        (_, Stage stageB) = await SeedDivisionAndStageAsync(db, tournament);

        Team teamA = await SeedTeamAsync(db, tournament.Id);
        Team teamB = await SeedTeamAsync(db, tournament.Id);
        await AssignTeamToStageAsync(db, stageA, teamA);
        await AssignTeamToStageAsync(db, stageB, teamB);

        Player playerA = await SeedPlayerAsync(db, teamA, "Alpha", "Player", null);
        Player playerB = await SeedPlayerAsync(db, teamB, "Bravo", "Player", null);

        Match matchA = await SeedMatchAsync(db, stageA, homeTeamId: teamA.Id, visitorTeamId: null);
        Match matchB = await SeedMatchAsync(db, stageB, homeTeamId: teamB.Id, visitorTeamId: null);
        await AddScorerAsync(db, playerA.Id, matchA.Id, points: 8);
        await AddScorerAsync(db, playerB.Id, matchB.Id, points: 3);

        GetScorerFilteredRequest divisionFilter = new() { DivisionId = divisionA.Id };
        (IEnumerable<ScorerByPlayerResponse> divisionItems, int divisionTotal) = await scorerRepository.GetPlayerScoresAsync(divisionFilter);
        ScorerByPlayerResponse divisionResult = Assert.Single(divisionItems);
        Assert.Equal(1, divisionTotal);
        Assert.Equal(playerA.Id, divisionResult.PlayerId);
        Assert.Equal(8, divisionResult.Points);

        GetScorerFilteredRequest stageFilter = new() { StageId = stageB.Id };
        (IEnumerable<ScorerByPlayerResponse> stageItems, int stageTotal) = await scorerRepository.GetPlayerScoresAsync(stageFilter);
        ScorerByPlayerResponse stageResult = Assert.Single(stageItems);
        Assert.Equal(1, stageTotal);
        Assert.Equal(playerB.Id, stageResult.PlayerId);
        Assert.Equal(3, stageResult.Points);
    }

    private static async Task AssignTeamToStageAsync(ApplicationDBContext db, Stage stage, Team team)
    {
        db.StageTeamMatches.Add(new StageTeamMatch
        {
            StageId = stage.Id,
            TeamId = team.Id,
            CreatedBy = "test",
        });

        await db.SaveChangesAsync();
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Scorer ranking characterization tournament",
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

    private static async Task<(Division division, Stage stage)> SeedDivisionAndStageAsync(ApplicationDBContext db, Tournament tournament)
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

        return (division, stage);
    }

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, Guid? tournamentId)
    {
        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Yellow",
            TournamentId = tournamentId,
            Players = [],
            CreatedBy = "test",
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        return team;
    }

    private static async Task<Player> SeedPlayerAsync(ApplicationDBContext db, Team team, string lastName, string firstName, string? secondName)
    {
        Player player = new()
        {
            Slug = $"player-{Guid.NewGuid()}",
            FirstName = firstName,
            LastName = lastName,
            SecondName = secondName,
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

    private static async Task<Match> SeedMatchAsync(ApplicationDBContext db, Stage stage, Guid? homeTeamId, Guid? visitorTeamId)
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
            VisitorTeamId = visitorTeamId,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return match;
    }

    /// <summary>
    /// Seeds a full standalone graph (own tournament/division/stage/team/match) for a single
    /// player, used by tests that scope the query via PlayerId and don't care about a shared
    /// tournament.
    /// </summary>
    private static async Task<(Player player, Match match, Team team)> SeedPlayerWithMatchAsync(
        ApplicationDBContext db, string lastName, string firstName, string? secondName)
    {
        Tournament tournament = await SeedTournamentAsync(db);
        return await SeedPlayerInTournamentAsync(db, tournament, lastName, firstName, sharedMatch: null, secondName);
    }

    /// <summary>
    /// Seeds a player (with its own team) inside an existing tournament, reusing a shared
    /// match when provided (so multiple players can be scored against the same match/stage),
    /// or creating a new division/stage/match otherwise.
    /// </summary>
    private static async Task<(Player player, Match match, Team team)> SeedPlayerInTournamentAsync(
        ApplicationDBContext db, Tournament tournament, string lastName, string firstName,
        Match? sharedMatch = null, string? secondName = null)
    {
        Team team = await SeedTeamAsync(db, tournament.Id);
        Player player = await SeedPlayerAsync(db, team, lastName, firstName, secondName);

        Match match;
        if (sharedMatch is not null)
        {
            match = sharedMatch;
        }
        else
        {
            (_, Stage stage) = await SeedDivisionAndStageAsync(db, tournament);
            match = await SeedMatchAsync(db, stage, homeTeamId: team.Id, visitorTeamId: null);
        }

        return (player, match, team);
    }

    /// <summary>
    /// HU-72: the goleadores ranking now aggregates from PlayerStatistic
    /// (Type == Points) — the same table the per-match loading path writes —
    /// instead of the retired Scorer read-model. These characterization tests
    /// therefore seed the player's points as a Points PlayerStatistic.
    /// </summary>
    private static async Task<PlayerStatistic> AddScorerAsync(ApplicationDBContext db, Guid playerId, Guid matchId, int points)
    {
        PlayerStatistic statistic = new()
        {
            PlayerId = playerId,
            MatchId = matchId,
            Value = points,
            Type = StatisticType.Points,
            CreatedBy = "test",
        };

        db.PlayersStatistics.Add(statistic);
        await db.SaveChangesAsync();

        return statistic;
    }
}
