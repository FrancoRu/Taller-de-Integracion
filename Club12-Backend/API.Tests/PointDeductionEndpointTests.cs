using Application.DTOs.PointDeductions.Request;
using Application.DTOs.PointDeductions.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Integration coverage for the point-deduction (deducción de puntos)
/// endpoints: create/list/delete through real HTTP round trips, the
/// AdminOrOwner gating, and the effect on the computed standings.
/// </summary>
public class PointDeductionEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PointDeductionEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateThenList_ReturnsDeduction_AndSubtractsFromStandings()
    {
        (Division division, List<Team> teams) = await SeedDivisionWithFinishedMatchAsync();
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage create = await client.PostAsJsonAsync(
            $"api/divisions/{division.Id}/point-deductions",
            new CreatePointDeductionRequest { TeamId = teams[0].Id, Points = 1, Reason = "Alineación indebida" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        PointDeductionResponse? created = await create.Content.ReadFromJsonAsync<PointDeductionResponse>();
        Assert.NotNull(created);
        Assert.Equal(teams[0].Id, created!.TeamId);
        Assert.Equal(1, created.Points);
        Assert.Equal("Alineación indebida", created.Reason);
        Assert.Equal(teams[0].Name, created.TeamName);

        List<PointDeductionResponse>? listed = await client.GetFromJsonAsync<List<PointDeductionResponse>>(
            $"api/divisions/{division.Id}/point-deductions");
        Assert.NotNull(listed);
        Assert.Single(listed!);
        Assert.Equal(created.Id, listed![0].Id);

        // The winner earned 2 points; the 1-point deduction drops it to 1 and
        // the position row exposes the applied deduction.
        using IServiceScope scope = _factory.Services.CreateScope();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();
        List<Position> positions = await divisionService.GetPositionsByDivisionIdAsync(division.Id);

        Position winner = Assert.Single(positions, p => p.TeamId == teams[0].Id);
        Assert.Equal(1, winner.Points);
        Assert.NotNull(winner.PointDeduction);
        Assert.Equal(1, winner.PointDeduction!.Points);
    }

    [Fact]
    public async Task Delete_RemovesDeduction_AndRestoresStandings()
    {
        (Division division, List<Team> teams) = await SeedDivisionWithFinishedMatchAsync();
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);

        HttpResponseMessage create = await client.PostAsJsonAsync(
            $"api/divisions/{division.Id}/point-deductions",
            new CreatePointDeductionRequest { TeamId = teams[0].Id, Points = 2, Reason = "Sanción" });
        PointDeductionResponse created = (await create.Content.ReadFromJsonAsync<PointDeductionResponse>())!;

        HttpResponseMessage delete = await client.DeleteAsync($"api/point-deductions/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        List<PointDeductionResponse>? listed = await client.GetFromJsonAsync<List<PointDeductionResponse>>(
            $"api/divisions/{division.Id}/point-deductions");
        Assert.Empty(listed!);

        using IServiceScope scope = _factory.Services.CreateScope();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();
        List<Position> positions = await divisionService.GetPositionsByDivisionIdAsync(division.Id);
        Position winner = Assert.Single(positions, p => p.TeamId == teams[0].Id);
        Assert.Equal(2, winner.Points);
        Assert.Null(winner.PointDeduction);
    }

    [Fact]
    public async Task Create_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/divisions/{Guid.NewGuid()}/point-deductions",
            new CreatePointDeductionRequest { TeamId = Guid.NewGuid(), Points = 1, Reason = "x" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WrongRole_ReturnsForbidden()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Guest);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/divisions/{Guid.NewGuid()}/point-deductions",
            new CreatePointDeductionRequest { TeamId = Guid.NewGuid(), Points = 1, Reason = "x" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownDivision_ReturnsNotFound()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/divisions/{Guid.NewGuid()}/point-deductions",
            new CreatePointDeductionRequest { TeamId = Guid.NewGuid(), Points = 1, Reason = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(Division, List<Team>)> SeedDivisionWithFinishedMatchAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);
        Tournament tournament = new()
        {
            Description = "Deduction test tournament",
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

        List<Team> teams = [];
        for (int i = 0; i < 2; i++)
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

        Stage groupStage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Group-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            IsElimination = false,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(7),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };
        db.Stages.Add(groupStage);
        await db.SaveChangesAsync();

        Match match = new()
        {
            MatchDate = groupStage.StartDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            HomeTeam = teams[0],
            HomeTeamId = teams[0].Id,
            VisitorTeam = teams[1],
            VisitorTeamId = teams[1].Id,
            HomeScore = 90,
            VisitorScore = 80,
            IsFinished = true,
            WinningTeam = teams[0],
            WinningTeamId = teams[0].Id,
            Stage = groupStage,
            StageId = groupStage.Id,
            CreatedBy = "test",
        };
        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return (division, teams);
    }
}
