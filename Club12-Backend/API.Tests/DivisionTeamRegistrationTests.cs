using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Pins the backfill rule the AddDivisionTeamRegistrationAndStageDrawnAt
/// migration's raw SQL implements: exactly one DivisionTeamRegistration row
/// per distinct (TeamId, DivisionId) pair derived from StageTeamMatch ->
/// Stage.DivisionId, deduplicated on the pair, never on TeamId alone, and
/// safe to re-run. The migration's SQL is PostgreSQL-only (schema-qualified
/// identifiers, gen_random_uuid) and cannot execute against this project's
/// SQLite-backed test harness, so this test replicates the same join and
/// NOT EXISTS guard as an EF/LINQ query against real seeded StageTeamMatch
/// rows, mirroring the established pattern in
/// TeamTournamentRegistrationTests.Backfill_JoinLogic_RecoversHistoricalAndCurrentParticipationIdempotently.
/// </summary>
public class DivisionTeamRegistrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DivisionTeamRegistrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Backfill_TeamInTwoSubGroupsOfOneDivision_CollapsesToOneRegistration()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage groupA = await SeedStageAsync(db, division, StageType.Group, "Grupo A");
        Stage groupB = await SeedStageAsync(db, division, StageType.Group, "Grupo B");
        Team team = await SeedTeamAsync(db, tournament);
        await SeedStageTeamMatchAsync(db, groupA, team);
        await SeedStageTeamMatchAsync(db, groupB, team);

        await RunBackfillAsync(db);

        List<DivisionTeamRegistration> registrations = await ReadRegistrationsAsync(db, team.Id);
        DivisionTeamRegistration only = Assert.Single(registrations);
        Assert.Equal(division.Id, only.DivisionId);
    }

    [Fact]
    public async Task Backfill_TeamInGroupAndSameDivisionBracket_CollapsesToOneRegistration()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage groupStage = await SeedStageAsync(db, division, StageType.Group, "Fase de grupos");
        Stage bracketStage = await SeedStageAsync(db, division, StageType.SemiFinal, "Semifinal");
        Team team = await SeedTeamAsync(db, tournament);
        await SeedStageTeamMatchAsync(db, groupStage, team);
        await SeedStageTeamMatchAsync(db, bracketStage, team);

        await RunBackfillAsync(db);

        List<DivisionTeamRegistration> registrations = await ReadRegistrationsAsync(db, team.Id);
        DivisionTeamRegistration only = Assert.Single(registrations);
        Assert.Equal(division.Id, only.DivisionId);
    }

    [Fact]
    public async Task Backfill_CrossDivisionCupTeam_ProducesTwoRegistrations_NotCollapsed()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division regularDivision = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: false);
        Division cupDivision = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: true);
        Stage regularStage = await SeedStageAsync(db, regularDivision, StageType.Group, "Fase de grupos");
        Stage cupStage = await SeedStageAsync(db, cupDivision, StageType.Group, "Grupo 1");
        Team team = await SeedTeamAsync(db, tournament);
        await SeedStageTeamMatchAsync(db, regularStage, team);
        await SeedStageTeamMatchAsync(db, cupStage, team);

        await RunBackfillAsync(db);

        List<DivisionTeamRegistration> registrations = await ReadRegistrationsAsync(db, team.Id);
        Assert.Equal(2, registrations.Count);
        Assert.Contains(registrations, r => r.DivisionId == regularDivision.Id);
        Assert.Contains(registrations, r => r.DivisionId == cupDivision.Id);
    }

    [Fact]
    public async Task Backfill_ReRunAgainstAlreadyBackfilledData_CreatesNoDuplicates()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage groupA = await SeedStageAsync(db, division, StageType.Group, "Grupo A");
        Stage groupB = await SeedStageAsync(db, division, StageType.Group, "Grupo B");
        Team team = await SeedTeamAsync(db, tournament);
        await SeedStageTeamMatchAsync(db, groupA, team);
        await SeedStageTeamMatchAsync(db, groupB, team);

        await RunBackfillAsync(db);
        await RunBackfillAsync(db);
        await RunBackfillAsync(db);

        List<DivisionTeamRegistration> registrations = await ReadRegistrationsAsync(db, team.Id);
        Assert.Single(registrations);
    }

    /// <summary>
    /// Replicates the migration's INSERT ... SELECT ... GROUP BY (TeamId,
    /// DivisionId) ... WHERE NOT EXISTS: computes every distinct
    /// (TeamId, DivisionId) pair implied by StageTeamMatch -> Stage.DivisionId,
    /// skips pairs already present, and inserts exactly the missing ones.
    /// Deduplicating on the tuple, not on TeamId alone, is what keeps a
    /// cross-division-cup team's two registrations distinct.
    /// </summary>
    private static async Task RunBackfillAsync(ApplicationDBContext db)
    {
        List<(Guid TeamId, Guid DivisionId)> sourcePairs = await db.StageTeamMatches
            .Join(db.Stages, stm => stm.StageId, s => s.Id, (stm, s) => new { stm.TeamId, s.DivisionId })
            .Select(x => new ValueTuple<Guid, Guid>(x.TeamId, x.DivisionId))
            .Distinct()
            .ToListAsync();

        HashSet<(Guid TeamId, Guid DivisionId)> existingPairs = [.. await db.DivisionTeamRegistrations
            .Select(r => new ValueTuple<Guid, Guid>(r.TeamId, r.DivisionId))
            .ToListAsync()];

        foreach ((Guid teamId, Guid divisionId) in sourcePairs)
        {
            if (existingPairs.Contains((teamId, divisionId)))
            {
                continue;
            }

            db.DivisionTeamRegistrations.Add(new DivisionTeamRegistration
            {
                TeamId = teamId,
                DivisionId = divisionId,
                CreatedBy = "System",
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<List<DivisionTeamRegistration>> ReadRegistrationsAsync(ApplicationDBContext db, Guid teamId)
    {
        return await db.DivisionTeamRegistrations
            .AsNoTracking()
            .Where(r => r.TeamId == teamId)
            .ToListAsync();
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Backfill test tournament",
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
        ApplicationDBContext db, Tournament tournament, bool isCrossDivisionCup = false)
    {
        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            IsCrossDivisionCup = isCrossDivisionCup,
            CreatedBy = "test",
        };

        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        return division;
    }

    private static async Task<Stage> SeedStageAsync(
        ApplicationDBContext db, Division division, StageType stageType, string name)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(10);

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"{name}-{Guid.NewGuid()}",
            StageType = stageType,
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

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, Tournament tournament)
    {
        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Green",
            TournamentId = tournament.Id,
            Players = [],
            CreatedBy = "test",
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        return team;
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
}
