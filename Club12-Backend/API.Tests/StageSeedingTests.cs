using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Covers StageService.SeedKnockoutStageAsync: pairing an elimination
/// stage's matches from group-stage standings in classic bracket seed
/// order, and the guardrails around it (stage must have matches, must not
/// already be seeded, every assigned team must have a position).
/// </summary>
public class StageSeedingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StageSeedingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Seeds a 6-match round robin among 4 teams that produces the standings
    /// A(3-0) > B(2-1) > C(1-2) > D(0-3).
    /// </summary>
    [Fact]
    public async Task SeedKnockoutStageAsync_FourTeams_PairsBestSeedAgainstWorst()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);

        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group);
        await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[1], 90, 80);
        await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[2], 90, 80);
        await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[3], 90, 80);
        await SeedFinishedMatchAsync(db, groupStage, teams[1], teams[2], 90, 80);
        await SeedFinishedMatchAsync(db, groupStage, teams[1], teams[3], 90, 80);
        await SeedFinishedMatchAsync(db, groupStage, teams[2], teams[3], 90, 80);

        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        foreach (Team team in teams)
        {
            await AssignTeamToStageAsync(db, semiFinalStage, team);
        }

        List<Match> seeded = await stageService.SeedKnockoutStageAsync(semiFinalStage.Id);

        List<Match> ordered = [.. seeded.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];
        Assert.Equal(teams[0].Id, ordered[0].HomeTeamId);
        Assert.Equal(teams[3].Id, ordered[0].VisitorTeamId);
        Assert.Equal(teams[1].Id, ordered[1].HomeTeamId);
        Assert.Equal(teams[2].Id, ordered[1].VisitorTeamId);
    }

    /// <summary>
    /// Seeds a 3-match round robin among 3 teams that produces the standings
    /// A(2-0) > B(1-1) > C(0-2).
    /// </summary>
    [Fact]
    public async Task SeedKnockoutStageAsync_ThreeTeams_BestSeedGetsAByeAndIsMarkedAsAWalkoverWin()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 3);

        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group);
        await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[1], 90, 80);
        await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[2], 90, 80);
        await SeedFinishedMatchAsync(db, groupStage, teams[1], teams[2], 90, 80);

        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        foreach (Team team in teams)
        {
            await AssignTeamToStageAsync(db, semiFinalStage, team);
        }

        List<Match> seeded = await stageService.SeedKnockoutStageAsync(semiFinalStage.Id);
        List<Match> ordered = [.. seeded.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];

        Match byeMatch = ordered[0];
        Assert.Equal(teams[0].Id, byeMatch.HomeTeamId);
        Assert.Null(byeMatch.VisitorTeamId);
        Assert.True(byeMatch.IsFinished);
        Assert.Equal(teams[0].Id, byeMatch.WinningTeamId);

        Match realMatch = ordered[1];
        Assert.Equal(teams[1].Id, realMatch.HomeTeamId);
        Assert.Equal(teams[2].Id, realMatch.VisitorTeamId);
        Assert.False(realMatch.IsFinished);
    }

    [Fact]
    public async Task SeedKnockoutStageAsync_NoMatchesGeneratedYet_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.SeedKnockoutStageAsync(semiFinalStage.Id));
    }

    [Fact]
    public async Task SeedKnockoutStageAsync_AlreadySeeded_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal);
        await SeedFinishedMatchAsync(db, semiFinalStage, teams[0], teams[1], 90, 80);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.SeedKnockoutStageAsync(semiFinalStage.Id));
    }

    [Fact]
    public async Task SeedKnockoutStageAsync_AssignedTeamMissingStandings_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);

        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await AssignTeamToStageAsync(db, semiFinalStage, teams[0]);
        await AssignTeamToStageAsync(db, semiFinalStage, teams[1]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.SeedKnockoutStageAsync(semiFinalStage.Id));
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Seeding test tournament",
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

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db, Division division, Tournament tournament, StageType stageType)
    {
        Stage stage = new()
        {
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = stageType,
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

    private static async Task SeedFinishedMatchAsync(ApplicationDBContext db, Stage stage, Team home, Team visitor, int homeScore, int visitorScore)
    {
        Match match = new()
        {
            StageId = stage.Id,
            Type = MatchType.Regular,
            MatchDate = stage.StartDate,
            HomeTeamId = home.Id,
            VisitorTeamId = visitor.Id,
            HomeScore = homeScore,
            VisitorScore = visitorScore,
            IsFinished = true,
            WinningTeamId = homeScore > visitorScore ? home.Id : visitor.Id,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();
    }

    private static async Task SeedEmptyMatchAsync(ApplicationDBContext db, Stage stage)
    {
        Match match = new()
        {
            StageId = stage.Id,
            Type = MatchType.Playoff,
            MatchDate = stage.StartDate.AddMinutes(db.Matches.Count(m => m.StageId == stage.Id)),
            IsFinished = false,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();
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
