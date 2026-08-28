using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Covers HU-110: a cross-division cup division with MORE THAN ONE internal
/// StageType.Group stage is seeded by pooling the top-QualifiersPerGroup teams
/// of each group and ordering them by group-stage strength before seeding the
/// bracket via the shared classic-seed/BYE path. A cross cup with exactly ONE
/// group (and every regular division) keeps behaving as before — see
/// <see cref="StageSeedingTests"/>.
/// </summary>
public class CrossCupMultiGroupSeedingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CrossCupMultiGroupSeedingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Division_QualifiersPerGroup_DefaultsToOne()
    {
        Division division = new()
        {
            Slug = "d",
            Name = "d",
            Tournament = null!,
            Stages = [],
            CreatedBy = "test",
        };

        Assert.Equal(1, division.QualifiersPerGroup);
    }

    /// <summary>
    /// Groups of 4, 4 and 3. Group A plays 100-70 (biggest margins), Group B
    /// 90-80, Group C 90-80. With K = 1 the pooled winners are a0 (Diff +90),
    /// b0 (Diff +30) and c0 (2 wins), ordered a0 > b0 > c0. Three seeds pad to
    /// a 4-team bracket: a0 gets the BYE, b0 plays c0.
    /// </summary>
    [Fact]
    public async Task SeedKnockoutStageAsync_MultiGroupCrossCup_PoolsTopOnePerGroupOrderedByStrength()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: true, qualifiersPerGroup: 1);

        List<Team> groupA = await SeedRoundRobinGroupAsync(db, division, tournament, size: 4, homeScore: 100, visitorScore: 70);
        List<Team> groupB = await SeedRoundRobinGroupAsync(db, division, tournament, size: 4, homeScore: 90, visitorScore: 80);
        List<Team> groupC = await SeedRoundRobinGroupAsync(db, division, tournament, size: 3, homeScore: 90, visitorScore: 80);

        Stage bracket = await SeedStageAsync(db, division, tournament, StageType.SemiFinal);
        await SeedEmptyMatchAsync(db, bracket);
        await SeedEmptyMatchAsync(db, bracket);

        List<Match> seeded = await stageService.SeedKnockoutStageAsync(bracket.Id);
        List<Match> ordered = [.. seeded.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];

        // Seed 1 (a0) gets the BYE.
        Assert.Equal(groupA[0].Id, ordered[0].HomeTeamId);
        Assert.Null(ordered[0].VisitorTeamId);
        Assert.True(ordered[0].IsFinished);
        Assert.Equal(groupA[0].Id, ordered[0].WinningTeamId);

        // Seed 2 (b0) vs seed 3 (c0).
        Assert.Equal(groupB[0].Id, ordered[1].HomeTeamId);
        Assert.Equal(groupC[0].Id, ordered[1].VisitorTeamId);
        Assert.False(ordered[1].IsFinished);
    }

    /// <summary>
    /// Two groups of 4 with K = 2. Group A margins are bigger, so the pooled
    /// order is a0 (Diff +90) > b0 (Diff +30) > a1 (4 pts, Diff +30) >
    /// b1 (4 pts, Diff +10). Four seeds fill a clean 4-team bracket:
    /// 1v4 (a0 vs b1) and 2v3 (b0 vs a1).
    /// </summary>
    [Fact]
    public async Task SeedKnockoutStageAsync_MultiGroupCrossCup_KTwo_PoolsTopTwoPerGroup()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: true, qualifiersPerGroup: 2);

        List<Team> groupA = await SeedRoundRobinGroupAsync(db, division, tournament, size: 4, homeScore: 100, visitorScore: 70);
        List<Team> groupB = await SeedRoundRobinGroupAsync(db, division, tournament, size: 4, homeScore: 90, visitorScore: 80);

        Stage bracket = await SeedStageAsync(db, division, tournament, StageType.SemiFinal);
        await SeedEmptyMatchAsync(db, bracket);
        await SeedEmptyMatchAsync(db, bracket);

        List<Match> seeded = await stageService.SeedKnockoutStageAsync(bracket.Id);
        List<Match> ordered = [.. seeded.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];

        // 1v4: a0 vs b1.
        Assert.Equal(groupA[0].Id, ordered[0].HomeTeamId);
        Assert.Equal(groupB[1].Id, ordered[0].VisitorTeamId);

        // 2v3: b0 vs a1.
        Assert.Equal(groupB[0].Id, ordered[1].HomeTeamId);
        Assert.Equal(groupA[1].Id, ordered[1].VisitorTeamId);
    }

    /// <summary>
    /// A cross cup with a SINGLE group must keep behaving exactly as today:
    /// it uses the teams assigned to the elimination stage and orders them by
    /// the (single) group's standings — never the multi-group pool.
    /// </summary>
    [Fact]
    public async Task SeedKnockoutStageAsync_SingleGroupCrossCup_BehavesLikeToday()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: true, qualifiersPerGroup: 1);

        List<Team> teams = await SeedRoundRobinGroupAsync(db, division, tournament, size: 4, homeScore: 90, visitorScore: 80);

        Stage bracket = await SeedStageAsync(db, division, tournament, StageType.SemiFinal);
        await SeedEmptyMatchAsync(db, bracket);
        await SeedEmptyMatchAsync(db, bracket);
        foreach (Team team in teams)
        {
            await AssignTeamToStageAsync(db, bracket, team);
        }

        List<Match> seeded = await stageService.SeedKnockoutStageAsync(bracket.Id);
        List<Match> ordered = [.. seeded.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];

        // Classic single-group seeding: 1v4, 2v3.
        Assert.Equal(teams[0].Id, ordered[0].HomeTeamId);
        Assert.Equal(teams[3].Id, ordered[0].VisitorTeamId);
        Assert.Equal(teams[1].Id, ordered[1].HomeTeamId);
        Assert.Equal(teams[2].Id, ordered[1].VisitorTeamId);
    }

    /// <summary>
    /// Multi-group cross cup where only one group has finished results yields a
    /// single pooled qualifier, which is below the two-team seeding floor.
    /// </summary>
    [Fact]
    public async Task SeedKnockoutStageAsync_MultiGroupCrossCup_FewerThanTwoQualifiers_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: true, qualifiersPerGroup: 1);

        await SeedRoundRobinGroupAsync(db, division, tournament, size: 3, homeScore: 90, visitorScore: 80);
        // Second group with teams but NO finished matches -> empty standings.
        await SeedEmptyGroupAsync(db, division, tournament, size: 3);

        Stage bracket = await SeedStageAsync(db, division, tournament, StageType.SemiFinal);
        await SeedEmptyMatchAsync(db, bracket);
        await SeedEmptyMatchAsync(db, bracket);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.SeedKnockoutStageAsync(bracket.Id));
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);
        Tournament tournament = new()
        {
            Description = "Cross-cup multi-group test tournament",
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

    private static async Task<Division> SeedDivisionAsync(
        ApplicationDBContext db, Tournament tournament, bool isCrossDivisionCup, int qualifiersPerGroup)
    {
        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            IsCrossDivisionCup = isCrossDivisionCup,
            QualifiersPerGroup = qualifiersPerGroup,
            Stages = [],
            CreatedBy = "test",
        };
        db.Divisions.Add(division);
        await db.SaveChangesAsync();
        return division;
    }

    /// <summary>
    /// Creates a group stage with <paramref name="size"/> teams and a full
    /// single round-robin where the lower-index team is always home and wins
    /// <paramref name="homeScore"/>-<paramref name="visitorScore"/>, producing
    /// the deterministic standings teams[0] &gt; teams[1] &gt; ... .
    /// </summary>
    private static async Task<List<Team>> SeedRoundRobinGroupAsync(
        ApplicationDBContext db, Division division, Tournament tournament, int size, int homeScore, int visitorScore)
    {
        List<Team> teams = await SeedTeamsAsync(db, tournament, size);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group);
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                await SeedFinishedMatchAsync(db, groupStage, teams[i], teams[j], homeScore, visitorScore);
            }
        }
        return teams;
    }

    private static async Task SeedEmptyGroupAsync(
        ApplicationDBContext db, Division division, Tournament tournament, int size)
    {
        List<Team> teams = await SeedTeamsAsync(db, tournament, size);
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group);
        foreach (Team team in teams)
        {
            await AssignTeamToStageAsync(db, groupStage, team);
        }
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

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db, Division division, Tournament tournament, StageType stageType)
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
