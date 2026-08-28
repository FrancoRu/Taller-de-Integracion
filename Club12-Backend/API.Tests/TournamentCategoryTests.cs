using Application.DTOs.Tournament.Request;
using Application.DTOs.Tournament.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Tests;

/// <summary>
/// HU-48: femenino is, by design, a SEPARATE tournament. A tournament carries
/// a <see cref="TournamentCategory"/> (the source of truth, surfaced on the
/// response) and every division under it must share that category — a single
/// tournament can never mix feminine and masculine divisions. These tests
/// cover the persisted/surfaced category and the create/update invariant that
/// rejects a mix at the service layer.
/// </summary>
public class TournamentCategoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    // The API serializes enums as strings (JsonStringEnumConverter), so
    // response reads must use the same convention to parse Status/Category.
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public TournamentCategoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTournament_Feminine_PersistsAndSurfacesCategoryOnResponse()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        CreateTournamentRequest request = new()
        {
            Name = $"Femenino-{Guid.NewGuid()}",
            Description = "HU-48 feminine tournament",
            StartDate = start,
            TeamRegistrationDeadline = start.AddDays(-1),
            Category = TournamentCategory.Feminine,
        };

        HttpResponseMessage createResponse = await client.PostAsJsonAsync("api/tournaments", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        TournamentResponse? created = await createResponse.Content.ReadFromJsonAsync<TournamentResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(TournamentCategory.Feminine, created!.Category);

        // Surfaces on a subsequent read too (real round trip, not just the
        // create echo).
        TournamentResponse? fetched = await client.GetFromJsonAsync<TournamentResponse>(
            $"api/tournaments/{created.Id}", JsonOptions);
        Assert.NotNull(fetched);
        Assert.Equal(TournamentCategory.Feminine, fetched!.Category);
    }

    [Fact]
    public async Task CreateTournament_DefaultsToMasculineWhenOmitted()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        // Category intentionally omitted from the JSON payload.
        var payload = new
        {
            Name = $"Default-{Guid.NewGuid()}",
            Description = "Category omitted",
            StartDate = start,
            TeamRegistrationDeadline = start.AddDays(-1),
        };

        HttpResponseMessage createResponse = await client.PostAsJsonAsync("api/tournaments", payload);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        TournamentResponse? created = await createResponse.Content.ReadFromJsonAsync<TournamentResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(TournamentCategory.Masculine, created!.Category);
    }

    [Fact]
    public async Task CreateDivisionAsync_CategoryMatchesTournament_SucceedsAndPersists()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentCategory.Feminine);

        Division division = BuildDivision(tournament, TournamentCategory.Feminine);

        Division created = await divisionService.CreateDivisionAsync(division);

        Assert.NotEqual(Guid.Empty, created.Id);

        Division persisted = await db.Divisions.AsNoTracking().SingleAsync(d => d.Id == created.Id);
        Assert.Equal(TournamentCategory.Feminine, persisted.Category);
    }

    [Fact]
    public async Task CreateDivisionAsync_FeminineDivisionInMasculineTournament_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentCategory.Masculine);

        Division division = BuildDivision(tournament, TournamentCategory.Feminine);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => divisionService.CreateDivisionAsync(division));

        Assert.Equal(0, await db.Divisions.CountAsync(d => d.TournamentId == tournament.Id));
    }

    [Fact]
    public async Task CreateDivisionAsync_MasculineDivisionInFeminineTournament_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentCategory.Feminine);

        Division division = BuildDivision(tournament, TournamentCategory.Masculine);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => divisionService.CreateDivisionAsync(division));

        Assert.Equal(0, await db.Divisions.CountAsync(d => d.TournamentId == tournament.Id));
    }

    private static Division BuildDivision(Tournament tournament, TournamentCategory category)
    {
        return new Division
        {
            Name = $"Division-{Guid.NewGuid()}",
            Slug = string.Empty,
            Tournament = tournament,
            TournamentId = tournament.Id,
            Category = category,
            Stages = [],
            CreatedBy = "test",
        };
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, TournamentCategory category)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "HU-48 category test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = start.AddDays(-1),
            StartDate = start,
            // Structural changes (division create) require OpenForRegistration,
            // so the category invariant is reached rather than the HU-31 guard.
            Status = TournamentStatus.OpenForRegistration,
            Category = category,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }
}
