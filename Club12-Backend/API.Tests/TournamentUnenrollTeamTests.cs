using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;

namespace API.Tests;

/// <summary>
/// HU-107 gap: DELETE api/tournaments/{tournamentId}/teams/{teamId} removes a
/// team from a tournament while it is still OpenForRegistration or
/// RegistrationClosed. Only THIS tournament's footprint (season registration,
/// this season's roster registrations, and stage assignments) is removed; other
/// seasons are untouched. Exercised as real HTTP round trips through
/// CustomWebApplicationFactory.
/// </summary>
public class TournamentUnenrollTeamTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentUnenrollTeamTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unenroll_EnrolledTeam_RemovesRegistrationRosterAssignmentsAndClearsPointer()
    {
        Guid tournamentId;
        Guid teamId;
        Guid stageId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            Division division = await SeedDivisionAsync(db, tournament);
            Stage stage = await SeedGroupStageAsync(db, division);
            Team team = await SeedTeamAsync(db, tournament.Id);
            Player player = await SeedPlayerAsync(db, team);
            await EnrollAsync(db, tournament, stage, team, player);

            tournamentId = tournament.Id;
            teamId = team.Id;
            stageId = stage.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.DeleteAsync($"api/tournaments/{tournamentId}/teams/{teamId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Assert.False(await verifyDb.TeamTournamentRegistrations.AsNoTracking()
            .AnyAsync(r => r.TeamId == teamId && r.TournamentId == tournamentId));
        Assert.False(await verifyDb.PlayerTeamRegistrations.AsNoTracking()
            .AnyAsync(r => r.TeamId == teamId && r.TournamentId == tournamentId));
        Assert.False(await verifyDb.StageTeamMatches.AsNoTracking()
            .AnyAsync(m => m.TeamId == teamId && m.StageId == stageId));

        Team reloaded = await verifyDb.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId);
        Assert.Null(reloaded.TournamentId);
    }

    [Fact]
    public async Task Unenroll_TournamentOngoing_ReturnsConflictAndKeepsRegistration()
    {
        Guid tournamentId;
        Guid teamId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
            Team team = await SeedTeamAsync(db, tournament.Id);
            db.TeamTournamentRegistrations.Add(new TeamTournamentRegistration
            {
                TeamId = team.Id,
                TournamentId = tournament.Id,
                CreatedBy = "test",
            });
            await db.SaveChangesAsync();

            tournamentId = tournament.Id;
            teamId = team.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.DeleteAsync($"api/tournaments/{tournamentId}/teams/{teamId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Assert.True(await verifyDb.TeamTournamentRegistrations.AsNoTracking()
            .AnyAsync(r => r.TeamId == teamId && r.TournamentId == tournamentId));
    }

    [Fact]
    public async Task Unenroll_TeamNotEnrolled_ReturnsNotFound()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.DeleteAsync($"api/tournaments/{tournamentId}/teams/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unenroll_OnlyRemovesThisSeason_OtherSeasonUntouched()
    {
        Guid targetTournamentId;
        Guid otherTournamentId;
        Guid teamId;
        Guid otherStageId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament target = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);
            Tournament other = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            Team team = await SeedTeamAsync(db, target.Id);
            Player player = await SeedPlayerAsync(db, team);

            Division targetDivision = await SeedDivisionAsync(db, target);
            Stage targetStage = await SeedGroupStageAsync(db, targetDivision);
            await EnrollAsync(db, target, targetStage, team, player);

            Division otherDivision = await SeedDivisionAsync(db, other);
            Stage otherStage = await SeedGroupStageAsync(db, otherDivision);
            await EnrollAsync(db, other, otherStage, team, player);

            targetTournamentId = target.Id;
            otherTournamentId = other.Id;
            teamId = team.Id;
            otherStageId = otherStage.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.DeleteAsync($"api/tournaments/{targetTournamentId}/teams/{teamId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        // Target season fully removed.
        Assert.False(await verifyDb.TeamTournamentRegistrations.AsNoTracking()
            .AnyAsync(r => r.TeamId == teamId && r.TournamentId == targetTournamentId));
        // Other season completely intact.
        Assert.True(await verifyDb.TeamTournamentRegistrations.AsNoTracking()
            .AnyAsync(r => r.TeamId == teamId && r.TournamentId == otherTournamentId));
        Assert.True(await verifyDb.PlayerTeamRegistrations.AsNoTracking()
            .AnyAsync(r => r.TeamId == teamId && r.TournamentId == otherTournamentId));
        Assert.True(await verifyDb.StageTeamMatches.AsNoTracking()
            .AnyAsync(m => m.TeamId == teamId && m.StageId == otherStageId));
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, TournamentStatus status)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Unenroll test tournament",
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
            Name = $"Zone-{Guid.NewGuid()}",
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

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, Guid tournamentId)
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

    private static async Task<Player> SeedPlayerAsync(ApplicationDBContext db, Team team)
    {
        Player player = new()
        {
            FirstName = "First",
            LastName = $"Last-{Guid.NewGuid()}",
            Slug = $"player-{Guid.NewGuid()}",
            DocumentNumber = Guid.NewGuid().ToString("N")[..8],
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = team.Id,
            PlayerTeamRegistrations = [],
            CreatedBy = "test",
        };

        db.Players.Add(player);
        await db.SaveChangesAsync();
        return player;
    }

    private static async Task EnrollAsync(
        ApplicationDBContext db, Tournament tournament, Stage stage, Team team, Player player)
    {
        db.TeamTournamentRegistrations.Add(new TeamTournamentRegistration
        {
            TeamId = team.Id,
            TournamentId = tournament.Id,
            CreatedBy = "test",
        });

        db.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
        {
            PlayerId = player.Id,
            TeamId = team.Id,
            TournamentId = tournament.Id,
            CreatedBy = "test",
        });

        db.StageTeamMatches.Add(new StageTeamMatch
        {
            StageId = stage.Id,
            TeamId = team.Id,
            CreatedBy = "test",
        });

        await db.SaveChangesAsync();
    }
}
