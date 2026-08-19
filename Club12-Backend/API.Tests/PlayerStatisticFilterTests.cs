using Application.DTOs.PlayerStatistic.Request;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Covers GetPlayerStatisticsAsync's TeamId filter (joining through the
/// statistic's player) and that the response carries the match date
/// without a separate lookup.
/// </summary>
public class PlayerStatisticFilterTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayerStatisticFilterTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPlayerStatisticsAsync_FilterByTeamId_ReturnsOnlyThatTeamsPlayerStatistics()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerStatisticService playerStatisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Team teamA = await SeedTeamAsync(db, tournament);
        Team teamB = await SeedTeamAsync(db, tournament);
        Player playerA = await SeedPlayerAsync(db, teamA);
        Player playerB = await SeedPlayerAsync(db, teamB);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedStageAsync(db, division, tournament);
        Match match = await SeedMatchAsync(db, stage, teamA, teamB);

        db.PlayersStatistics.Add(new PlayerStatistic { Value = 20, Type = StatisticType.Points, MatchId = match.Id, PlayerId = playerA.Id, CreatedBy = "test" });
        db.PlayersStatistics.Add(new PlayerStatistic { Value = 15, Type = StatisticType.Points, MatchId = match.Id, PlayerId = playerB.Id, CreatedBy = "test" });
        await db.SaveChangesAsync();

        Application.DTOs.Abstract.Response.PaginatedResponse<PlayerStatistic> result =
            await playerStatisticService.GetPlayerStatisticsAsync(new GetPlayerStatisticsFilteredRequest { TeamId = teamA.Id });

        PlayerStatistic onlyResult = Assert.Single(result.Items);
        Assert.Equal(playerA.Id, onlyResult.PlayerId);
        Assert.Equal(match.MatchDate, onlyResult.Match!.MatchDate);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "PlayerStatistic filter test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            MaxTeams = 32,
            MinTeams = 2,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, Tournament tournament)
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
        await db.SaveChangesAsync();

        return team;
    }

    private static async Task<Player> SeedPlayerAsync(ApplicationDBContext db, Team team)
    {
        Player player = new()
        {
            FirstName = "Test",
            LastName = $"Player-{Guid.NewGuid()}",
            DocumentNumber = Guid.NewGuid().ToString("N")[..8],
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "N/A",
            Team = team,
            TeamId = team.Id,
            CreatedBy = "test",
        };

        db.Players.Add(player);
        await db.SaveChangesAsync();

        return player;
    }

    private static async Task<Division> SeedDivisionAsync(ApplicationDBContext db, Tournament tournament)
    {
        Division division = new()
        {
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

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db, Division division, Tournament tournament)
    {
        Stage stage = new()
        {
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(7),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
    }

    private static async Task<Match> SeedMatchAsync(ApplicationDBContext db, Stage stage, Team home, Team visitor)
    {
        Match match = new()
        {
            StageId = stage.Id,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            MatchDate = stage.StartDate,
            HomeTeamId = home.Id,
            VisitorTeamId = visitor.Id,
            IsFinished = false,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return match;
    }
}
