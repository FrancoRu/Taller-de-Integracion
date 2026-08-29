using Application.Interfaces.Services;

using Domain.Entities.Models;

using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies Season's slug support: SeasonService.CreateSeasonAsync generates a
/// clean, unique kebab-case slug from the season's Name, and
/// GetSeasonByIdOrSlugAsync resolves a season by either its GUID id or its
/// slug. Exercised at the service layer (resolved directly from DI), mirroring
/// VenueServiceSlugTests / DivisionSlugTests.
/// </summary>
public class SeasonServiceSlugTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SeasonServiceSlugTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateSeasonAsync_GeneratesCleanKebabSlugFromName()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISeasonService seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();

        Season created = await seasonService.CreateSeasonAsync(new Season
        {
            Name = $"Temporada Ñandú {Guid.NewGuid():N}",
            Slug = null!,
            Year = 2026,
            CreatedBy = "test",
        });

        Assert.False(string.IsNullOrWhiteSpace(created.Slug));
        Assert.DoesNotContain(' ', created.Slug);
        Assert.Equal(created.Slug, created.Slug.ToLowerInvariant());
        // Clean kebab: only lowercase letters, digits and single hyphens, no GUID braces/underscores.
        Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", created.Slug);
    }

    [Fact]
    public async Task CreateSeasonAsync_DuplicateName_AppendsSuffixToSlug()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISeasonService seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();

        string sharedName = $"Temporada Compartida {Guid.NewGuid():N}";

        Season first = await seasonService.CreateSeasonAsync(new Season
        {
            Name = sharedName,
            Slug = null!,
            CreatedBy = "test",
        });

        Season second = await seasonService.CreateSeasonAsync(new Season
        {
            Name = sharedName,
            Slug = null!,
            CreatedBy = "test",
        });

        Assert.NotEqual(first.Slug, second.Slug);
        Assert.Equal($"{first.Slug}-2", second.Slug);
    }

    [Fact]
    public async Task GetSeasonByIdOrSlugAsync_ResolvesByGuidIdAndBySlug()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISeasonService seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();

        Season created = await seasonService.CreateSeasonAsync(new Season
        {
            Name = $"Temporada Resolucion {Guid.NewGuid():N}",
            Slug = null!,
            CreatedBy = "test",
        });

        Season? byId = await seasonService.GetSeasonByIdOrSlugAsync(created.Id.ToString());
        Season? bySlug = await seasonService.GetSeasonByIdOrSlugAsync(created.Slug);

        Assert.NotNull(byId);
        Assert.Equal(created.Id, byId!.Id);
        Assert.NotNull(bySlug);
        Assert.Equal(created.Id, bySlug!.Id);
    }

    [Fact]
    public async Task GetSeasonByIdOrSlugAsync_UnknownSlug_ReturnsNull()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISeasonService seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();

        Season? found = await seasonService.GetSeasonByIdOrSlugAsync($"unknown-slug-{Guid.NewGuid():N}");

        Assert.Null(found);
    }
}
