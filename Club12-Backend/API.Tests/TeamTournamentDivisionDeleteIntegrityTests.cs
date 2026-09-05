using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Integrity tests for team, tournament and division deletion — the global
/// extension of the player/venue delete-integrity principle. Deleting a
/// competitive entity that owns or is referenced by real history must BLOCK
/// with a clear 409 (InvalidOperationException) instead of silently orphaning
/// data or cascading history away; an entity with no such dependents is still
/// removed cleanly. Exercised through the real services/repositories against
/// the SQLite-backed integration host.
/// </summary>
public class TeamTournamentDivisionDeleteIntegrityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TeamTournamentDivisionDeleteIntegrityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------- Team ----------

    [Fact]
    public async Task DeleteTeam_WithTournamentRegistration_IsBlocked()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        (Team team, _, Guid tournamentId) = await SeedTeamAndStageAsync(db);

        db.Set<TeamTournamentRegistration>().Add(new TeamTournamentRegistration
        {
            TeamId = team.Id,
            TournamentId = tournamentId,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => teamService.DeleteTeamAsync(team.Id));

        Assert.Contains("No se puede eliminar", ex.Message);
        Assert.True(await db.Teams.AnyAsync(t => t.Id == team.Id));
    }

    [Fact]
    public async Task DeleteTeam_WithMatchHistory_IsBlocked()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        (Team team, Stage stage, _) = await SeedTeamAndStageAsync(db);
        await SeedMatchAsync(db, stage, homeTeamId: team.Id, isFinished: true);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => teamService.DeleteTeamAsync(team.Id));

        Assert.Contains("No se puede eliminar", ex.Message);
        Assert.True(await db.Teams.AnyAsync(t => t.Id == team.Id));
    }

    [Fact]
    public async Task DeleteTeam_WithPointDeduction_IsBlocked()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        (Team team, _, _) = await SeedTeamAndStageAsync(db);
        Division division = await db.Divisions.FirstAsync(d => d.TournamentId == team.TournamentId);

        db.Set<TeamPointDeduction>().Add(new TeamPointDeduction
        {
            DivisionId = division.Id,
            TeamId = team.Id,
            Points = 3,
            Reason = "Test deduction",
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => teamService.DeleteTeamAsync(team.Id));

        Assert.Contains("No se puede eliminar", ex.Message);
        Assert.True(await db.Teams.AnyAsync(t => t.Id == team.Id));
    }

    [Fact]
    public async Task DeleteTeam_WithPlayerHavingIndividualSanction_IsBlocked()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        (Team team, Stage stage, _) = await SeedTeamAndStageAsync(db);
        Match match = await SeedMatchAsync(db, stage, homeTeamId: null, isFinished: false);

        Player player = new()
        {
            FirstName = "Carlos",
            LastName = "Lopez",
            Slug = $"player-{Guid.NewGuid()}",
            DocumentNumber = Guid.NewGuid().ToString("N")[..8],
            IsSanctioned = true,
            BirthDate = DateTime.UtcNow.AddYears(-20),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = team.Id,
            CreatedBy = "test",
        };
        db.Set<Player>().Add(player);
        await db.SaveChangesAsync();

        db.Set<PlayerSanction>().Add(new PlayerSanction
        {
            Duration = 1,
            IssuedDate = DateTime.UtcNow,
            Description = "Test sanction",
            SubjectType = Domain.Enums.SanctionSubjectType.Player,
            Player = player,
            PlayerId = player.Id,
            Match = match,
            MatchId = match.Id,
            Slug = $"sanction-{Guid.NewGuid()}",
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        // Team-level checks (hasSanctions filters on PlayerSanction.TeamId,
        // which is null for a Player-subject sanction) must not miss this —
        // Team.Players cascades, which would otherwise silently wipe the
        // player and their sanction along with the team.
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => teamService.DeleteTeamAsync(team.Id));

        Assert.Contains("No se puede eliminar", ex.Message);
        Assert.True(await db.Teams.AnyAsync(t => t.Id == team.Id));
        Assert.True(await db.Set<Player>().AnyAsync(p => p.Id == player.Id));
    }

    [Fact]
    public async Task DeleteTeam_WithoutDependents_RemovesTeam()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            TournamentId = null,
            Players = [],
            CreatedBy = "test",
        };
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        await teamService.DeleteTeamAsync(team.Id);

        Assert.False(await db.Teams.AnyAsync(t => t.Id == team.Id));
    }

    // ---------- Tournament ----------

    [Fact]
    public async Task DeleteTournament_Ongoing_IsBlocked()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        (_, _, Guid tournamentId) = await SeedTeamAndStageAsync(db);
        Tournament tournament = await db.Tournaments.FirstAsync(t => t.Id == tournamentId);
        tournament.Status = TournamentStatus.Ongoing;
        await db.SaveChangesAsync();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => tournamentService.DeleteTournamentAsync(tournamentId));

        Assert.Contains("No se puede eliminar", ex.Message);
        Assert.True(await db.Tournaments.AnyAsync(t => t.Id == tournamentId));
    }

    [Fact]
    public async Task DeleteTournament_WithPlayedMatch_IsBlocked()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        (_, Stage stage, Guid tournamentId) = await SeedTeamAndStageAsync(db);
        await SeedMatchAsync(db, stage, homeTeamId: null, isFinished: true);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => tournamentService.DeleteTournamentAsync(tournamentId));

        Assert.Contains("No se puede eliminar", ex.Message);
        Assert.True(await db.Tournaments.AnyAsync(t => t.Id == tournamentId));
    }

    [Fact]
    public async Task DeleteTournament_OpenForRegistrationWithoutHistory_RemovesTournamentAndCascadesStructure()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        (Team team, Stage stage, Guid tournamentId) = await SeedTeamAndStageAsync(db);
        Tournament tournament = await db.Tournaments.FirstAsync(t => t.Id == tournamentId);
        tournament.Status = TournamentStatus.OpenForRegistration;
        await db.SaveChangesAsync();

        Guid divisionId = stage.DivisionId;

        await tournamentService.DeleteTournamentAsync(tournamentId);

        Assert.False(await db.Tournaments.AnyAsync(t => t.Id == tournamentId));
        // Owned structure cascaded away.
        Assert.False(await db.Divisions.AnyAsync(d => d.Id == divisionId));
        Assert.False(await db.Stages.AnyAsync(s => s.Id == stage.Id));
        // The team identity survives; its current-season pointer was cleared.
        Team? survivingTeam = await db.Teams.FirstOrDefaultAsync(t => t.Id == team.Id);
        Assert.NotNull(survivingTeam);
        Assert.Null(survivingTeam!.TournamentId);
    }

    // ---------- Division ----------

    [Fact]
    public async Task DeleteDivision_WithPlayedMatch_IsBlocked()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        (_, Stage stage, _) = await SeedTeamAndStageAsync(db);
        await SeedMatchAsync(db, stage, homeTeamId: null, isFinished: true);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => divisionService.DeleteDivisionAsync(stage.DivisionId));

        Assert.Contains("No se puede eliminar", ex.Message);
        Assert.True(await db.Divisions.AnyAsync(d => d.Id == stage.DivisionId));
    }

    [Fact]
    public async Task DeleteDivision_WithPointDeduction_IsBlocked()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        (Team team, Stage stage, _) = await SeedTeamAndStageAsync(db);

        db.Set<TeamPointDeduction>().Add(new TeamPointDeduction
        {
            DivisionId = stage.DivisionId,
            TeamId = team.Id,
            Points = 2,
            Reason = "Test deduction",
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => divisionService.DeleteDivisionAsync(stage.DivisionId));

        Assert.Contains("No se puede eliminar", ex.Message);
        Assert.True(await db.Divisions.AnyAsync(d => d.Id == stage.DivisionId));
    }

    [Fact]
    public async Task DeleteDivision_WithoutHistory_RemovesDivisionAndStages()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        (_, Stage stage, _) = await SeedTeamAndStageAsync(db);
        Guid divisionId = stage.DivisionId;

        await divisionService.DeleteDivisionAsync(divisionId);

        Assert.False(await db.Divisions.AnyAsync(d => d.Id == divisionId));
        // Empty stages cascade away with the division.
        Assert.False(await db.Stages.AnyAsync(s => s.Id == stage.Id));
    }

    [Fact]
    public async Task DeleteDivision_TournamentOngoing_IsBlockedEvenWithoutPlayedMatches()
    {
        // Regression: a division with zero played matches and zero point
        // deductions used to be deletable at ANY tournament status — including
        // Ongoing, where Stages/Matches are cascade-deleted at the DB level,
        // silently wiping a live, scheduled-but-unplayed fixture.
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        (_, Stage stage, _) = await SeedTeamAndStageAsync(db, TournamentStatus.Ongoing);
        await SeedMatchAsync(db, stage, homeTeamId: null, isFinished: false);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => divisionService.DeleteDivisionAsync(stage.DivisionId));

        Assert.Contains("el torneo ya arrancó", ex.Message);
        Assert.True(await db.Divisions.AnyAsync(d => d.Id == stage.DivisionId));
        Assert.True(await db.Stages.AnyAsync(s => s.Id == stage.Id));
    }

    // ---------- seeding ----------

    private static async Task<(Team Team, Stage Stage, Guid TournamentId)> SeedTeamAndStageAsync(
        ApplicationDBContext db, TournamentStatus status = TournamentStatus.OpenForRegistration)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Delete-integrity test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Status = status,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            Tournament = tournament,
            TournamentId = tournament.Id,
            Players = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Teams.Add(team);
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Group-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            IsElimination = false,
            StartDate = startDate,
            EndDate = startDate.AddDays(7),
            Division = division,
            DivisionId = division.Id,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return (team, stage, tournament.Id);
    }

    private static async Task<Match> SeedMatchAsync(ApplicationDBContext db, Stage stage, Guid? homeTeamId, bool isFinished)
    {
        Match match = new()
        {
            MatchDate = stage.StartDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            IsFinished = isFinished,
            Status = isFinished ? MatchStatus.Played : MatchStatus.Scheduled,
            Stage = stage,
            StageId = stage.Id,
            HomeTeamId = homeTeamId,
            VenueId = null,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return match;
    }
}
