using System.Linq;
using System.Net;
using System.Reflection;

using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerSanction.Request;
using Application.Interfaces.Services;

using Domain.Entities.Models;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Regression tests for the "GET /api/player-sanctions/find returns 500" bug.
///
/// Root cause: the migration 20260828040000_AddSubjectToPlayerSanction shipped
/// WITHOUT its .Designer.cs partial, so the [Migration] attribute (and the
/// migration's target-model snapshot) were missing. EF Core only discovers a
/// migration through that attribute, so the migration was silently skipped when
/// the production Npgsql database was migrated: the PlayerSanctions table never
/// got its SubjectType / TeamId / StaffName columns, while the current model
/// expected them, and every read of the entity failed with "column does not
/// exist" (HTTP 500) — even against an empty table, which is why it surfaced on
/// a fresh DB / after a data wipe and, because a global active-sanctions check
/// fires this endpoint on every page, on all routes.
///
/// The SQLite integration harness builds its schema from the current model via
/// EnsureCreated(), so it can never reproduce a "migration is not registered"
/// bug. The first test below is therefore provider-independent: it asserts every
/// compiled Migration in the Infrastructure assembly carries a [Migration]
/// attribute, which is exactly what a missing Designer partial removes.
/// </summary>
public class PlayerSanctionEmptyFilterRepro : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayerSanctionEmptyFilterRepro(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void EveryApplicationMigration_IsRegistered_WithMigrationAttribute()
    {
        Assembly migrationsAssembly = typeof(ApplicationDBContext).Assembly;

        string[] orphanMigrations = migrationsAssembly
            .GetTypes()
            .Where(t => typeof(Migration).IsAssignableFrom(t) && !t.IsAbstract)
            .Where(t => t.GetCustomAttribute<MigrationAttribute>() is null)
            .Select(t => t.FullName!)
            .OrderBy(name => name)
            .ToArray();

        Assert.True(
            orphanMigrations.Length == 0,
            "These Migration classes have no [Migration] attribute (a missing .Designer.cs partial), "
            + "so EF Core will never apply them: " + string.Join(", ", orphanMigrations));
    }

    [Fact]
    public void SubjectToPlayerSanction_Migration_IsKnownToEfCore()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Assert.Contains(
            "20260828040000_AddSubjectToPlayerSanction",
            db.Database.GetMigrations());
    }

    [Fact]
    public async Task GetPlayerSanctionsAsync_NoFilters_DoesNotThrow()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        PaginatedResponse<PlayerSanction> result = await sanctionService.GetPlayerSanctionsAsync(
            new GetPlayerSanctionsFilteredRequest { PageNumber = 1, PageSize = 10 });

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public async Task FindEndpoint_NoFilters_Returns200()
    {
        System.Net.Http.HttpClient client = _factory.CreateClient();

        System.Net.Http.HttpResponseMessage response =
            await client.GetAsync("/api/player-sanctions/find?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
