using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// HU-107: with a tournament OpenForRegistration, the admin enrolls teams into
/// the registration phase through POST api/tournaments/{tournamentId}/enroll-team.
/// Two modes: create a brand-new team, or enroll an existing club (from another
/// season) optionally copying its roster as an editable base for the new season.
/// Exercised as real HTTP round trips through CustomWebApplicationFactory
/// (TournamentController has no SupabaseHelper dependency).
/// </summary>
public class TournamentEnrollTeamTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentEnrollTeamTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// New-team path: a brand-new team is created and registered to the
    /// tournament in one shot; its roster starts empty.
    /// </summary>
    [Fact]
    public async Task EnrollTeam_NewTeam_CreatesTeamRegistersItAndStartsEmpty()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        string teamName = $"New Club {Guid.NewGuid():N}";

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/enroll-team",
            new { newTeamName = teamName });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        EnrolledTeamResponse? body = await response.Content.ReadFromJsonAsync<EnrolledTeamResponse>();
        Assert.NotNull(body);
        Assert.Equal(teamName, body!.Name);
        Assert.Empty(body.Players);

        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Assert.True(await verifyDb.TeamTournamentRegistrations
            .AsNoTracking()
            .AnyAsync(r => r.TeamId == body.Id && r.TournamentId == tournamentId));
    }

    /// <summary>
    /// Existing-team path: the same Team is enrolled to a new season via a new
    /// TeamTournamentRegistration — no duplicate Team identity is created (HU-99).
    /// </summary>
    [Fact]
    public async Task EnrollTeam_ExistingTeam_RegistersSameTeamToTournament()
    {
        Guid tournamentId;
        Guid existingTeamId;
        int teamsBefore;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament previousSeason = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            Tournament newSeason = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            Team team = await SeedTeamAsync(db, previousSeason.Id);
            tournamentId = newSeason.Id;
            existingTeamId = team.Id;
            teamsBefore = await db.Teams.CountAsync();
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/enroll-team",
            new { existingTeamId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        EnrolledTeamResponse? body = await response.Content.ReadFromJsonAsync<EnrolledTeamResponse>();
        Assert.NotNull(body);
        Assert.Equal(existingTeamId, body!.Id);

        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        // No new Team identity was minted.
        Assert.Equal(teamsBefore, await verifyDb.Teams.AsNoTracking().CountAsync());
        Assert.True(await verifyDb.TeamTournamentRegistrations
            .AsNoTracking()
            .AnyAsync(r => r.TeamId == existingTeamId && r.TournamentId == tournamentId));
    }

    /// <summary>
    /// Existing-team + copy roster: every player from the source season gets a
    /// fresh season-scoped registration on THIS tournament, reusing the same
    /// Player; the medical approval is NOT inherited — it starts Pending (HU-59).
    /// </summary>
    [Fact]
    public async Task EnrollTeam_ExistingTeamWithCopyRoster_CopiesPlayersButNotMedicalApproval()
    {
        Guid targetTournamentId;
        Guid sourceTournamentId;
        Guid teamId;
        Guid playerId;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament sourceSeason = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            Tournament targetSeason = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            Team team = await SeedTeamAsync(db, sourceSeason.Id);
            Player player = await SeedPlayerAsync(db, team);

            PlayerTeamRegistration registration = new()
            {
                PlayerId = player.Id,
                TeamId = team.Id,
                TournamentId = sourceSeason.Id,
                MedicalRecordStatus = MedicalRecordStatus.Approved,
                MedicalRecordFileUrl = "medical-records/prev.pdf",
                MedicalRecordFileName = "prev.pdf",
                JerseyNumber = 10,
                CreatedBy = "test",
            };
            db.PlayerTeamRegistrations.Add(registration);
            await db.SaveChangesAsync();

            sourceTournamentId = sourceSeason.Id;
            targetTournamentId = targetSeason.Id;
            teamId = team.Id;
            playerId = player.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{targetTournamentId}/enroll-team",
            new { existingTeamId = teamId, copyRosterFromTournamentId = sourceTournamentId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        EnrolledTeamResponse? body = await response.Content.ReadFromJsonAsync<EnrolledTeamResponse>();
        Assert.NotNull(body);
        Assert.Contains(body!.Players, p => p.Id == playerId);

        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        PlayerTeamRegistration copied = await verifyDb.PlayerTeamRegistrations
            .AsNoTracking()
            .SingleAsync(r => r.TeamId == teamId && r.TournamentId == targetTournamentId && r.PlayerId == playerId);

        Assert.Equal(MedicalRecordStatus.Pending, copied.MedicalRecordStatus);
        Assert.Null(copied.MedicalRecordFileUrl);
        Assert.Null(copied.MedicalRecordFileName);
        Assert.Null(copied.JerseyNumber);
    }

    /// <summary>
    /// Enrolling a second team must not drop the first: enroll is additive, not
    /// a whole-tournament reconcile that would unregister the rest of the roster.
    /// </summary>
    [Fact]
    public async Task EnrollTeam_SecondTeam_KeepsFirstTeamEnrolled()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        EnrolledTeamResponse? first = await (await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/enroll-team",
            new { newTeamName = $"First {Guid.NewGuid():N}" })).Content.ReadFromJsonAsync<EnrolledTeamResponse>();

        EnrolledTeamResponse? second = await (await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/enroll-team",
            new { newTeamName = $"Second {Guid.NewGuid():N}" })).Content.ReadFromJsonAsync<EnrolledTeamResponse>();

        Assert.NotNull(first);
        Assert.NotNull(second);

        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Assert.True(await verifyDb.TeamTournamentRegistrations.AsNoTracking()
            .AnyAsync(r => r.TeamId == first!.Id && r.TournamentId == tournamentId));
        Assert.True(await verifyDb.TeamTournamentRegistrations.AsNoTracking()
            .AnyAsync(r => r.TeamId == second!.Id && r.TournamentId == tournamentId));
    }

    /// <summary>
    /// Structural guard (HU-31): teams cannot be enrolled once registration
    /// closes. Mapped to 409 by the global handler (InvalidOperationException).
    /// </summary>
    [Fact]
    public async Task EnrollTeam_TournamentNotOpenForRegistration_IsRejected()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);
            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/enroll-team",
            new { newTeamName = $"New {Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Assert.Equal(0, await verifyDb.TeamTournamentRegistrations.AsNoTracking()
            .CountAsync(r => r.TournamentId == tournamentId));
    }

    /// <summary>
    /// Integrity guard: teams cannot be enrolled once the tournament is Ongoing
    /// (en curso) — the roster is frozen after it starts. Mapped to 409 by the
    /// global handler (InvalidOperationException).
    /// </summary>
    [Fact]
    public async Task EnrollTeam_TournamentOngoing_IsRejected()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/enroll-team",
            new { newTeamName = $"New {Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Assert.Equal(0, await verifyDb.TeamTournamentRegistrations.AsNoTracking()
            .CountAsync(r => r.TournamentId == tournamentId));
    }

    /// <summary>
    /// Exactly one of ExistingTeamId / NewTeamName must be provided — both is a 400.
    /// </summary>
    [Fact]
    public async Task EnrollTeam_BothExistingTeamIdAndNewTeamName_ReturnsBadRequest()
    {
        Guid tournamentId;
        Guid existingTeamId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            Team team = await SeedTeamAsync(db, tournamentId: null);
            tournamentId = tournament.Id;
            existingTeamId = team.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/enroll-team",
            new { existingTeamId, newTeamName = "Ambiguous" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Neither ExistingTeamId nor NewTeamName provided is a 400.
    /// </summary>
    [Fact]
    public async Task EnrollTeam_NeitherExistingTeamIdNorNewTeamName_ReturnsBadRequest()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/enroll-team",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// A team can be enrolled only once per tournament — a clean 409, not a raw
    /// DB error, when it is already enrolled.
    /// </summary>
    [Fact]
    public async Task EnrollTeam_TeamAlreadyEnrolled_ReturnsConflict()
    {
        Guid tournamentId;
        Guid existingTeamId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            Team team = await SeedTeamAsync(db, tournament.Id);
            db.TeamTournamentRegistrations.Add(new TeamTournamentRegistration
            {
                TeamId = team.Id,
                TournamentId = tournament.Id,
                CreatedBy = "test",
            });
            await db.SaveChangesAsync();
            tournamentId = tournament.Id;
            existingTeamId = team.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/enroll-team",
            new { existingTeamId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// Enrolling an existing team that does not exist yields a 404.
    /// </summary>
    [Fact]
    public async Task EnrollTeam_ExistingTeamNotFound_ReturnsNotFound()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/enroll-team",
            new { existingTeamId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record EnrolledTeamResponse(Guid Id, string Name, Guid? TournamentId, List<PlayerRef> Players);

    private sealed record PlayerRef(Guid Id);

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, TournamentStatus status)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Enroll-team test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Status = status,
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
}
