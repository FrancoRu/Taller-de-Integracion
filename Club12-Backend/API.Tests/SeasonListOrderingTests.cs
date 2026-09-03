using Application.Interfaces.Services;

using Domain.Entities.Models;

using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies <c>SeasonService.GetAllSeasonsAsync</c> returns seasons ordered by
/// <see cref="Season.Year"/> descending (newest first), null years last, with
/// <see cref="Season.Name"/> as the deterministic tiebreaker. The admin
/// (<c>/panel/temporadas</c>) and public (<c>/temporadas</c>) pages render the
/// array in the order the endpoint returns it, so this ordering is the
/// effective one. Each test tags its seasons with a unique name token so the
/// shared fixture database cannot leak rows between tests.
/// </summary>
public class SeasonListOrderingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SeasonListOrderingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllSeasonsAsync_OrdersByYearDescending()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISeasonService seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();

        string token = Guid.NewGuid().ToString("N")[..8];
        // Created out of chronological order — a physical/insertion-order
        // result would come back 2024, 2026, 2025.
        await CreateSeasonAsync(seasonService, $"Temporada {token} 2024", 2024);
        await CreateSeasonAsync(seasonService, $"Temporada {token} 2026", 2026);
        await CreateSeasonAsync(seasonService, $"Temporada {token} 2025", 2025);

        List<int?> years = [.. (await seasonService.GetAllSeasonsAsync())
            .Where(season => season.Name.Contains(token))
            .Select(season => season.Year)];

        Assert.Equal([2026, 2025, 2024], years);
    }

    [Fact]
    public async Task GetAllSeasonsAsync_NullYearSeasons_SortLast()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISeasonService seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();

        string token = Guid.NewGuid().ToString("N")[..8];
        await CreateSeasonAsync(seasonService, $"Temporada {token} sin anio", year: null);
        await CreateSeasonAsync(seasonService, $"Temporada {token} 2025", 2025);

        List<int?> years = [.. (await seasonService.GetAllSeasonsAsync())
            .Where(season => season.Name.Contains(token))
            .Select(season => season.Year)];

        Assert.Equal([2025, null], years);
    }

    [Fact]
    public async Task GetAllSeasonsAsync_SameYear_BrokenByName()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISeasonService seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();

        string token = Guid.NewGuid().ToString("N")[..8];
        await CreateSeasonAsync(seasonService, $"Temporada {token} B", 2026);
        await CreateSeasonAsync(seasonService, $"Temporada {token} A", 2026);

        List<string> names = [.. (await seasonService.GetAllSeasonsAsync())
            .Where(season => season.Name.Contains(token))
            .Select(season => season.Name)];

        Assert.Equal([$"Temporada {token} A", $"Temporada {token} B"], names);
    }

    private static async Task CreateSeasonAsync(ISeasonService service, string name, int? year)
    {
        await service.CreateSeasonAsync(new Season
        {
            Name = name,
            Slug = null!,
            Year = year,
            CreatedBy = "test",
        });
    }
}
