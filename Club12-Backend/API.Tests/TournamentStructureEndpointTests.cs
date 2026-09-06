using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Application.DTOs.Tournament.Response;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// HU-cloning: GET api/tournaments/{idOrSlug}/structure returns the source
/// tournament's full cloneable structure tree, resolving id-or-slug the same
/// way GetTournamentById already does.
/// </summary>
public class TournamentStructureEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    // The API serializes enums as strings (JsonStringEnumConverter), so
    // response reads must use the same convention to parse Category.
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public TournamentStructureEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        Tournament tournament = new()
        {
            Description = "Endpoint structure test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = DateTime.UtcNow.Date.AddDays(29),
            StartDate = DateTime.UtcNow.Date.AddDays(30),
            Category = TournamentCategory.Masculine,
            Status = TournamentStatus.OpenForRegistration,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Division zone = new()
        {
            Name = "Zona A",
            Slug = $"zona-a-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            Category = tournament.Category,
            Stages = [],
            CreatedBy = "test",
        };
        zone.Stages.Add(new Stage
        {
            Name = "Fase de Grupos",
            Slug = $"fase-de-grupos-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            IsElimination = false,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(7),
            DivisionId = zone.Id,
            Division = zone,
            Matches = [],
            Order = 0,
            CreatedBy = "test",
        });
        tournament.Divisions.Add(zone);

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }

    [Fact]
    public async Task GetTournamentStructure_ById_ReturnsFullTree()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            tournamentId = (await SeedTournamentAsync(db)).Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        HttpResponseMessage response = await client.GetAsync($"api/tournaments/{tournamentId}/structure");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TournamentStructureResponse? body =
            await response.Content.ReadFromJsonAsync<TournamentStructureResponse>(JsonOptions);

        Assert.NotNull(body);
        DivisionStructureResponse division = Assert.Single(body!.Divisions);
        Assert.Equal("Zona A", division.Name);
        Assert.Single(division.Stages);
    }

    [Fact]
    public async Task GetTournamentStructure_BySlug_ReturnsFullTree()
    {
        string slug;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            slug = (await SeedTournamentAsync(db)).Slug;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        HttpResponseMessage response = await client.GetAsync($"api/tournaments/{slug}/structure");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTournamentStructure_TournamentNotFound_ReturnsNotFound()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        HttpResponseMessage response = await client.GetAsync($"api/tournaments/{Guid.NewGuid()}/structure");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
