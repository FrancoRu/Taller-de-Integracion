using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Verifies Stage's slug support: StageService.CreateStageAsync generates a
/// unique slug from the stage's Name, GetStageByIdOrSlugAsync resolves a stage
/// by either its GUID id or its slug, and GET api/stages/{idOrSlug} resolves by
/// either form. StageController has no SupabaseHelper dependency, so this can
/// run as a real HTTP round trip through CustomWebApplicationFactory.
///
/// A stage name is unique per division (CreateStageAsync rejects a duplicate
/// name in the same division), so slug collision — and therefore the numeric
/// suffix disambiguation — is exercised with two stages that share a name
/// across two different divisions.
/// </summary>
public class StageSlugTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StageSlugTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateStageAsync_GeneratesSlugFromName()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Division division = await SeedDivisionAsync(db);

        Stage created = await stageService.CreateStageAsync(BuildStage(division, "Fase de Grupos Ñandú"));

        Assert.False(string.IsNullOrWhiteSpace(created.Slug));
        Assert.DoesNotContain(' ', created.Slug);
        Assert.Equal(created.Slug, created.Slug.ToLowerInvariant());
    }

    [Fact]
    public async Task CreateStageAsync_DuplicateNameAcrossDivisions_AppendsSuffixToSlug()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Division divisionA = await SeedDivisionAsync(db);
        Division divisionB = await SeedDivisionAsync(db);
        string sharedName = "Playoff Oro " + Guid.NewGuid().ToString("N")[..12];

        Stage first = await stageService.CreateStageAsync(BuildStage(divisionA, sharedName));
        Stage second = await stageService.CreateStageAsync(BuildStage(divisionB, sharedName));

        Assert.NotEqual(first.Slug, second.Slug);
        Assert.Equal($"{first.Slug}-2", second.Slug);
    }

    [Fact]
    public async Task GetStageById_BySlug_Returns200WithMatchingStage()
    {
        Guid createdId;
        string createdSlug;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

            Division division = await SeedDivisionAsync(db);
            Stage created = await stageService.CreateStageAsync(
                BuildStage(division, "Fase " + Guid.NewGuid().ToString("N")[..12]));

            createdId = created.Id;
            createdSlug = created.Slug;
        }

        HttpClient client = _factory.CreateClient();

        HttpResponseMessage byId = await client.GetAsync($"api/stages/{createdId}");
        HttpResponseMessage bySlug = await client.GetAsync($"api/stages/{createdSlug}");

        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bySlug.StatusCode);

        StageIdResponse? bySlugBody = await bySlug.Content.ReadFromJsonAsync<StageIdResponse>();
        Assert.NotNull(bySlugBody);
        Assert.Equal(createdId, bySlugBody!.Id);
    }

    [Fact]
    public async Task GetStageById_UnknownSlug_Returns404()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"api/stages/unknown-slug-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record StageIdResponse(Guid Id, string Slug);

    private static Stage BuildStage(Division division, string name)
    {
        DateTime startDate = DateTime.UtcNow.Date;

        return new Stage
        {
            Name = name,
            Slug = null!,
            StageType = StageType.SemiFinal,
            IsActive = true,
            StartDate = startDate,
            EndDate = startDate.AddDays(14),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };
    }

    private static async Task<Division> SeedDivisionAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date;

        Tournament tournament = new()
        {
            Description = "Stage slug characterization tournament",
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

        Division division = new()
        {
            Id = Guid.NewGuid(),
            Name = $"Division-{Guid.NewGuid()}",
            Slug = $"division-{Guid.NewGuid()}",
            Tournament = tournament,
            Stages = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        return division;
    }
}
