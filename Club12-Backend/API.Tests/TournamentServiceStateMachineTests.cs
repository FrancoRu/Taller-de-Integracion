using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Service-level tests for the tournament state machine (HU-35) and the
/// RegistrationClosed fixture trigger (HU-37 / HU-64):
/// <see cref="ITournamentService.ChangeStatusAsync"/> enforces forward-only
/// transitions and, on entering RegistrationClosed, auto-generates the
/// fixture for every stage of every division exactly once.
/// </summary>
public class TournamentServiceStateMachineTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentServiceStateMachineTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(TournamentStatus.Scheduled, TournamentStatus.OpenForRegistration)]
    [InlineData(TournamentStatus.OpenForRegistration, TournamentStatus.RegistrationClosed)]
    [InlineData(TournamentStatus.RegistrationClosed, TournamentStatus.Ongoing)]
    [InlineData(TournamentStatus.Ongoing, TournamentStatus.Finished)]
    [InlineData(TournamentStatus.Ongoing, TournamentStatus.Canceled)]
    public async Task ChangeStatusAsync_ValidTransition_PersistsNewStatus(
        TournamentStatus from, TournamentStatus to)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, from);

        await tournamentService.ChangeStatusAsync(tournament.Id, to);

        Tournament reloaded = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(to, reloaded.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_SameStatus_IsNoOp()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);

        await tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.Ongoing);

        Tournament reloaded = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.Ongoing, reloaded.Status);
    }

    [Theory]
    // Skipping forward.
    [InlineData(TournamentStatus.Scheduled, TournamentStatus.Ongoing)]
    [InlineData(TournamentStatus.OpenForRegistration, TournamentStatus.Ongoing)]
    // Backward.
    [InlineData(TournamentStatus.RegistrationClosed, TournamentStatus.OpenForRegistration)]
    [InlineData(TournamentStatus.Ongoing, TournamentStatus.Scheduled)]
    // Out of terminal state.
    [InlineData(TournamentStatus.Finished, TournamentStatus.Ongoing)]
    [InlineData(TournamentStatus.Canceled, TournamentStatus.OpenForRegistration)]
    public async Task ChangeStatusAsync_InvalidTransition_ThrowsAndKeepsStatus(
        TournamentStatus from, TournamentStatus to)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, from);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => tournamentService.ChangeStatusAsync(tournament.Id, to));

        Tournament reloaded = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(from, reloaded.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_UnknownTournament_ThrowsKeyNotFound()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => tournamentService.ChangeStatusAsync(Guid.NewGuid(), TournamentStatus.OpenForRegistration));
    }

    [Fact]
    public async Task ChangeStatusAsync_IntoRegistrationClosed_AutoGeneratesFixtureForEveryStage()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedGroupStageAsync(db, division);
        await SeedAndAssignTeamsAsync(db, tournament, stage, teamCount: 4);

        Assert.Equal(0, await db.Matches.CountAsync(m => m.StageId == stage.Id));

        await tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.RegistrationClosed);

        // 4 teams, single round-robin => 4*3/2 = 6 matches, all seeded with a
        // real home/visitor pairing.
        List<Match> matches = await db.Matches.AsNoTracking().Where(m => m.StageId == stage.Id).ToListAsync();
        Assert.Equal(6, matches.Count);
        Assert.All(matches, m =>
        {
            Assert.NotNull(m.HomeTeamId);
            Assert.NotNull(m.VisitorTeamId);
        });

        Tournament reloaded = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.RegistrationClosed, reloaded.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_IntoRegistrationClosed_DoesNotRegenerateStageThatAlreadyHasMatches()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedGroupStageAsync(db, division);
        await SeedAndAssignTeamsAsync(db, tournament, stage, teamCount: 4);

        // Pre-generate the fixture for this stage (mirrors the still-supported
        // manual generation path). The transition must NOT double it.
        List<Match> preGenerated = await matchService.CreateAutomatedMatchesAsync(stage.Id);
        Assert.Equal(6, preGenerated.Count);

        await tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.RegistrationClosed);

        Assert.Equal(6, await db.Matches.CountAsync(m => m.StageId == stage.Id));
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, TournamentStatus status)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "State machine test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = start.AddDays(-1),
            StartDate = start,
            Status = status,
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
            Stages = [],
            CreatedBy = "test",
        };

        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        return division;
    }

    private static async Task<Stage> SeedGroupStageAsync(ApplicationDBContext db, Division division)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Group-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = start,
            EndDate = start.AddDays(14),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
    }

    private static async Task SeedAndAssignTeamsAsync(
        ApplicationDBContext db, Tournament tournament, Stage stage, int teamCount)
    {
        List<Team> teams = [];
        for (int i = 0; i < teamCount; i++)
        {
            teams.Add(new Team
            {
                Name = $"Team-{i}-{Guid.NewGuid()}",
                Slug = $"team-{i}-{Guid.NewGuid()}",
                ThreeLetterCode = $"T{i:D2}",
                LogoUrl = "https://example.com/logo.png",
                ShirtColor = "Red",
                TournamentId = tournament.Id,
                Players = [],
                CreatedBy = "test",
            });
        }

        db.Teams.AddRange(teams);
        await db.SaveChangesAsync();

        db.StageTeamMatches.AddRange(teams.Select(t => new StageTeamMatch
        {
            StageId = stage.Id,
            TeamId = t.Id,
            CreatedBy = "test",
        }));
        await db.SaveChangesAsync();
    }
}
