using Application.DTOs.TeamStaff.Request;
using Application.DTOs.TeamStaff.Response;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Integration coverage for the team-staff (cuerpo técnico) endpoints:
/// create/list/delete through real HTTP round trips and the AdminOrOwner
/// gating, mirroring PointDeductionEndpointTests.
/// </summary>
public class TeamStaffEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TeamStaffEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateThenList_ReturnsStaff_WithTeamNameAndRole()
    {
        (Team team, Tournament tournament) = await SeedTeamAndTournamentAsync();
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage create = await client.PostAsJsonAsync(
            $"api/teams/{team.Id}/staff",
            new CreateTeamStaffRequest { FullName = "Carlos Gómez", Role = TeamStaffRole.Coach, TournamentId = tournament.Id });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        TeamStaffResponse? created = await create.Content.ReadFromJsonAsync<TeamStaffResponse>();
        Assert.NotNull(created);
        Assert.Equal(team.Id, created!.TeamId);
        Assert.Equal(tournament.Id, created.TournamentId);
        Assert.Equal("Carlos Gómez", created.FullName);
        Assert.Equal(nameof(TeamStaffRole.Coach), created.Role);
        Assert.Equal(team.Name, created.TeamName);

        List<TeamStaffResponse>? listed = await client.GetFromJsonAsync<List<TeamStaffResponse>>(
            $"api/teams/{team.Id}/staff?tournamentId={tournament.Id}");
        Assert.NotNull(listed);
        Assert.Single(listed!);
        Assert.Equal(created.Id, listed![0].Id);
    }

    [Fact]
    public async Task Get_WithNoStaff_ReturnsEmptyList()
    {
        (Team team, Tournament tournament) = await SeedTeamAndTournamentAsync();
        HttpClient client = _factory.CreateClient();

        List<TeamStaffResponse>? listed = await client.GetFromJsonAsync<List<TeamStaffResponse>>(
            $"api/teams/{team.Id}/staff?tournamentId={tournament.Id}");

        Assert.NotNull(listed);
        Assert.Empty(listed!);
    }

    [Fact]
    public async Task Delete_RemovesStaff_FromSubsequentList()
    {
        (Team team, Tournament tournament) = await SeedTeamAndTournamentAsync();
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);

        HttpResponseMessage create = await client.PostAsJsonAsync(
            $"api/teams/{team.Id}/staff",
            new CreateTeamStaffRequest { FullName = "Javier Coronel", Role = TeamStaffRole.AssistantCoach, TournamentId = tournament.Id });
        TeamStaffResponse created = (await create.Content.ReadFromJsonAsync<TeamStaffResponse>())!;

        HttpResponseMessage delete = await client.DeleteAsync($"api/staff/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        List<TeamStaffResponse>? listed = await client.GetFromJsonAsync<List<TeamStaffResponse>>(
            $"api/teams/{team.Id}/staff?tournamentId={tournament.Id}");
        Assert.Empty(listed!);
    }

    [Fact]
    public async Task Create_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/teams/{Guid.NewGuid()}/staff",
            new CreateTeamStaffRequest { FullName = "x", Role = TeamStaffRole.Coach, TournamentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WrongRole_ReturnsForbidden()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Guest);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/teams/{Guid.NewGuid()}/staff",
            new CreateTeamStaffRequest { FullName = "x", Role = TeamStaffRole.Coach, TournamentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownTeam_ReturnsNotFound()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/teams/{Guid.NewGuid()}/staff",
            new CreateTeamStaffRequest { FullName = "x", Role = TeamStaffRole.Coach, TournamentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownTournament_ReturnsNotFound()
    {
        (Team team, _) = await SeedTeamAndTournamentAsync();
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/teams/{team.Id}/staff",
            new CreateTeamStaffRequest { FullName = "x", Role = TeamStaffRole.Coach, TournamentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(Team, Tournament)> SeedTeamAndTournamentAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);
        Tournament tournament = new()
        {
            Description = "Team staff test tournament",
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
        await db.SaveChangesAsync();

        return (team, tournament);
    }
}
