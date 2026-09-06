using Application.Interfaces.Services;
using Application.Utils.Helper.Playoff;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Covers StageService.CommitDrawAsync's private re-draw guard
/// (EnsureBracketDrawableAsync): a bracket may be drawn or re-drawn only
/// while every real match of that Stage.DivisionId + Stage.BracketName is
/// unplayed, byes and empty slots never count as played, and parallel
/// brackets under different BracketName values lock independently.
/// </summary>
public class BracketRedrawGuardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BracketRedrawGuardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EnsureBracketDrawableAsync_NoPlayedMatches_Allowed()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAndRegisterAsync(db, tournament, division, 4);
        Stage stage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, stage);
        await SeedEmptyMatchAsync(db, stage);

        List<Match> committed = await stageService.CommitDrawAsync(
            stage.Id, DrawMode.Manual, manualOrder: [.. teams.Select(t => t.Id)]);

        Assert.Equal(2, committed.Count);
    }

    public static readonly TheoryData<bool, int?, int?, MatchStatus> PlayedTriggers = new()
    {
        { true, null, null, MatchStatus.Scheduled },
        { false, 10, 8, MatchStatus.Scheduled },
        { false, null, null, MatchStatus.Played },
    };

    [Theory]
    [MemberData(nameof(PlayedTriggers))]
    public async Task EnsureBracketDrawableAsync_OneMatchFinishedOrScored_Rejected(
        bool isFinished, int? homeScore, int? visitorScore, MatchStatus status)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAndRegisterAsync(db, tournament, division, 4);
        Stage stage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedRealMatchAsync(db, stage, teams[0], teams[1], isFinished, homeScore, visitorScore, status);
        await SeedEmptyMatchAsync(db, stage);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CommitDrawAsync(stage.Id, DrawMode.Manual, manualOrder: [.. teams.Select(t => t.Id)]));
    }

    [Fact]
    public async Task EnsureBracketDrawableAsync_ByeMatchesDoNotCountAsPlayed()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAndRegisterAsync(db, tournament, division, 3);
        Stage stage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, stage);
        await SeedEmptyMatchAsync(db, stage);

        List<Guid> firstOrder = [.. teams.Select(t => t.Id)];
        await stageService.CommitDrawAsync(stage.Id, DrawMode.Manual, manualOrder: firstOrder);

        List<Guid> reversedOrder = [.. firstOrder.AsEnumerable().Reverse()];
        List<Match> reDrawn = await stageService.CommitDrawAsync(stage.Id, DrawMode.Manual, manualOrder: reversedOrder);

        Assert.Equal(2, reDrawn.Count);
    }

    [Fact]
    public async Task EnsureBracketDrawableAsync_ParallelBracketsLockIndependently()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAndRegisterAsync(db, tournament, division, 4);

        Stage goldStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa de Oro", bestOf: 1);
        await SeedRealMatchAsync(db, goldStage, teams[0], teams[1], isFinished: true, homeScore: null, visitorScore: null, status: MatchStatus.Scheduled);
        await SeedEmptyMatchAsync(db, goldStage);

        Stage silverStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa de Plata", bestOf: 1);
        await SeedEmptyMatchAsync(db, silverStage);
        await SeedEmptyMatchAsync(db, silverStage);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CommitDrawAsync(goldStage.Id, DrawMode.Manual, manualOrder: [.. teams.Select(t => t.Id)]));

        List<Match> silverCommitted = await stageService.CommitDrawAsync(
            silverStage.Id, DrawMode.Manual, manualOrder: [.. teams.Select(t => t.Id)]);

        Assert.Equal(2, silverCommitted.Count);
    }

    [Fact]
    public async Task CommitDrawAsync_ReDraw_ResetsPriorSeedingAndSeries()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAndRegisterAsync(db, tournament, division, 4);
        Stage stage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 3);
        await SeedEmptyMatchAsync(db, stage);
        await SeedEmptyMatchAsync(db, stage);

        List<Guid> firstOrder = [teams[0].Id, teams[1].Id, teams[2].Id, teams[3].Id];
        await stageService.CommitDrawAsync(stage.Id, DrawMode.Manual, manualOrder: firstOrder);

        List<Guid> firstSeriesIds = await db.MatchSeries.Where(s => s.StageId == stage.Id).Select(s => s.Id).ToListAsync();
        Assert.Equal(2, firstSeriesIds.Count);

        List<Guid> secondOrder = [teams[3].Id, teams[2].Id, teams[1].Id, teams[0].Id];
        List<Match> reCommitted = await stageService.CommitDrawAsync(stage.Id, DrawMode.Manual, manualOrder: secondOrder);

        List<MatchSeries> secondSeries = await db.MatchSeries.Where(s => s.StageId == stage.Id).ToListAsync();
        Assert.Equal(2, secondSeries.Count);
        Assert.DoesNotContain(secondSeries, s => firstSeriesIds.Contains(s.Id));

        List<(Guid HomeTeamId, Guid? VisitorTeamId)> expectedPairs = PlayoffSeeder.SeedPairs(secondOrder);
        List<Match> ordered = [.. reCommitted.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];
        for (int i = 0; i < ordered.Count; i++)
        {
            Assert.Equal(expectedPairs[i].HomeTeamId, ordered[i].HomeTeamId);
            Assert.Equal(expectedPairs[i].VisitorTeamId, ordered[i].VisitorTeamId);
        }
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Bracket re-draw guard test tournament",
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

    private static async Task<List<Team>> SeedTeamsAndRegisterAsync(
        ApplicationDBContext db, Tournament tournament, Division division, int count)
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

        foreach (Team team in teams)
        {
            db.DivisionTeamRegistrations.Add(new DivisionTeamRegistration
            {
                TeamId = team.Id,
                DivisionId = division.Id,
                CreatedBy = "test",
            });
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

    private static async Task SeedRealMatchAsync(
        ApplicationDBContext db, Stage stage, Team home, Team visitor,
        bool isFinished, int? homeScore, int? visitorScore, MatchStatus status)
    {
        db.Matches.Add(new Match
        {
            StageId = stage.Id,
            Type = MatchType.Playoff,
            Slug = $"match-{Guid.NewGuid()}",
            MatchDate = stage.StartDate,
            HomeTeamId = home.Id,
            VisitorTeamId = visitor.Id,
            HomeScore = homeScore,
            VisitorScore = visitorScore,
            IsFinished = isFinished,
            Status = status,
            CreatedBy = "test",
        });

        await db.SaveChangesAsync();
    }
}
