using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Covers StageService.SeedPlayoffCupsAsync (HU-45/HU-81): seeding multiple
/// playoff cups from a division's final standings using its position-range
/// mapping, so the right positions land in the right cup's bracket.
/// </summary>
public class StagePlayoffCupSeedingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StagePlayoffCupSeedingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedPlayoffCupsAsync_TwoCups_PlacesTopRangeInGoldAndNextInSilver()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);

        // 8 teams. A ladder round-robin where the lower index always wins
        // yields the exact standings teams[0] > teams[1] > ... > teams[7].
        List<Team> teams = await SeedTeamsAsync(db, tournament, 8);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null);
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                await SeedFinishedMatchAsync(db, groupStage, teams[i], teams[j], 90, 80);
            }
        }

        await SeedMappingAsync(db, division, 1, 4, "Copa Oro");
        await SeedMappingAsync(db, division, 5, 8, "Copa Plata");

        Stage goldStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Oro");
        await SeedEmptyMatchAsync(db, goldStage);
        await SeedEmptyMatchAsync(db, goldStage);

        Stage silverStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Plata");
        await SeedEmptyMatchAsync(db, silverStage);
        await SeedEmptyMatchAsync(db, silverStage);

        Dictionary<string, List<Match>> seeded = await stageService.SeedPlayoffCupsAsync(division.Id);

        List<Match> gold = [.. seeded["Copa Oro"].OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];
        Assert.Equal(teams[0].Id, gold[0].HomeTeamId);
        Assert.Equal(teams[3].Id, gold[0].VisitorTeamId);
        Assert.Equal(teams[1].Id, gold[1].HomeTeamId);
        Assert.Equal(teams[2].Id, gold[1].VisitorTeamId);

        List<Match> silver = [.. seeded["Copa Plata"].OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];
        Assert.Equal(teams[4].Id, silver[0].HomeTeamId);
        Assert.Equal(teams[7].Id, silver[0].VisitorTeamId);
        Assert.Equal(teams[5].Id, silver[1].HomeTeamId);
        Assert.Equal(teams[6].Id, silver[1].VisitorTeamId);
    }

    [Fact]
    public async Task SeedPlayoffCupsAsync_NoMappings_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.SeedPlayoffCupsAsync(division.Id));
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);
        Tournament tournament = new()
        {
            Description = "Cup seeding test tournament",
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

    private static async Task SeedEmptyMatchAsync(ApplicationDBContext db, Stage stage)
    {
        db.Matches.Add(new Match
        {
            StageId = stage.Id,
            Type = MatchType.Playoff,
            Slug = $"match-{Guid.NewGuid()}",
            MatchDate = stage.StartDate.AddMinutes(db.Matches.Count(m => m.StageId == stage.Id)),
            IsFinished = false,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();
    }
}
