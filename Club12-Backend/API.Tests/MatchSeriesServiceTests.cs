using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Covers best-of-N playoff series: creation guardrails (teams must be
/// distinct, assigned to the stage, and not duplicated), game scheduling
/// limits (cannot exceed BestOf, cannot add to a decided series), and
/// winner determination once one team wins the majority of games.
/// </summary>
public class MatchSeriesServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MatchSeriesServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateSeriesAsync_ValidTeams_CopiesBestOfFromStage()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchSeriesService matchSeriesService = scope.ServiceProvider.GetRequiredService<IMatchSeriesService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedStageAsync(db, division, tournament, bestOf: 3);
        await AssignTeamToStageAsync(db, stage, teams[0]);
        await AssignTeamToStageAsync(db, stage, teams[1]);

        MatchSeries series = await matchSeriesService.CreateSeriesAsync(stage.Id, teams[0].Id, teams[1].Id);

        Assert.Equal(3, series.BestOf);
        Assert.Null(series.WinningTeamId);
    }

    [Fact]
    public async Task CreateSeriesAsync_SameTeamTwice_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchSeriesService matchSeriesService = scope.ServiceProvider.GetRequiredService<IMatchSeriesService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 1);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedStageAsync(db, division, tournament, bestOf: 1);
        await AssignTeamToStageAsync(db, stage, teams[0]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => matchSeriesService.CreateSeriesAsync(stage.Id, teams[0].Id, teams[0].Id));
    }

    [Fact]
    public async Task CreateSeriesAsync_TeamNotAssignedToStage_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchSeriesService matchSeriesService = scope.ServiceProvider.GetRequiredService<IMatchSeriesService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedStageAsync(db, division, tournament, bestOf: 1);
        await AssignTeamToStageAsync(db, stage, teams[0]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => matchSeriesService.CreateSeriesAsync(stage.Id, teams[0].Id, teams[1].Id));
    }

    [Fact]
    public async Task CreateSeriesAsync_DuplicatePairing_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchSeriesService matchSeriesService = scope.ServiceProvider.GetRequiredService<IMatchSeriesService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedStageAsync(db, division, tournament, bestOf: 1);
        await AssignTeamToStageAsync(db, stage, teams[0]);
        await AssignTeamToStageAsync(db, stage, teams[1]);

        await matchSeriesService.CreateSeriesAsync(stage.Id, teams[0].Id, teams[1].Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => matchSeriesService.CreateSeriesAsync(stage.Id, teams[1].Id, teams[0].Id));
    }

    [Fact]
    public async Task AddGameToSeriesAsync_AssignsSequentialGameNumbers()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchSeriesService matchSeriesService = scope.ServiceProvider.GetRequiredService<IMatchSeriesService>();

        MatchSeries series = await SeedDecidableSeriesAsync(db, matchSeriesService, bestOf: 3);

        Match firstGame = await matchSeriesService.AddGameToSeriesAsync(series.Id, DateTime.UtcNow.AddDays(1), null);
        Match secondGame = await matchSeriesService.AddGameToSeriesAsync(series.Id, DateTime.UtcNow.AddDays(2), null);

        Assert.Equal(1, firstGame.GameNumber);
        Assert.Equal(2, secondGame.GameNumber);
    }

    [Fact]
    public async Task AddGameToSeriesAsync_ExceedsBestOf_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchSeriesService matchSeriesService = scope.ServiceProvider.GetRequiredService<IMatchSeriesService>();

        MatchSeries series = await SeedDecidableSeriesAsync(db, matchSeriesService, bestOf: 1);

        await matchSeriesService.AddGameToSeriesAsync(series.Id, DateTime.UtcNow.AddDays(1), null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => matchSeriesService.AddGameToSeriesAsync(series.Id, DateTime.UtcNow.AddDays(2), null));
    }

    [Fact]
    public async Task RecalculateSeriesWinnerAsync_MajorityOfGamesWon_SetsWinner()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchSeriesService matchSeriesService = scope.ServiceProvider.GetRequiredService<IMatchSeriesService>();

        MatchSeries series = await SeedDecidableSeriesAsync(db, matchSeriesService, bestOf: 3);

        Match game1 = await matchSeriesService.AddGameToSeriesAsync(series.Id, DateTime.UtcNow.AddDays(1), null);
        Match game2 = await matchSeriesService.AddGameToSeriesAsync(series.Id, DateTime.UtcNow.AddDays(2), null);

        await FinishGameAsync(db, game1, series.HomeTeamId);
        await matchSeriesService.RecalculateSeriesWinnerAsync(series.Id);

        MatchSeries? afterOneWin = await matchSeriesService.GetSeriesByIdAsync(series.Id);
        Assert.Null(afterOneWin!.WinningTeamId);

        await FinishGameAsync(db, game2, series.HomeTeamId);
        await matchSeriesService.RecalculateSeriesWinnerAsync(series.Id);

        MatchSeries? afterTwoWins = await matchSeriesService.GetSeriesByIdAsync(series.Id);
        Assert.Equal(series.HomeTeamId, afterTwoWins!.WinningTeamId);
    }

    [Fact]
    public async Task AddGameToSeriesAsync_SeriesAlreadyDecided_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchSeriesService matchSeriesService = scope.ServiceProvider.GetRequiredService<IMatchSeriesService>();

        MatchSeries series = await SeedDecidableSeriesAsync(db, matchSeriesService, bestOf: 1);

        Match game = await matchSeriesService.AddGameToSeriesAsync(series.Id, DateTime.UtcNow.AddDays(1), null);
        await FinishGameAsync(db, game, series.HomeTeamId);
        await matchSeriesService.RecalculateSeriesWinnerAsync(series.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => matchSeriesService.AddGameToSeriesAsync(series.Id, DateTime.UtcNow.AddDays(2), null));
    }

    private static async Task<MatchSeries> SeedDecidableSeriesAsync(ApplicationDBContext db, IMatchSeriesService matchSeriesService, int bestOf)
    {
        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedStageAsync(db, division, tournament, bestOf);
        await AssignTeamToStageAsync(db, stage, teams[0]);
        await AssignTeamToStageAsync(db, stage, teams[1]);

        return await matchSeriesService.CreateSeriesAsync(stage.Id, teams[0].Id, teams[1].Id);
    }

    private static async Task FinishGameAsync(ApplicationDBContext db, Match game, Guid winningTeamId)
    {
        Match tracked = await db.Matches.FirstAsync(m => m.Id == game.Id);
        tracked.IsFinished = true;
        tracked.WinningTeamId = winningTeamId;
        await db.SaveChangesAsync();
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Series test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
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

    private static async Task<List<Team>> SeedTeamsAsync(ApplicationDBContext db, Tournament tournament, int count)
    {
        List<Team> teams = [];

        for (int i = 0; i < count; i++)
        {
            Team team = new()
            {
                Name = $"Team-{Guid.NewGuid()}",
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

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db, Division division, Tournament tournament, int bestOf)
    {
        Stage stage = new()
        {
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.SemiFinal,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(7),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            BestOf = bestOf,
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
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
}
