using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Verifies IDivisionService.GetPositionsByDivisionIdAsync actually computes
/// standings from real Group-stage match results — the gap that previously
/// left DivisionResponse.Positions always null/empty, since no code path
/// ever populated it.
/// </summary>
public class DivisionStandingsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DivisionStandingsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPositionsByDivisionIdAsync_FinishedGroupStageMatches_ReturnsComputedStandings()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage groupStage = await SeedGroupStageAsync(db, division, tournament);

        await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[1], homeScore: 90, visitorScore: 80);

        List<Position> positions = await divisionService.GetPositionsByDivisionIdAsync(division.Id);

        Assert.Equal(2, positions.Count);
        Position winner = Assert.Single(positions, p => p.TeamId == teams[0].Id);
        Assert.Equal(1, winner.Wins);
        Assert.Equal(2, winner.Points);
        Position loser = Assert.Single(positions, p => p.TeamId == teams[1].Id);
        Assert.Equal(1, loser.Losses);
        Assert.Equal(1, loser.Points);
    }

    [Fact]
    public async Task GetPositionsByDivisionIdAsync_ElimintationStageMatchesOnly_AreExcludedFromStandings()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage finalStage = await SeedEliminationStageAsync(db, division, tournament, StageType.Final);

        await SeedFinishedMatchAsync(db, finalStage, teams[0], teams[1], homeScore: 90, visitorScore: 80);

        List<Position> positions = await divisionService.GetPositionsByDivisionIdAsync(division.Id);

        Assert.Empty(positions);
    }

    [Fact]
    public async Task GetPositionsByDivisionIdAsync_NoGroupStage_ReturnsEmptyList()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);

        List<Position> positions = await divisionService.GetPositionsByDivisionIdAsync(division.Id);

        Assert.Empty(positions);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Standings test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            MaxTeams = 8,
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
            Stages = [],
            CreatedBy = "test",
        };

        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        return division;
    }

    private static async Task<Stage> SeedGroupStageAsync(ApplicationDBContext db, Division division, Tournament tournament)
    {
        return await SeedEliminationStageAsync(db, division, tournament, StageType.Group, isElimination: false);
    }

    private static async Task<Stage> SeedEliminationStageAsync(
        ApplicationDBContext db, Division division, Tournament tournament, StageType stageType, bool isElimination = true)
    {
        Stage stage = new()
        {
            Name = $"{stageType}-{Guid.NewGuid()}",
            StageType = stageType,
            IsActive = true,
            IsElimination = isElimination,
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

    private static async Task SeedFinishedMatchAsync(
        ApplicationDBContext db, Stage stage, Team home, Team visitor, int homeScore, int visitorScore)
    {
        Match match = new()
        {
            MatchDate = stage.StartDate,
            Type = MatchType.Regular,
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            HomeScore = homeScore,
            VisitorScore = visitorScore,
            IsFinished = true,
            WinningTeam = homeScore > visitorScore ? home : visitor,
            WinningTeamId = homeScore > visitorScore ? home.Id : visitor.Id,
            Stage = stage,
            StageId = stage.Id,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();
    }
}
