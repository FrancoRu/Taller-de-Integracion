using Application.DTOs.Divisions.Request;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Proves DivisionRosterController's HTTP wiring: every route requires Owner
/// or Admin (mirroring SeasonControllerAuthorizationTests), and staff round
/// trips through roster enroll/unenroll, sub-group rebuild, auto-distribute,
/// and manual reassignment reach the underlying service correctly.
/// </summary>
public class DivisionRosterControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DivisionRosterControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRoster_Anonymous_ReturnsUnauthorized()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Division division = await SeedDivisionAsync(db);

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync($"api/divisions/{division.Id}/roster");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRoster_GuestRole_ReturnsForbidden()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Division division = await SeedDivisionAsync(db);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Guest);
        HttpResponseMessage response = await client.GetAsync($"api/divisions/{division.Id}/roster");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EnrollTeams_StaffRole_ReturnsOkWithUpdatedRoster()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Division division = await SeedDivisionAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, division.TournamentId, 2);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/divisions/{division.Id}/roster",
            new EnrollTeamsRequest { TeamIds = [.. teams.Select(t => t.Id)] });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<TeamBody>? roster = await response.Content.ReadFromJsonAsync<List<TeamBody>>();
        Assert.NotNull(roster);
        Assert.Equal(2, roster!.Count);
        Assert.All(teams, t => Assert.Contains(roster, r => r.Id == t.Id));
    }

    [Fact]
    public async Task UnenrollTeams_StaffRole_RemovesTeamFromRoster()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Division division = await SeedDivisionAsync(db);
        Team team = (await SeedTeamsAsync(db, division.TournamentId, 1))[0];
        await SeedRegistrationAsync(db, division, team);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        HttpRequestMessage request = new(HttpMethod.Delete, $"api/divisions/{division.Id}/roster")
        {
            Content = JsonContent.Create(new UnenrollTeamsRequest { TeamIds = [team.Id] }),
        };
        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        int rowCount = await db.DivisionTeamRegistrations.CountAsync(
            r => r.DivisionId == division.Id && r.TeamId == team.Id);
        Assert.Equal(0, rowCount);
    }

    [Fact]
    public async Task RebuildSubGroups_StaffRole_ReturnsOkWithNewStages()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Division division = await SeedDivisionAsync(db);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/divisions/{division.Id}/sub-groups/rebuild",
            new RebuildSubGroupsRequest { SubGroupCount = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<StageBody>? stages = await response.Content.ReadFromJsonAsync<List<StageBody>>();
        Assert.NotNull(stages);
        Assert.Single(stages!);
    }

    [Fact]
    public async Task AutoDistributeRoster_StaffRole_ReturnsNoContent()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Division division = await SeedDivisionAsync(db);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        HttpResponseMessage response = await client.PostAsync(
            $"api/divisions/{division.Id}/roster/auto-distribute", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ReassignTeamToSubGroup_StagesInDifferentDivisions_ReturnsConflict()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Division divisionA = await SeedDivisionAsync(db);
        Division divisionB = await SeedDivisionAsync(db, divisionA.TournamentId);
        Stage fromStage = await SeedStageAsync(db, divisionA);
        Stage toStage = await SeedStageAsync(db, divisionB);
        Team team = (await SeedTeamsAsync(db, divisionA.TournamentId, 1))[0];

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/divisions/{divisionA.Id}/sub-groups/reassign",
            new ReassignTeamToSubGroupRequest { TeamId = team.Id, FromStageId = fromStage.Id, ToStageId = toStage.Id });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Roster controller test tournament",
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

    private async Task<Division> SeedDivisionAsync(ApplicationDBContext db, Guid? tournamentId = null)
    {
        Tournament tournament = tournamentId.HasValue
            ? await db.Tournaments.SingleAsync(t => t.Id == tournamentId.Value)
            : await SeedTournamentAsync(db);

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
            Name = $"Grupo-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = DateTime.UtcNow.Date.AddDays(30),
            EndDate = DateTime.UtcNow.Date.AddDays(60),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
    }

    private static async Task<List<Team>> SeedTeamsAsync(ApplicationDBContext db, Guid tournamentId, int count)
    {
        List<Team> teams = [];

        for (int i = 0; i < count; i++)
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
            teams.Add(team);
        }

        await db.SaveChangesAsync();

        return teams;
    }

    private static async Task SeedRegistrationAsync(ApplicationDBContext db, Division division, Team team)
    {
        db.DivisionTeamRegistrations.Add(new DivisionTeamRegistration
        {
            TeamId = team.Id,
            DivisionId = division.Id,
            CreatedBy = "test",
        });

        await db.SaveChangesAsync();
    }

    private sealed record TeamBody(Guid Id);

    private sealed record StageBody(Guid Id, string Name);
}
