using Application.DTOs.MedicalRecord.Response;
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
/// Behavioural tests for the FICHA MEDICA / eligibility batch (Epica 11):
/// HU-57 (per team+tournament habilitado state), HU-58 (approve/reject with
/// reason), HU-59 (a new season starts un-habilitado, never inheriting the
/// prior one), HU-60 (only habilitado, non-sanctioned players are eligible for
/// the planilla) and HU-62 (per-player eligibility surfaced). Exercised through
/// the real services/repositories against the SQLite-backed integration host.
/// </summary>
public class MedicalRecordEligibilityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MedicalRecordEligibilityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------- HU-57/HU-59: default status is Pending ----------

    [Fact]
    public async Task NewRegistration_DefaultsToPending_AndNotHabilitado()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        Fixture fx = await SeedRegistrationAsync(db);

        MedicalRecordResponse? record = await medicalRecordService.GetAsync(fx.PlayerId, fx.TeamId, fx.TournamentId);

        Assert.NotNull(record);
        Assert.Equal(MedicalRecordStatus.Pending, record!.Status);
        Assert.False(record.IsHabilitado);
    }

    // ---------- HU-55: uploading a file does NOT habilitate ----------

    [Fact]
    public async Task RecordUpload_StoresReference_ButStaysPending()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        Fixture fx = await SeedRegistrationAsync(db);
        string fileReference = $"{fx.TeamId}/{fx.PlayerId}/{Guid.NewGuid()}.pdf";

        MedicalRecordResponse record = await medicalRecordService.RecordUploadAsync(
            fx.PlayerId, fx.TeamId, fx.TournamentId,
            fileReference, "ficha.pdf", "owner@club12");

        Assert.Equal(MedicalRecordStatus.Pending, record.Status);
        Assert.False(record.IsHabilitado);
        Assert.Equal(fileReference, record.FileUrl);
        Assert.Equal("ficha.pdf", record.FileName);
    }

    [Fact]
    public async Task RecordUpload_UnknownRegistration_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => medicalRecordService.RecordUploadAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ref", "ficha.pdf", "owner@club12"));
    }

    // ---------- HU-57: once Approved the ficha cannot be re-uploaded ----------

    [Fact]
    public async Task RecordUpload_AfterApproval_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        Fixture fx = await SeedRegistrationAsync(db);
        string originalRef = $"{fx.TeamId}/{fx.PlayerId}/{Guid.NewGuid()}.pdf";

        // Upload + approve so the record becomes habilitado.
        await medicalRecordService.RecordUploadAsync(
            fx.PlayerId, fx.TeamId, fx.TournamentId,
            originalRef, "ficha.pdf", "owner@club12");
        await medicalRecordService.ReviewAsync(
            fx.PlayerId, fx.TeamId, fx.TournamentId, approve: true, reason: null, actor: "owner@club12");

        // A second upload on an Approved record is rejected (view/download only).
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => medicalRecordService.RecordUploadAsync(
                fx.PlayerId, fx.TeamId, fx.TournamentId,
                $"{fx.TeamId}/{fx.PlayerId}/{Guid.NewGuid()}.pdf", "otra-ficha.pdf", "owner@club12"));

        Assert.Contains("ya está aprobada", ex.Message);

        // The originally approved ficha is untouched.
        MedicalRecordResponse? record = await medicalRecordService.GetAsync(fx.PlayerId, fx.TeamId, fx.TournamentId);
        Assert.Equal(MedicalRecordStatus.Approved, record!.Status);
        Assert.Equal(originalRef, record.FileUrl);
        Assert.Equal("ficha.pdf", record.FileName);
    }

    // ---------- Part 2 gap: an Approved-but-never-habilitado (legacy
    // reference) row must not be stuck forever ----------

    [Fact]
    public async Task RecordUpload_AfterApprovalWithLegacyReference_IsAllowed()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        Fixture fx = await SeedRegistrationAsync(db);

        // Approved directly on the seeded row (bypassing the write guard,
        // same shape as historical pre-relocation data) with a legacy
        // reference — this never actually habilitated the player.
        PlayerTeamRegistration registration = await db.PlayerTeamRegistrations.SingleAsync(r =>
            r.PlayerId == fx.PlayerId && r.TeamId == fx.TeamId && r.TournamentId == fx.TournamentId);
        registration.MedicalRecordStatus = MedicalRecordStatus.Approved;
        registration.MedicalRecordFileUrl = "medical-records/legacy/object.pdf";
        await db.SaveChangesAsync();

        string newRef = $"{fx.TeamId}/{fx.PlayerId}/{Guid.NewGuid()}.pdf";
        MedicalRecordResponse record = await medicalRecordService.RecordUploadAsync(
            fx.PlayerId, fx.TeamId, fx.TournamentId, newRef, "ficha-nueva.pdf", "owner@club12");

        Assert.Equal(MedicalRecordStatus.Pending, record.Status);
        Assert.Equal(newRef, record.FileUrl);
        Assert.False(record.IsHabilitado);

        // ...and can now be properly approved against the real stored file.
        MedicalRecordResponse approved = await medicalRecordService.ReviewAsync(
            fx.PlayerId, fx.TeamId, fx.TournamentId, approve: true, reason: null, actor: "owner@club12");

        Assert.Equal(MedicalRecordStatus.Approved, approved.Status);
        Assert.True(approved.IsHabilitado);
    }

    // ---------- HU-58: approve -> habilitado ----------

    [Fact]
    public async Task Approve_MakesHabilitado()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        Fixture fx = await SeedRegistrationAsync(db);

        // A file must be stored before an approve is allowed (Part 2 write guard).
        await medicalRecordService.RecordUploadAsync(
            fx.PlayerId, fx.TeamId, fx.TournamentId,
            $"{fx.TeamId}/{fx.PlayerId}/{Guid.NewGuid()}.pdf", "ficha.pdf", "owner@club12");

        MedicalRecordResponse record = await medicalRecordService.ReviewAsync(
            fx.PlayerId, fx.TeamId, fx.TournamentId, approve: true, reason: null, actor: "owner@club12");

        Assert.Equal(MedicalRecordStatus.Approved, record.Status);
        Assert.True(record.IsHabilitado);
        Assert.Null(record.ReviewReason);
        Assert.NotNull(record.ReviewedAt);
    }

    // ---------- Part 2: approve-time write guard (medical-records-storage-eligibility) ----------

    [Fact]
    public async Task Approve_WithNoStoredFile_IsRejected_AndRowStaysPending()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        Fixture fx = await SeedRegistrationAsync(db);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => medicalRecordService.ReviewAsync(fx.PlayerId, fx.TeamId, fx.TournamentId, approve: true, reason: null, actor: "owner@club12"));

        Assert.Contains("no hay un archivo cargado", ex.Message);

        MedicalRecordResponse? record = await medicalRecordService.GetAsync(fx.PlayerId, fx.TeamId, fx.TournamentId);
        Assert.Equal(MedicalRecordStatus.Pending, record!.Status);
    }

    [Fact]
    public async Task Approve_WithLegacyReference_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        Fixture fx = await SeedRegistrationAsync(db);
        await medicalRecordService.RecordUploadAsync(
            fx.PlayerId, fx.TeamId, fx.TournamentId,
            "medical-records/some/object/path.pdf", "ficha.pdf", "owner@club12");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => medicalRecordService.ReviewAsync(fx.PlayerId, fx.TeamId, fx.TournamentId, approve: true, reason: null, actor: "owner@club12"));
    }

    [Fact]
    public async Task Reject_WithNoStoredFile_IsStillAllowed()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        Fixture fx = await SeedRegistrationAsync(db);

        MedicalRecordResponse record = await medicalRecordService.ReviewAsync(
            fx.PlayerId, fx.TeamId, fx.TournamentId, approve: false, reason: "Missing signature", actor: "owner@club12");

        Assert.Equal(MedicalRecordStatus.Rejected, record.Status);
    }

    // ---------- HU-58: reject -> not habilitado + reason ----------

    [Fact]
    public async Task Reject_LeavesNotHabilitado_WithReason()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        Fixture fx = await SeedRegistrationAsync(db);

        MedicalRecordResponse record = await medicalRecordService.ReviewAsync(
            fx.PlayerId, fx.TeamId, fx.TournamentId, approve: false, reason: "Missing signature", actor: "owner@club12");

        Assert.Equal(MedicalRecordStatus.Rejected, record.Status);
        Assert.False(record.IsHabilitado);
        Assert.Equal("Missing signature", record.ReviewReason);
    }

    // ---------- HU-60: only approved players are eligible for the planilla ----------

    [Fact]
    public async Task LoadTeamMatchSheetAsync_NotApprovedPlayer_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();

        Fixture fx = await SeedFinishedMatchAsync(db, homeScore: 20, medicalStatus: MedicalRecordStatus.Pending);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => statisticService.LoadTeamMatchSheetAsync(new LoadMatchSheetRequest
            {
                MatchId = fx.MatchId,
                TeamId = fx.TeamId,
                Scores = [new PlayerScoreEntry { PlayerId = fx.PlayerId, Points = 20 }],
            }));

        Assert.Contains("no está habilitado", ex.Message);
    }

    [Fact]
    public async Task LoadTeamMatchSheetAsync_AfterApproval_Succeeds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        Fixture fx = await SeedFinishedMatchAsync(db, homeScore: 15, medicalStatus: MedicalRecordStatus.Pending);

        // A file must be stored before an approve is allowed (Part 2 write guard),
        // and the match-sheet gate itself now requires it too.
        await medicalRecordService.RecordUploadAsync(
            fx.PlayerId, fx.TeamId, fx.TournamentId,
            $"{fx.TeamId}/{fx.PlayerId}/{Guid.NewGuid()}.pdf", "ficha.pdf", "owner");
        await medicalRecordService.ReviewAsync(fx.PlayerId, fx.TeamId, fx.TournamentId, approve: true, reason: null, actor: "owner");

        List<PlayerStatistic> created = await statisticService.LoadTeamMatchSheetAsync(new LoadMatchSheetRequest
        {
            MatchId = fx.MatchId,
            TeamId = fx.TeamId,
            Scores = [new PlayerScoreEntry { PlayerId = fx.PlayerId, Points = 15 }],
        });

        Assert.Single(created);
    }

    // ---------- Part 2: match-sheet gate rejects Approved-without-file ----------

    [Fact]
    public async Task LoadTeamMatchSheetAsync_ApprovedWithNoStoredFile_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();

        // Approved directly on the seeded row (bypassing the write guard), with
        // no file reference — the exact "wrong today" shape this rule targets.
        Fixture fx = await SeedFinishedMatchAsync(db, homeScore: 20, medicalStatus: MedicalRecordStatus.Approved);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => statisticService.LoadTeamMatchSheetAsync(new LoadMatchSheetRequest
            {
                MatchId = fx.MatchId,
                TeamId = fx.TeamId,
                Scores = [new PlayerScoreEntry { PlayerId = fx.PlayerId, Points = 20 }],
            }));

        Assert.Contains("no está habilitado", ex.Message);
    }

    // ---------- Part 2: season roster surfacing reflects the file-backed rule ----------

    [Fact]
    public async Task GetTeamByIdAsync_ApprovedPlayerWithLegacyReference_ReadsAsNotHabilitado()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = await SeedTournamentAsync(seedDb);
        Team team = await SeedTeamAsync(seedDb, tournament.Id);
        Player player = await SeedPlayerAsync(seedDb, team);

        seedDb.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
        {
            PlayerId = player.Id,
            TeamId = team.Id,
            TournamentId = tournament.Id,
            MedicalRecordStatus = MedicalRecordStatus.Approved,
            MedicalRecordFileUrl = "medical-records/some/object/path.pdf",
            CreatedBy = "test",
        });
        await seedDb.SaveChangesAsync();

        using IServiceScope scope = _factory.Services.CreateScope();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Team? loaded = await teamService.GetTeamByIdAsync(team.Id, tournament.Id);

        Assert.NotNull(loaded);
        Domain.Entities.Models.Player rosterPlayer = Assert.Single(loaded!.Players, p => p.Id == player.Id);
        Assert.False(rosterPlayer.IsHabilitado);
    }

    // ---------- HU-59: approval does NOT carry across seasons ----------

    [Fact]
    public async Task ApprovalInOneSeason_DoesNotHabilitateAnother()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerStatisticService statisticService = scope.ServiceProvider.GetRequiredService<IPlayerStatisticService>();
        IMedicalRecordService medicalRecordService = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

        // Season A: player approved for team A in tournament A (file stored first — Part 2 write guard).
        Fixture seasonA = await SeedFinishedMatchAsync(db, homeScore: 10, medicalStatus: MedicalRecordStatus.Pending);
        await medicalRecordService.RecordUploadAsync(
            seasonA.PlayerId, seasonA.TeamId, seasonA.TournamentId,
            $"{seasonA.TeamId}/{seasonA.PlayerId}/{Guid.NewGuid()}.pdf", "ficha.pdf", "owner");
        await medicalRecordService.ReviewAsync(seasonA.PlayerId, seasonA.TeamId, seasonA.TournamentId, approve: true, reason: null, actor: "owner");

        // Season B: the SAME player registered to a different team in a
        // different tournament, with a fresh (Pending) medical record.
        Tournament tournamentB = await SeedTournamentAsync(db);
        Team teamB = await SeedTeamAsync(db, tournamentB.Id);
        Division divisionB = await SeedDivisionAsync(db, tournamentB);
        Stage stageB = await SeedStageAsync(db, divisionB);
        Match matchB = await SeedFinishedMatchRowAsync(db, teamB, stageB, homeScore: 10);
        db.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
        {
            PlayerId = seasonA.PlayerId,
            TeamId = teamB.Id,
            TournamentId = tournamentB.Id,
            MedicalRecordStatus = MedicalRecordStatus.Pending,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();
        await SeedHabilitadoFillersAsync(db, teamB, tournamentB.Id);
        await SeedHabilitadoFillersAsync(db, matchB.VisitorTeam!, tournamentB.Id);

        // Season B eligibility is independent: the player is NOT eligible there.
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => statisticService.LoadTeamMatchSheetAsync(new LoadMatchSheetRequest
            {
                MatchId = matchB.Id,
                TeamId = teamB.Id,
                Scores = [new PlayerScoreEntry { PlayerId = seasonA.PlayerId, Points = 10 }],
            }));
        Assert.Contains("no está habilitado", ex.Message);

        // ...and season B's record is still Pending while season A stays Approved.
        MedicalRecordResponse? recordB = await medicalRecordService.GetAsync(seasonA.PlayerId, teamB.Id, tournamentB.Id);
        MedicalRecordResponse? recordA = await medicalRecordService.GetAsync(seasonA.PlayerId, seasonA.TeamId, seasonA.TournamentId);
        Assert.Equal(MedicalRecordStatus.Pending, recordB!.Status);
        Assert.Equal(MedicalRecordStatus.Approved, recordA!.Status);
    }

    // ---------- seeding ----------

    private sealed record Fixture(Guid PlayerId, Guid TeamId, Guid TournamentId, Guid MatchId);

    private static async Task<Fixture> SeedRegistrationAsync(ApplicationDBContext db)
    {
        Tournament tournament = await SeedTournamentAsync(db);
        Team team = await SeedTeamAsync(db, tournament.Id);
        Player player = await SeedPlayerAsync(db, team);

        db.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
        {
            PlayerId = player.Id,
            TeamId = team.Id,
            TournamentId = tournament.Id,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        return new Fixture(player.Id, team.Id, tournament.Id, Guid.Empty);
    }

    private static async Task<Fixture> SeedFinishedMatchAsync(
        ApplicationDBContext db, int homeScore, MedicalRecordStatus medicalStatus)
    {
        Tournament tournament = await SeedTournamentAsync(db);
        Team team = await SeedTeamAsync(db, tournament.Id);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedStageAsync(db, division);
        Player player = await SeedPlayerAsync(db, team);

        db.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
        {
            PlayerId = player.Id,
            TeamId = team.Id,
            TournamentId = tournament.Id,
            MedicalRecordStatus = medicalStatus,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        Match match = await SeedFinishedMatchRowAsync(db, team, stage, homeScore);

        // Baseline habilitado padding (owner's walkover-threshold rule,
        // HU-73): these tests target the SINGLE player under test's own
        // eligibility, so both teams need >= 4 habilitado players of their
        // own to avoid tripping the team-wide walkover gate instead.
        await SeedHabilitadoFillersAsync(db, team, tournament.Id);
        await SeedHabilitadoFillersAsync(db, match.VisitorTeam!, tournament.Id);

        return new Fixture(player.Id, team.Id, tournament.Id, match.Id);
    }

    private static async Task SeedHabilitadoFillersAsync(
        ApplicationDBContext db, Team team, Guid tournamentId, int count = 4)
    {
        for (int i = 0; i < count; i++)
        {
            Player filler = await SeedPlayerAsync(db, team);
            db.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
            {
                PlayerId = filler.Id,
                TeamId = team.Id,
                TournamentId = tournamentId,
                MedicalRecordStatus = MedicalRecordStatus.Approved,
                MedicalRecordFileUrl = $"{team.Id}/{filler.Id}/{Guid.NewGuid()}.pdf",
                CreatedBy = "test",
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Match> SeedFinishedMatchRowAsync(
        ApplicationDBContext db, Team home, Stage stage, int homeScore)
    {
        Team visitor = await SeedTeamAsync(db, home.TournamentId!.Value);

        Match match = new()
        {
            MatchDate = stage.StartDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            HomeScore = homeScore,
            VisitorScore = homeScore - 1,
            IsFinished = true,
            Status = MatchStatus.Played,
            WinningTeamId = home.Id,
            Stage = stage,
            StageId = stage.Id,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return match;
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Medical-record test tournament",
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

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db, Division division)
    {
        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Group-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            IsElimination = false,
            StartDate = DateTime.UtcNow.Date.AddDays(30),
            EndDate = DateTime.UtcNow.Date.AddDays(37),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
    }

    private static async Task<Player> SeedPlayerAsync(ApplicationDBContext db, Team team)
    {
        Player player = new()
        {
            Slug = $"player-{Guid.NewGuid()}",
            FirstName = "Test",
            LastName = $"Player-{Guid.NewGuid():N}",
            SecondName = null,
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = false,
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
}
