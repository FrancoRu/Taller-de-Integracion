using Application.DTOs.PlayerStatistic.Request;
using Application.DTOs.Scorer.Request;
using Application.DTOs.Scorer.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Behavioural tests for the MATCH RESULT / GOLEADORES batch:
/// HU-69 (match status), HU-70 (no draws — a played match needs a winner),
/// HU-71 (planilla coherence: players' points must equal the team score, and
/// players must be eligible roster members), HU-72 (the ranking reflects the
/// real loaded points), and HU-73 (walkover). Exercised through the real
/// services/repositories against the SQLite-backed integration host.
/// </summary>
public class MatchResultAndSheetTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MatchResultAndSheetTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------- HU-70: no draws ----------

    [Fact]
    public async Task LoadMatchResultAsync_GroupStageTie_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => matchService.LoadMatchResultAsync(seeded.Match.Id, homeScore: 70, visitorScore: 70));

        Assert.Contains("no puede terminar empatado", ex.Message);

        // Nothing persisted: the match stays scheduled and unfinished.
        Match reloaded = await ReloadMatchAsync(db, seeded.Match.Id);
        Assert.False(reloaded.IsFinished);
        Assert.Equal(MatchStatus.Scheduled, reloaded.Status);
    }

    [Fact]
    public async Task LoadMatchResultAsync_PlayoffTie_IsRejectedRequiringAWinner()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Final);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => matchService.LoadMatchResultAsync(seeded.Match.Id, homeScore: 88, visitorScore: 88));

        Assert.Contains("tiempo suplementario", ex.Message);
    }

    // ---------- HU-69: status transitions + winner (no silent visitor) ----------

    [Fact]
    public async Task LoadMatchResultAsync_DecisiveHomeWin_MarksPlayedAndPicksHomeWinner()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);

        Match? updated = await matchService.LoadMatchResultAsync(seeded.Match.Id, homeScore: 91, visitorScore: 80);

        Assert.NotNull(updated);
        Assert.True(updated!.IsFinished);
        Assert.Equal(MatchStatus.Played, updated.Status);
        // The winner is the higher-scoring team, NOT a silent visitor default.
        Assert.Equal(seeded.HomeTeam.Id, updated.WinningTeamId);

        Match reloaded = await ReloadMatchAsync(db, seeded.Match.Id);
        Assert.Equal(MatchStatus.Played, reloaded.Status);
        Assert.Equal(seeded.HomeTeam.Id, reloaded.WinningTeamId);
        Assert.Equal(91, reloaded.HomeScore);
    }

    [Fact]
    public async Task LoadMatchResultAsync_NonExistentMatch_ReturnsNull()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Match? updated = await matchService.LoadMatchResultAsync(Guid.NewGuid(), 10, 20);

        Assert.Null(updated);
    }

    // ---------- HU-73: walkover ----------

    [Fact]
    public async Task LoadWalkOverAsync_AwardsRegulationDefaultAndCountsInStandings()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);

        Match? updated = await matchService.LoadWalkOverAsync(
            seeded.Match.Id, presentTeamId: seeded.VisitorTeam.Id, presentTeamScore: null);

        Assert.NotNull(updated);
        Assert.Equal(MatchStatus.WalkOver, updated!.Status);
        Assert.True(updated.IsFinished);
        Assert.Equal(seeded.VisitorTeam.Id, updated.WinningTeamId);
        Assert.Equal(20, updated.VisitorScore);
        Assert.Equal(0, updated.HomeScore);

        // The walkover impacts standings just like a normal finished result.
        List<Position> positions = await divisionService.GetPositionsByDivisionIdAsync(seeded.DivisionId);
        Position winner = Assert.Single(positions, p => p.TeamId == seeded.VisitorTeam.Id);
        Assert.Equal(1, winner.Wins);
        Assert.Equal(2, winner.Points);
    }

    [Fact]
    public async Task LoadWalkOverAsync_TeamNotInMatch_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => matchService.LoadWalkOverAsync(seeded.Match.Id, presentTeamId: Guid.NewGuid(), presentTeamScore: null));
    }

    // ---------- HU-71 + HU-72: coherent sheet feeds the ranking ----------

    [Fact]
    public async Task LoadTeamMatchSheetAsync_SumEqualsScore_PersistsAndRankingReflectsPoints()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);
        Player scorerA = await SeedRosterPlayerAsync(db, seeded.HomeTeam, seeded.TournamentId, "Alpha");
        Player scorerB = await SeedRosterPlayerAsync(db, seeded.HomeTeam, seeded.TournamentId, "Bravo");

        await matchService.LoadMatchResultAsync(seeded.Match.Id, homeScore: 90, visitorScore: 80);

        LoadMatchSheetRequest request = new()
        {
            MatchId = seeded.Match.Id,
            TeamId = seeded.HomeTeam.Id,
            Scores =
            [
                new PlayerScoreEntry { PlayerId = scorerA.Id, Points = 55 },
                new PlayerScoreEntry { PlayerId = scorerB.Id, Points = 35 },
            ],
        };

        List<PlayerStatistic> created = await statisticService.LoadTeamMatchSheetAsync(request);
        Assert.Equal(2, created.Count);

        (IEnumerable<ScorerByPlayerResponse> items, _) =
            await scorerRepository.GetPlayerScoresAsync(new GetScorerFilteredRequest { MatchId = seeded.Match.Id });

        List<ScorerByPlayerResponse> ranking = [.. items];
        Assert.Equal(55, Assert.Single(ranking, r => r.PlayerId == scorerA.Id).Points);
        Assert.Equal(35, Assert.Single(ranking, r => r.PlayerId == scorerB.Id).Points);
    }

    [Fact]
    public async Task LoadTeamMatchSheetAsync_Reload_ReplacesPreviousSheet()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);
        Player scorer = await SeedRosterPlayerAsync(db, seeded.HomeTeam, seeded.TournamentId, "Solo");

        await matchService.LoadMatchResultAsync(seeded.Match.Id, homeScore: 40, visitorScore: 30);

        await statisticService.LoadTeamMatchSheetAsync(new LoadMatchSheetRequest
        {
            MatchId = seeded.Match.Id,
            TeamId = seeded.HomeTeam.Id,
            Scores = [new PlayerScoreEntry { PlayerId = scorer.Id, Points = 40 }],
        });

        // Correct the score, then reload the sheet — the previous rows are replaced.
        await matchService.LoadMatchResultAsync(seeded.Match.Id, homeScore: 44, visitorScore: 30);
        await statisticService.LoadTeamMatchSheetAsync(new LoadMatchSheetRequest
        {
            MatchId = seeded.Match.Id,
            TeamId = seeded.HomeTeam.Id,
            Scores = [new PlayerScoreEntry { PlayerId = scorer.Id, Points = 44 }],
        });

        (IEnumerable<ScorerByPlayerResponse> items, _) =
            await scorerRepository.GetPlayerScoresAsync(new GetScorerFilteredRequest { MatchId = seeded.Match.Id });

        Assert.Equal(44, Assert.Single(items, r => r.PlayerId == scorer.Id).Points);
    }

    [Fact]
    public async Task LoadTeamMatchSheetAsync_SumDiffersFromScore_IsRejectedWithDifference()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);
        Player scorer = await SeedRosterPlayerAsync(db, seeded.HomeTeam, seeded.TournamentId, "Alpha");

        await matchService.LoadMatchResultAsync(seeded.Match.Id, homeScore: 90, visitorScore: 80);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => statisticService.LoadTeamMatchSheetAsync(new LoadMatchSheetRequest
            {
                MatchId = seeded.Match.Id,
                TeamId = seeded.HomeTeam.Id,
                Scores = [new PlayerScoreEntry { PlayerId = scorer.Id, Points = 85 }],
            }));

        // 90 (team score) - 85 (players sum) = difference of 5.
        Assert.Contains("diferencia de 5", ex.Message);
    }

    [Fact]
    public async Task LoadTeamMatchSheetAsync_PlayerNotOnRoster_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);
        // A player with no PlayerTeamRegistration for this season.
        Player stranger = await SeedPlayerAsync(db, seeded.HomeTeam, "Stranger");

        await matchService.LoadMatchResultAsync(seeded.Match.Id, homeScore: 10, visitorScore: 8);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => statisticService.LoadTeamMatchSheetAsync(new LoadMatchSheetRequest
            {
                MatchId = seeded.Match.Id,
                TeamId = seeded.HomeTeam.Id,
                Scores = [new PlayerScoreEntry { PlayerId = stranger.Id, Points = 10 }],
            }));

        Assert.Contains("no está en el plantel", ex.Message);
    }

    [Fact]
    public async Task LoadTeamMatchSheetAsync_SanctionedPlayer_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);
        Player sanctioned = await SeedRosterPlayerAsync(db, seeded.HomeTeam, seeded.TournamentId, "Banned", isSanctioned: true);

        await matchService.LoadMatchResultAsync(seeded.Match.Id, homeScore: 12, visitorScore: 9);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => statisticService.LoadTeamMatchSheetAsync(new LoadMatchSheetRequest
            {
                MatchId = seeded.Match.Id,
                TeamId = seeded.HomeTeam.Id,
                Scores = [new PlayerScoreEntry { PlayerId = sanctioned.Id, Points = 12 }],
            }));

        Assert.Contains("no está habilitado", ex.Message);
    }

    // ---------- HU-72: result derived from both teams' sheets in one shot ----------

    [Fact]
    public async Task LoadMatchResultFromSheetsAsync_ValidSheets_DerivesScoreAndFinishesMatch()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);
        Player homeA = await SeedRosterPlayerAsync(db, seeded.HomeTeam, seeded.TournamentId, "Alpha");
        Player homeB = await SeedRosterPlayerAsync(db, seeded.HomeTeam, seeded.TournamentId, "Bravo");
        Player visitorA = await SeedRosterPlayerAsync(db, seeded.VisitorTeam, seeded.TournamentId, "Charlie");

        Match? updated = await statisticService.LoadMatchResultFromSheetsAsync(new LoadMatchResultFromSheetsRequest
        {
            MatchId = seeded.Match.Id,
            HomeScores =
            [
                new PlayerScoreEntry { PlayerId = homeA.Id, Points = 55 },
                new PlayerScoreEntry { PlayerId = homeB.Id, Points = 35 },
            ],
            VisitorScores = [new PlayerScoreEntry { PlayerId = visitorA.Id, Points = 80 }],
        });

        Assert.NotNull(updated);
        Assert.Equal(90, updated!.HomeScore);
        Assert.Equal(80, updated.VisitorScore);
        Assert.True(updated.IsFinished);
        Assert.Equal(MatchStatus.Played, updated.Status);
        Assert.Equal(seeded.HomeTeam.Id, updated.WinningTeamId);

        Match reloaded = await ReloadMatchAsync(db, seeded.Match.Id);
        Assert.Equal(90, reloaded.HomeScore);
        Assert.Equal(80, reloaded.VisitorScore);

        (IEnumerable<ScorerByPlayerResponse> items, _) =
            await scorerRepository.GetPlayerScoresAsync(new GetScorerFilteredRequest { MatchId = seeded.Match.Id });
        List<ScorerByPlayerResponse> ranking = [.. items];
        Assert.Equal(55, Assert.Single(ranking, r => r.PlayerId == homeA.Id).Points);
        Assert.Equal(35, Assert.Single(ranking, r => r.PlayerId == homeB.Id).Points);
        Assert.Equal(80, Assert.Single(ranking, r => r.PlayerId == visitorA.Id).Points);
    }

    [Fact]
    public async Task LoadMatchResultFromSheetsAsync_TiedSums_IsRejectedAndPersistsNothing()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);
        Player homePlayer = await SeedRosterPlayerAsync(db, seeded.HomeTeam, seeded.TournamentId, "Alpha");
        Player visitorPlayer = await SeedRosterPlayerAsync(db, seeded.VisitorTeam, seeded.TournamentId, "Bravo");

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => statisticService.LoadMatchResultFromSheetsAsync(new LoadMatchResultFromSheetsRequest
            {
                MatchId = seeded.Match.Id,
                HomeScores = [new PlayerScoreEntry { PlayerId = homePlayer.Id, Points = 70 }],
                VisitorScores = [new PlayerScoreEntry { PlayerId = visitorPlayer.Id, Points = 70 }],
            }));

        Assert.Contains("no puede terminar empatado", ex.Message);

        Match reloaded = await ReloadMatchAsync(db, seeded.Match.Id);
        Assert.False(reloaded.IsFinished);
        Assert.Equal(MatchStatus.Scheduled, reloaded.Status);
        Assert.Empty(await db.PlayersStatistics.Where(s => s.MatchId == seeded.Match.Id).ToListAsync());
    }

    [Fact]
    public async Task LoadMatchResultFromSheetsAsync_PlayerNotOnRoster_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);
        Player stranger = await SeedPlayerAsync(db, seeded.HomeTeam, "Stranger");
        Player visitorPlayer = await SeedRosterPlayerAsync(db, seeded.VisitorTeam, seeded.TournamentId, "Bravo");

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => statisticService.LoadMatchResultFromSheetsAsync(new LoadMatchResultFromSheetsRequest
            {
                MatchId = seeded.Match.Id,
                HomeScores = [new PlayerScoreEntry { PlayerId = stranger.Id, Points = 10 }],
                VisitorScores = [new PlayerScoreEntry { PlayerId = visitorPlayer.Id, Points = 8 }],
            }));

        Assert.Contains("no está en el plantel", ex.Message);
    }

    [Fact]
    public async Task LoadMatchResultFromSheetsAsync_SanctionedPlayer_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);
        Player sanctioned = await SeedRosterPlayerAsync(db, seeded.HomeTeam, seeded.TournamentId, "Banned", isSanctioned: true);
        Player visitorPlayer = await SeedRosterPlayerAsync(db, seeded.VisitorTeam, seeded.TournamentId, "Bravo");

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => statisticService.LoadMatchResultFromSheetsAsync(new LoadMatchResultFromSheetsRequest
            {
                MatchId = seeded.Match.Id,
                HomeScores = [new PlayerScoreEntry { PlayerId = sanctioned.Id, Points = 12 }],
                VisitorScores = [new PlayerScoreEntry { PlayerId = visitorPlayer.Id, Points = 9 }],
            }));

        Assert.Contains("no está habilitado", ex.Message);
    }

    [Fact]
    public async Task LoadMatchResultFromSheetsAsync_NonExistentMatch_ReturnsNull()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();

        Match? updated = await statisticService.LoadMatchResultFromSheetsAsync(new LoadMatchResultFromSheetsRequest
        {
            MatchId = Guid.NewGuid(),
            HomeScores = [],
            VisitorScores = [],
        });

        Assert.Null(updated);
    }

    [Fact]
    public async Task LoadMatchResultFromSheetsAsync_Correction_ReplacesBothTeamsPreviousSheetsAndScore()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();
        IScorerRepository scorerRepository = scope.ServiceProvider.GetRequiredService<IScorerRepository>();

        Seeded seeded = await SeedMatchAsync(db, StageType.Group);
        Player homePlayer = await SeedRosterPlayerAsync(db, seeded.HomeTeam, seeded.TournamentId, "Alpha");
        Player visitorPlayer = await SeedRosterPlayerAsync(db, seeded.VisitorTeam, seeded.TournamentId, "Bravo");

        await statisticService.LoadMatchResultFromSheetsAsync(new LoadMatchResultFromSheetsRequest
        {
            MatchId = seeded.Match.Id,
            HomeScores = [new PlayerScoreEntry { PlayerId = homePlayer.Id, Points = 40 }],
            VisitorScores = [new PlayerScoreEntry { PlayerId = visitorPlayer.Id, Points = 30 }],
        });

        Match? corrected = await statisticService.LoadMatchResultFromSheetsAsync(new LoadMatchResultFromSheetsRequest
        {
            MatchId = seeded.Match.Id,
            HomeScores = [new PlayerScoreEntry { PlayerId = homePlayer.Id, Points = 44 }],
            VisitorScores = [new PlayerScoreEntry { PlayerId = visitorPlayer.Id, Points = 30 }],
        });

        Assert.NotNull(corrected);
        Assert.Equal(44, corrected!.HomeScore);

        (IEnumerable<ScorerByPlayerResponse> items, _) =
            await scorerRepository.GetPlayerScoresAsync(new GetScorerFilteredRequest { MatchId = seeded.Match.Id });
        List<ScorerByPlayerResponse> ranking = [.. items];
        Assert.Equal(44, Assert.Single(ranking, r => r.PlayerId == homePlayer.Id).Points);
        Assert.Single(ranking, r => r.PlayerId == homePlayer.Id);
    }

    // ---------- seeding ----------

    private sealed record Seeded(Tournament Tournament, Guid TournamentId, Guid DivisionId, Team HomeTeam, Team VisitorTeam, Match Match);

    private static async Task<Seeded> SeedMatchAsync(ApplicationDBContext db, StageType stageType)
    {
        Tournament tournament = await SeedTournamentAsync(db);
        Team home = await SeedTeamAsync(db, tournament.Id);
        Team visitor = await SeedTeamAsync(db, tournament.Id);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedStageAsync(db, division, tournament, stageType);

        Match match = new()
        {
            MatchDate = stage.StartDate,
            Type = stageType == StageType.Group ? MatchType.Regular : MatchType.Playoff,
            Slug = $"match-{Guid.NewGuid()}",
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            IsFinished = false,
            Status = MatchStatus.Scheduled,
            Stage = stage,
            StageId = stage.Id,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return new Seeded(tournament, tournament.Id, division.Id, home, visitor, match);
    }

    private static async Task<Match> ReloadMatchAsync(ApplicationDBContext db, Guid matchId)
    {
        db.ChangeTracker.Clear();
        return await db.Matches.FindAsync(matchId)
            ?? throw new InvalidOperationException("Seeded match disappeared.");
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Match-result test tournament",
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

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, Guid tournamentId)
    {
        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            TournamentId = tournamentId,
            Players = [],
            CreatedBy = "test",
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        return team;
    }

    private static async Task<Division> SeedDivisionAsync(ApplicationDBContext db, Tournament tournament)
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

        return division;
    }

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db, Division division, Tournament tournament, StageType stageType)
    {
        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"{stageType}-{Guid.NewGuid()}",
            StageType = stageType,
            IsActive = true,
            IsElimination = stageType != StageType.Group,
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

    private static async Task<Player> SeedPlayerAsync(ApplicationDBContext db, Team team, string lastName, bool isSanctioned = false)
    {
        Player player = new()
        {
            Slug = $"player-{Guid.NewGuid()}",
            FirstName = "Test",
            LastName = lastName,
            SecondName = null,
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = isSanctioned,
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

    private static async Task<Player> SeedRosterPlayerAsync(
        ApplicationDBContext db, Team team, Guid tournamentId, string lastName,
        bool isSanctioned = false, MedicalRecordStatus medicalStatus = MedicalRecordStatus.Approved)
    {
        Player player = await SeedPlayerAsync(db, team, lastName, isSanctioned);

        // File-backed habilitación (medical-records-storage-eligibility): an
        // Approved registration must also carry a real stored file reference
        // to read as habilitado, so this default-Approved fixture stores one.
        bool approved = medicalStatus == MedicalRecordStatus.Approved;
        db.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
        {
            PlayerId = player.Id,
            TeamId = team.Id,
            TournamentId = tournamentId,
            MedicalRecordStatus = medicalStatus,
            MedicalRecordFileUrl = approved ? $"{team.Id}/{player.Id}/{Guid.NewGuid()}.pdf" : null,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        return player;
    }
}
