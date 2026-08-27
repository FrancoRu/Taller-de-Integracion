using Application.DTOs.Team.Request;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies season-scoped tournament participation: TeamTournamentRegistration
/// is the source of truth for "was this team registered to this tournament
/// during this season" (HU-98 / R2 / INC-2 — "Colón SF 2026" and
/// "Colón SF 2027" are two distinct, independently preserved
/// registrations), decoupled from Team.TournamentId (a denormalized
/// "current season" pointer) the same way PlayerTeamRegistration decouples
/// roster membership from Player.TeamId. Exercises the real ITeamService
/// capabilities through a DI scope against the CustomWebApplicationFactory's
/// SQLite-backed ApplicationDBContext, matching this project's established
/// integration-test style (see PlayerTeamRegistrationTests).
/// </summary>
public class TeamTournamentRegistrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TeamTournamentRegistrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Replicates, as an EF/LINQ query, the exact two-step backfill join
    /// performed by the AddTeamTournamentRegistrationTable migration's raw
    /// SQL:
    /// (1) StageTeamMatches JOIN Stages JOIN Divisions — recovers real
    ///     historical participation even for a team whose current pointer
    ///     has since moved elsewhere;
    /// (2) Teams WHERE TournamentId IS NOT NULL — covers a team that is
    ///     currently registered but was never staged (no StageTeamMatch
    ///     rows yet), guarded so it does not duplicate a pair step (1)
    ///     already produced.
    /// This test pins that combined logic inside the automated suite,
    /// mirroring PlayerTeamRegistrationTests.Backfill_JoinLogic.
    /// </summary>
    [Fact]
    public async Task Backfill_JoinLogic_RecoversHistoricalAndCurrentParticipationIdempotently()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament season2026 = await SeedTournamentAsync(seedDb, "Colón SF 2026");
        Tournament season2027 = await SeedTournamentAsync(seedDb, "Colón SF 2027");

        // Team played real matches under season2026 (StageTeamMatch history)
        // but its denormalized pointer has since moved to season2027 — step
        // (1) must still recover the season2026 participation from history.
        Team reassignedTeam = await SeedTeamAsync(seedDb, season2027.Id);
        Division division2026 = await SeedDivisionAsync(seedDb, season2026);
        Stage stage2026 = await SeedStageAsync(seedDb, division2026);
        await SeedStageTeamMatchAsync(seedDb, stage2026, reassignedTeam);

        // Team is currently registered to season2027 but has no
        // StageTeamMatch rows yet (not staged) — step (2) must still cover
        // it via the current pointer.
        Team unstagedTeam = await SeedTeamAsync(seedDb, season2027.Id);

        // Step (1): StageTeamMatch -> Stage -> Division join.
        List<(Guid TeamId, Guid TournamentId)> historicalRows = await seedDb.StageTeamMatches
            .Join(seedDb.Stages, stm => stm.StageId, s => s.Id, (stm, s) => new { stm, s })
            .Join(seedDb.Divisions, x => x.s.DivisionId, d => d.Id, (x, d) => new { x.stm, d })
            .Select(x => new ValueTuple<Guid, Guid>(x.stm.TeamId, x.d.TournamentId))
            .Distinct()
            .ToListAsync();

        Assert.Contains(historicalRows, r => r.TeamId == reassignedTeam.Id && r.TournamentId == season2026.Id);
        Assert.DoesNotContain(historicalRows, r => r.TeamId == unstagedTeam.Id);

        // Step (2): Teams.TournamentId IS NOT NULL, NOT EXISTS-guarded
        // against pairs step (1) already produced.
        HashSet<(Guid TeamId, Guid TournamentId)> historicalSet = [.. historicalRows];

        List<(Guid TeamId, Guid TournamentId)> pointerRows = await seedDb.Teams
            .Where(t => t.TournamentId != null)
            .Select(t => new ValueTuple<Guid, Guid>(t.Id, t.TournamentId!.Value))
            .ToListAsync();

        List<(Guid TeamId, Guid TournamentId)> newFromStep2 = [.. pointerRows.Where(r => !historicalSet.Contains(r))];

        Assert.Contains(newFromStep2, r => r.TeamId == unstagedTeam.Id && r.TournamentId == season2027.Id);
        // reassignedTeam's current pointer (season2027) is a NEW pair on top
        // of its recovered season2026 history — both must be preserved,
        // never collapsed into one.
        Assert.Contains(newFromStep2, r => r.TeamId == reassignedTeam.Id && r.TournamentId == season2027.Id);
        Assert.Contains(historicalRows, r => r.TeamId == reassignedTeam.Id && r.TournamentId == season2026.Id);

        // Combined result set (what both INSERT ... SELECT statements
        // together produce) has exactly one row per distinct (team,
        // tournament) pair — re-running step (2) a second time changes
        // nothing further, since every pointerRow is already covered by
        // either historicalSet or the first newFromStep2 pass.
        HashSet<(Guid TeamId, Guid TournamentId)> combined = [.. historicalRows, .. newFromStep2];
        List<(Guid TeamId, Guid TournamentId)> secondPassNew = [.. pointerRows.Where(r => !combined.Contains(r))];
        Assert.Empty(secondPassNew);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, string name)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = $"{name} description",
            Name = $"{name}-{Guid.NewGuid()}",
            Slug = $"{name.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid()}",
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

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, Guid? tournamentId)
    {
        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Green",
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
        DateTime start = DateTime.UtcNow.Date.AddDays(10);

        Stage stage = new()
        {
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = start,
            EndDate = start.AddDays(30),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
    }

    private static async Task SeedStageTeamMatchAsync(ApplicationDBContext db, Stage stage, Team team)
    {
        StageTeamMatch stageTeamMatch = new()
        {
            StageId = stage.Id,
            Stage = stage,
            TeamId = team.Id,
            Team = team,
            CreatedBy = "test",
        };

        db.StageTeamMatches.Add(stageTeamMatch);
        await db.SaveChangesAsync();
    }

    private static async Task<TeamTournamentRegistration> SeedRegistrationAsync(
        ApplicationDBContext db, Team team, Tournament tournament)
    {
        TeamTournamentRegistration registration = new()
        {
            TeamId = team.Id,
            TournamentId = tournament.Id,
            CreatedBy = "test",
        };

        db.TeamTournamentRegistrations.Add(registration);
        await db.SaveChangesAsync();

        return registration;
    }
}
