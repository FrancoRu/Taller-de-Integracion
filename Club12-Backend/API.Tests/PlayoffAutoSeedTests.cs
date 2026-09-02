using Application.DTOs.PlayerStatistic.Request;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Covers StageService.TryAutoSeedPlayoffPhaseAsync: once the LAST match of a
/// division's group phase finishes, its position-range playoff cups
/// (HU-45/HU-81) should seed themselves automatically — the admin no longer
/// has to click "Sembrar bracket" by hand.
/// </summary>
public class PlayoffAutoSeedTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayoffAutoSeedTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FinishingTheLastGroupMatch_AutoSeedsThePlayoffCup()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null);

        // Single round-robin among 4 teams: 6 matches. The lower index always
        // wins, so the final standings are teams[0] > teams[1] > teams[2] > teams[3].
        Match? lastMatch = null;
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                // Leave exactly one pairing (the last one) unfinished — the
                // trigger under test.
                if (i == teams.Count - 2 && j == teams.Count - 1)
                {
                    lastMatch = await SeedScheduledMatchAsync(db, groupStage, teams[i], teams[j]);
                }
                else
                {
                    await SeedFinishedMatchAsync(db, groupStage, teams[i], teams[j], 90, 80);
                }
            }
        }

        await SeedMappingAsync(db, division, 1, 4, "Copa Única");
        Stage cupStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única");
        await SeedEmptyMatchAsync(db, cupStage);
        await SeedEmptyMatchAsync(db, cupStage);

        Assert.NotNull(lastMatch);
        Match? updated = await matchService.LoadMatchResultAsync(lastMatch!.Id, 90, 80);
        Assert.NotNull(updated);
        Assert.True(updated!.IsFinished);

        List<Match> cupMatches = await db.Matches
            .Where(m => m.StageId == cupStage.Id)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();

        Assert.All(cupMatches, m => Assert.True(m.HomeTeamId.HasValue));
    }

    [Fact]
    public async Task FinishingAMidGroupMatch_DoesNotAutoSeed()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null);

        Match scheduled = await SeedScheduledMatchAsync(db, groupStage, teams[0], teams[1]);
        // The rest of the group phase is still scheduled/unplayed.
        await SeedScheduledMatchAsync(db, groupStage, teams[2], teams[3]);

        await SeedMappingAsync(db, division, 1, 4, "Copa Única");
        Stage cupStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única");
        await SeedEmptyMatchAsync(db, cupStage);
        await SeedEmptyMatchAsync(db, cupStage);

        await matchService.LoadMatchResultAsync(scheduled.Id, 90, 80);

        List<Match> cupMatches = await db.Matches
            .Where(m => m.StageId == cupStage.Id)
            .ToListAsync();

        Assert.All(cupMatches, m => Assert.False(m.HomeTeamId.HasValue));
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);
        Tournament tournament = new()
        {
            Description = "Auto-seed test tournament",
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

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db, Division division, Tournament tournament, StageType stageType, string? bracketName)
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
            CreatedBy = "test",
        };
        db.Stages.Add(stage);
        await db.SaveChangesAsync();
        return stage;
    }

    private static async Task SeedFinishedMatchAsync(ApplicationDBContext db, Stage stage, Team home, Team visitor, int homeScore, int visitorScore)
    {
        db.Matches.Add(new Match
        {
            StageId = stage.Id,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            MatchDate = stage.StartDate,
            HomeTeamId = home.Id,
            VisitorTeamId = visitor.Id,
            HomeScore = homeScore,
            VisitorScore = visitorScore,
            IsFinished = true,
            WinningTeamId = homeScore > visitorScore ? home.Id : visitor.Id,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();
    }

    private static async Task<Match> SeedScheduledMatchAsync(ApplicationDBContext db, Stage stage, Team home, Team visitor)
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
