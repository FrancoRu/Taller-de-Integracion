using Application.Interfaces.Services;

using Domain.Entities.Models;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Verifies Division's slug support: DivisionService.CreateDivisionAsync
/// generates a unique slug from the division's Name, GetSimpleDivisionByIdOrSlugAsync
/// resolves a division by either its GUID id or its slug, and
/// GET api/divisions/{idOrSlug}/detail resolves by either form. DivisionController
/// has no SupabaseHelper dependency, so this can run as a real HTTP round trip
/// through CustomWebApplicationFactory.
/// </summary>
public class DivisionSlugTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DivisionSlugTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateDivisionAsync_GeneratesSlugFromName()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db);

        Division created = await divisionService.CreateDivisionAsync(new Division
        {
            Name = "Zona Ñandú",
            Slug = null!,
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        });

        Assert.False(string.IsNullOrWhiteSpace(created.Slug));
        Assert.DoesNotContain(' ', created.Slug);
        Assert.Equal(created.Slug, created.Slug.ToLowerInvariant());
    }

    [Fact]
    public async Task CreateDivisionAsync_DuplicateName_AppendsSuffixToSlug()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        string sharedName = "Zona " + Guid.NewGuid().ToString("N")[..12];

        Division first = await divisionService.CreateDivisionAsync(new Division
        {
            Name = sharedName,
            Slug = null!,
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        });

        Division second = await divisionService.CreateDivisionAsync(new Division
        {
            Name = sharedName,
            Slug = null!,
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        });

        Assert.NotEqual(first.Slug, second.Slug);
        Assert.Equal($"{first.Slug}-2", second.Slug);
    }

    [Fact]
    public async Task GetDivisionById_BySlug_Returns200WithMatchingDivision()
    {
        Guid createdId;
        string createdSlug;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

            Tournament tournament = await SeedTournamentAsync(db);
            Division created = await divisionService.CreateDivisionAsync(new Division
            {
                Name = "Zona " + Guid.NewGuid().ToString("N")[..12],
                Slug = null!,
                Tournament = tournament,
                TournamentId = tournament.Id,
                Stages = [],
                CreatedBy = "test",
            });

            createdId = created.Id;
            createdSlug = created.Slug;
        }

        HttpClient client = _factory.CreateClient();

        HttpResponseMessage byId = await client.GetAsync($"api/divisions/{createdId}/detail");
        HttpResponseMessage bySlug = await client.GetAsync($"api/divisions/{createdSlug}/detail");

        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bySlug.StatusCode);

        DivisionIdResponse? bySlugBody = await bySlug.Content.ReadFromJsonAsync<DivisionIdResponse>();
        Assert.NotNull(bySlugBody);
        Assert.Equal(createdId, bySlugBody!.Id);
    }

    [Fact]
    public async Task GetDivisionById_UnknownSlug_Returns404()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"api/divisions/unknown-slug-{Guid.NewGuid():N}/detail");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record DivisionIdResponse(Guid Id, string Slug);

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date;

        Tournament tournament = new()
        {
            Description = "Division slug characterization tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            MaxTeams = 8,
            MinTeams = 2,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }
}
