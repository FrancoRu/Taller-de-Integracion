using Application.DTOs.Stage.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Application.Utils.Helper.Playoff;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Covers StageService.PreviewDrawAsync/CommitDrawAsync: the playoffs-only
/// random and manual seeding flow for a groupless bracket, the server-side
/// preview-token round trip, bye handling reused unchanged from
/// PlayoffSeeder, DrawnAt stamping, and the PlayoffDraw audit entry.
/// </summary>
public class PlayoffDrawTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayoffDrawTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PreviewDrawAsync_GrouplessDivision_ReturnsPairsAndToken_PersistsNothing()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAndRegisterAsync(db, tournament, division, 4);
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);

        DrawPreviewResult preview = await stageService.PreviewDrawAsync(semiFinalStage.Id, DrawMode.Random);

        Assert.Equal(2, preview.Pairs.Count);
        List<Guid> pairedTeamIds = [.. preview.Pairs
            .SelectMany(p => new[] { (Guid?) p.HomeTeamId, p.VisitorTeamId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)];
        Assert.Equal(4, pairedTeamIds.Count);
        Assert.All(teams, t => Assert.Contains(t.Id, pairedTeamIds));
        Assert.False(string.IsNullOrWhiteSpace(preview.DrawToken));

        List<Match> matches = await db.Matches.Where(m => m.StageId == semiFinalStage.Id).ToListAsync();
        Assert.All(matches, m => Assert.False(m.HomeTeamId.HasValue));
        Assert.All(matches, m => Assert.False(m.VisitorTeamId.HasValue));

        Stage? reloaded = await db.Stages.AsNoTracking().FirstOrDefaultAsync(s => s.Id == semiFinalStage.Id);
        Assert.Null(reloaded!.DrawnAt);
    }

    [Fact]
    public async Task PreviewDrawAsync_DivisionHasGroupPhase_Rejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        await SeedTeamsAndRegisterAsync(db, tournament, division, 4);
        await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null, bestOf: 1);
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.PreviewDrawAsync(semiFinalStage.Id, DrawMode.Random));
    }

    [Fact]
    public async Task PreviewDrawAsync_NonPowerOfTwoRoster_SeedPairsProducesByes()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        await SeedTeamsAndRegisterAsync(db, tournament, division, 6);
        Stage quarterFinalStage = await SeedStageAsync(db, division, tournament, StageType.QuarterFinal, bracketName: "Copa Única", bestOf: 1);
        for (int i = 0; i < 4; i++)
        {
            await SeedEmptyMatchAsync(db, quarterFinalStage);
        }

        DrawPreviewResult preview = await stageService.PreviewDrawAsync(quarterFinalStage.Id, DrawMode.Random);

        Assert.Equal(4, preview.Pairs.Count);
        Assert.Equal(2, preview.Pairs.Count(p => p.VisitorTeamId is null));
    }

    [Fact]
    public async Task CommitDrawAsync_ValidToken_BracketMatchesPreview()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        await SeedTeamsAndRegisterAsync(db, tournament, division, 4);
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);

        DrawPreviewResult preview = await stageService.PreviewDrawAsync(semiFinalStage.Id, DrawMode.Random);
        List<Match> committed = await stageService.CommitDrawAsync(semiFinalStage.Id, DrawMode.Random, drawToken: preview.DrawToken);

        List<Match> ordered = [.. committed.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];
        for (int i = 0; i < ordered.Count; i++)
        {
            Assert.Equal(preview.Pairs[i].HomeTeamId, ordered[i].HomeTeamId);
            Assert.Equal(preview.Pairs[i].VisitorTeamId, ordered[i].VisitorTeamId);
        }
    }

    [Fact]
    public async Task CommitDrawAsync_InvalidOrMismatchedToken_Rejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        await SeedTeamsAndRegisterAsync(db, tournament, division, 4);
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa de Oro", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CommitDrawAsync(semiFinalStage.Id, DrawMode.Random, drawToken: "not-a-real-token"));

        Stage otherStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa de Plata", bestOf: 1);
        await SeedEmptyMatchAsync(db, otherStage);
        await SeedEmptyMatchAsync(db, otherStage);

        DrawPreviewResult otherPreview = await stageService.PreviewDrawAsync(otherStage.Id, DrawMode.Random);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CommitDrawAsync(semiFinalStage.Id, DrawMode.Random, drawToken: otherPreview.DrawToken));
    }

    [Fact]
    public async Task CommitDrawAsync_ManualOrder_SeedsExactOrder_NoShuffle()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAndRegisterAsync(db, tournament, division, 4);
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);

        List<Guid> manualOrder = [teams[3].Id, teams[2].Id, teams[1].Id, teams[0].Id];

        List<Match> committed = await stageService.CommitDrawAsync(semiFinalStage.Id, DrawMode.Manual, manualOrder: manualOrder);
        List<Match> ordered = [.. committed.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)];

        List<(Guid HomeTeamId, Guid? VisitorTeamId)> expectedPairs = PlayoffSeeder.SeedPairs(manualOrder);
        for (int i = 0; i < ordered.Count; i++)
        {
            Assert.Equal(expectedPairs[i].HomeTeamId, ordered[i].HomeTeamId);
            Assert.Equal(expectedPairs[i].VisitorTeamId, ordered[i].VisitorTeamId);
        }
    }

    [Fact]
    public async Task CommitDrawAsync_ByesAdvanceAutomatically_ViaTryAdvanceStageWinnerAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAndRegisterAsync(db, tournament, division, 3);
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, finalStage);

        List<Guid> manualOrder = [.. teams.Select(t => t.Id)];
        await stageService.CommitDrawAsync(semiFinalStage.Id, DrawMode.Manual, manualOrder: manualOrder);

        Match finalMatch = await db.Matches.SingleAsync(m => m.StageId == finalStage.Id);

        Assert.True(finalMatch.HomeTeamId == teams[0].Id || finalMatch.VisitorTeamId == teams[0].Id);
    }

    [Fact]
    public async Task CommitDrawAsync_StampsDrawnAtOnFirstRoundStageOnly()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAndRegisterAsync(db, tournament, division, 4);
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, finalStage);

        await stageService.CommitDrawAsync(semiFinalStage.Id, DrawMode.Manual, manualOrder: [.. teams.Select(t => t.Id)]);

        Stage? reloadedSemi = await db.Stages.AsNoTracking().FirstOrDefaultAsync(s => s.Id == semiFinalStage.Id);
        Stage? reloadedFinal = await db.Stages.AsNoTracking().FirstOrDefaultAsync(s => s.Id == finalStage.Id);

        Assert.NotNull(reloadedSemi!.DrawnAt);
        Assert.Null(reloadedFinal!.DrawnAt);
    }

    [Theory]
    [InlineData(DrawMode.Random, "aleatorio")]
    [InlineData(DrawMode.Manual, "manual")]
    public async Task CommitDrawAsync_WritesPlayoffDrawAuditEntry_DetailDescribesDrawMode(DrawMode mode, string expectedWord)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAndRegisterAsync(db, tournament, division, 4);
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);

        string? drawToken = null;
        List<Guid>? manualOrder = null;

        if (mode == DrawMode.Random)
        {
            DrawPreviewResult preview = await stageService.PreviewDrawAsync(semiFinalStage.Id, DrawMode.Random);
            drawToken = preview.DrawToken;
        }
        else
        {
            manualOrder = [.. teams.Select(t => t.Id)];
        }

        await stageService.CommitDrawAsync(semiFinalStage.Id, mode, drawToken: drawToken, manualOrder: manualOrder);

        AuditLog entry = await db.AuditLogs.SingleAsync(
            a => a.Action == AuditAction.PlayoffDraw && a.TargetId == semiFinalStage.Id.ToString());

        Assert.Equal("Stage", entry.TargetType);
        Assert.Equal(semiFinalStage.Id.ToString(), entry.TargetId);
        Assert.Contains(expectedWord, entry.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("4", entry.Detail!);
    }

    [Fact]
    public async Task CommitDrawAsync_AuditServiceThrows_DrawStillSucceeds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        ILogger<StageService> logger = scope.ServiceProvider.GetRequiredService<ILogger<StageService>>();
        IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        IStageService stageService = new StageService(unitOfWork, logger, configuration, new ThrowingAuditService());

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAndRegisterAsync(db, tournament, division, 4);
        Stage semiFinalStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: "Copa Única", bestOf: 1);
        await SeedEmptyMatchAsync(db, semiFinalStage);
        await SeedEmptyMatchAsync(db, semiFinalStage);

        List<Match> committed = await stageService.CommitDrawAsync(
            semiFinalStage.Id, DrawMode.Manual, manualOrder: [.. teams.Select(t => t.Id)]);

        Assert.Equal(2, committed.Count);
        Assert.All(committed, m => Assert.True(m.HomeTeamId.HasValue));
    }

    /// <summary>
    /// A throwing IAuditService double used to prove a logging failure never blocks a draw commit.
    /// </summary>
    private sealed class ThrowingAuditService : IAuditService
    {
        public Task LogAsync(
            AuditAction action,
            string? targetType = null,
            string? targetId = null,
            string? targetName = null,
            string? detail = null,
            CancellationToken ct = default)
        {
            throw new InvalidOperationException("Simulated audit failure.");
        }

        public Task<Application.DTOs.Abstract.Response.PaginatedResponse<AuditLog>> GetAuditLogsAsync(
            Application.DTOs.AuditLogs.Request.AuditLogFilteredRequest filter)
        {
            throw new InvalidOperationException("Simulated audit failure.");
        }
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Playoff draw test tournament",
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
}
