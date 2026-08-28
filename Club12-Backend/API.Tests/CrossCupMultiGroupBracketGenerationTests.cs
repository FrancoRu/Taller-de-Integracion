using Application.Interfaces.Services;
using Application.Utils.Constants.Stage;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// End-to-end backend proof for HU-110 (wave B): a cross-division cup with
/// SEVERAL internal <see cref="StageType.Group"/> stages can be built and run
/// entirely through the real services on the real (SQLite) database — create
/// the group stages (guard relaxation), assign teams, generate + finish each
/// round-robin, then generate the bracket's first-round matches (dynamic
/// sizing) and seed them from the pooled top-K qualifiers.
///
/// Regression: a regular division and a single-group cross cup keep the fixed
/// per-stage-type bracket size, so only the multi-group cross cup is resized.
/// </summary>
public class CrossCupMultiGroupBracketGenerationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CrossCupMultiGroupBracketGenerationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Groups of 4, 4 and 3 with K = 1. Pooled winners a0 (Diff +120), b0
    /// (Diff +60) and c0 (4 pts) order a0 &gt; b0 &gt; c0. Three seeds pad to a
    /// 4-team bracket, so the whole pipeline must generate exactly 2 first-round
    /// matches and seed a0 into the BYE, b0 vs c0 into the other.
    /// </summary>
    [Fact]
    public async Task CrossCupMultiGroup_KOne_GeneratesTwoBracketMatchesAndSeedsPooledQualifiers()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedCrossCupDivisionAsync(db, tournament, qualifiersPerGroup: 1);

        List<Team> groupA = await BuildAndPlayGroupAsync(db, stageService, matchService, division, tournament, size: 4, winnerScore: 100, loserScore: 60);
        List<Team> groupB = await BuildAndPlayGroupAsync(db, stageService, matchService, division, tournament, size: 4, winnerScore: 100, loserScore: 80);
        List<Team> groupC = await BuildAndPlayGroupAsync(db, stageService, matchService, division, tournament, size: 3, winnerScore: 90, loserScore: 85);

        Stage bracket = await CreateBracketStageAsync(stageService, division, tournament);

        List<Match> generated = await matchService.CreateAutomatedMatchesAsync(bracket.Id);

        // Dynamic sizing: 3 pooled qualifiers -> next power of two (4) / 2 = 2.
        Assert.Equal(2, generated.Count);

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
    /// Groups of 4, 4 and 3 with K = 2 pool six qualifiers, so the bracket must
    /// grow to 8 slots = 4 first-round matches — four more than a plain
    /// <see cref="StageType.SemiFinal"/>'s fixed two — proving the bracket is
    /// sized from the pooled qualifiers, not the stage type. The two top seeds
    /// (a0, b0) receive the two byes.
    /// </summary>
    [Fact]
    public async Task CrossCupMultiGroup_KTwo_SizesBracketFromPooledQualifiers_NotStageType()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedCrossCupDivisionAsync(db, tournament, qualifiersPerGroup: 2);

        List<Team> groupA = await BuildAndPlayGroupAsync(db, stageService, matchService, division, tournament, size: 4, winnerScore: 100, loserScore: 60);
        List<Team> groupB = await BuildAndPlayGroupAsync(db, stageService, matchService, division, tournament, size: 4, winnerScore: 100, loserScore: 80);
        List<Team> groupC = await BuildAndPlayGroupAsync(db, stageService, matchService, division, tournament, size: 3, winnerScore: 90, loserScore: 85);

        Stage bracket = await CreateBracketStageAsync(stageService, division, tournament);

        List<Match> generated = await matchService.CreateAutomatedMatchesAsync(bracket.Id);

        // 6 pooled qualifiers -> next power of two (8) / 2 = 4, not SemiFinal's fixed 2.
        Assert.Equal(4, generated.Count);
        Assert.NotEqual(KnockoutMatchCount.SEMI_FINAL, generated.Count);

        List<Match> seeded = await stageService.SeedKnockoutStageAsync(bracket.Id);
        List<Match> ordered = [.. seeded.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];

        // Order: a0, b0, a1, b1, c0, c1. Byes to the top two seeds.
        // Bracket order [1,8,4,5,2,7,3,6] -> (a0,BYE)(b1,c0)(b0,BYE)(a1,c1).
        Assert.Equal(groupA[0].Id, ordered[0].HomeTeamId);
        Assert.Null(ordered[0].VisitorTeamId);

        Assert.Equal(groupB[1].Id, ordered[1].HomeTeamId);
        Assert.Equal(groupC[0].Id, ordered[1].VisitorTeamId);

        Assert.Equal(groupB[0].Id, ordered[2].HomeTeamId);
        Assert.Null(ordered[2].VisitorTeamId);

        Assert.Equal(groupA[1].Id, ordered[3].HomeTeamId);
        Assert.Equal(groupC[1].Id, ordered[3].VisitorTeamId);

        // Exactly the two top seeds walk over.
        List<Match> byes = [.. ordered.Where(m => m.VisitorTeamId is null)];
        Assert.Equal(2, byes.Count);
        Assert.Contains(byes, m => m.HomeTeamId == groupA[0].Id);
        Assert.Contains(byes, m => m.HomeTeamId == groupB[0].Id);
    }

    /// <summary>
    /// Regression: a regular (non-cross-cup) division keeps the fixed
    /// per-stage-type bracket size — a SemiFinal still generates exactly two
    /// first-round matches, untouched by the multi-group sizing branch.
    /// </summary>
    [Fact]
    public async Task RegularDivision_KeepsFixedSemiFinalBracketSize()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedCrossCupDivisionAsync(db, tournament, qualifiersPerGroup: 1, isCrossDivisionCup: false);

        // A single regular group is enough; the sizing branch is only entered
        // for a cross cup with more than one group.
        await BuildAndPlayGroupAsync(db, stageService, matchService, division, tournament, size: 4, winnerScore: 90, loserScore: 80);

        Stage bracket = await CreateBracketStageAsync(stageService, division, tournament);

        List<Match> generated = await matchService.CreateAutomatedMatchesAsync(bracket.Id);

        Assert.Equal(KnockoutMatchCount.SEMI_FINAL, generated.Count);
    }

    /// <summary>
    /// Regression: a cross cup with a SINGLE group is not a multi-group cup, so
    /// it keeps the fixed per-stage-type bracket size too.
    /// </summary>
    [Fact]
    public async Task SingleGroupCrossCup_KeepsFixedSemiFinalBracketSize()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedCrossCupDivisionAsync(db, tournament, qualifiersPerGroup: 2);

        await BuildAndPlayGroupAsync(db, stageService, matchService, division, tournament, size: 4, winnerScore: 90, loserScore: 80);

        Stage bracket = await CreateBracketStageAsync(stageService, division, tournament);

        List<Match> generated = await matchService.CreateAutomatedMatchesAsync(bracket.Id);

        Assert.Equal(KnockoutMatchCount.SEMI_FINAL, generated.Count);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);
        Tournament tournament = new()
        {
            Description = "Cross-cup multi-group bracket-generation test",
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

    private static async Task<Division> SeedCrossCupDivisionAsync(
        ApplicationDBContext db, Tournament tournament, int qualifiersPerGroup, bool isCrossDivisionCup = true)
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
    /// Creates a Group stage through the real service (exercising the relaxed
    /// multi-group guard), assigns <paramref name="size"/> fresh teams, generates
    /// its round-robin fixture, then finishes every match so the lower-ordered
    /// team of each pair wins by <paramref name="winnerScore"/>-<paramref
    /// name="loserScore"/>. Returns the teams best-first, so <c>teams[0]</c> wins
    /// the group.
    /// </summary>
    private static async Task<List<Team>> BuildAndPlayGroupAsync(
        ApplicationDBContext db,
        IStageService stageService,
        IMatchService matchService,
        Division division,
        Tournament tournament,
        int size,
        int winnerScore,
        int loserScore)
    {
        List<Team> teams = await SeedTeamsAsync(db, tournament, size);

        Stage groupStage = await stageService.CreateStageAsync(new Stage
        {
            Slug = string.Empty,
            Name = $"Grupo-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(14),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        });

        await stageService.AssignTeamsToStageAsync(groupStage, [.. teams.Select(t => t.Id)]);

        await matchService.CreateAutomatedMatchesAsync(groupStage.Id);

        Dictionary<Guid, int> rankByTeamId = teams
            .Select((team, index) => (team.Id, index))
            .ToDictionary(pair => pair.Id, pair => pair.index);

        List<Match> groupMatches = [.. await db.Matches.Where(m => m.StageId == groupStage.Id).ToListAsync()];

        foreach (Match match in groupMatches)
        {
            bool homeWins = rankByTeamId[match.HomeTeamId!.Value] < rankByTeamId[match.VisitorTeamId!.Value];

            match.HomeScore = homeWins ? winnerScore : loserScore;
            match.VisitorScore = homeWins ? loserScore : winnerScore;
            match.IsFinished = true;
            match.WinningTeamId = homeWins ? match.HomeTeamId : match.VisitorTeamId;
        }

        await db.SaveChangesAsync();

        return teams;
    }

    private static async Task<Stage> CreateBracketStageAsync(IStageService stageService, Division division, Tournament tournament)
    {
        return await stageService.CreateStageAsync(new Stage
        {
            Slug = string.Empty,
            Name = $"Semifinal-{Guid.NewGuid()}",
            StageType = StageType.SemiFinal,
            IsActive = true,
            StartDate = tournament.StartDate.AddDays(20),
            EndDate = tournament.StartDate.AddDays(27),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        });
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
}
