using Application.Interfaces.Services;

using Domain.Entities.Models;

using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies Venue's slug support: VenueService.CreateVenueAsync generates a
/// unique slug from the venue's Name, and GetVenueByIdOrSlugAsync resolves a
/// venue by either its GUID id or its slug. Exercised at the service layer
/// (resolved directly from DI) rather than through VenueController, because
/// the controller's constructor-injected SupabaseHelper eagerly opens a
/// Supabase Realtime websocket connection and cannot boot in a sandboxed test
/// environment — the same testability gap covered by BlogPostServiceSlugTests.
/// </summary>
public class VenueServiceSlugTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public VenueServiceSlugTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateVenueAsync_GeneratesSlugFromName()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IVenueService venueService = scope.ServiceProvider.GetRequiredService<IVenueService>();

        Venue created = await venueService.CreateVenueAsync(new Venue
        {
            Name = $"Polideportivo Ñandú {Guid.NewGuid():N}",
            Slug = null!,
            Address = "Av. Siempre Viva 1234",
            CreatedBy = "test",
        });

        Assert.False(string.IsNullOrWhiteSpace(created.Slug));
        Assert.DoesNotContain(' ', created.Slug);
        Assert.Equal(created.Slug, created.Slug.ToLowerInvariant());
    }

    [Fact]
    public async Task CreateVenueAsync_DuplicateName_AppendsSuffixToSlug()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IVenueService venueService = scope.ServiceProvider.GetRequiredService<IVenueService>();

        string sharedName = $"Cancha Compartida {Guid.NewGuid():N}";

        Venue first = await venueService.CreateVenueAsync(new Venue
        {
            Name = sharedName,
            Slug = null!,
            Address = "Calle Uno 1",
            CreatedBy = "test",
        });

        Venue second = await venueService.CreateVenueAsync(new Venue
        {
            Name = sharedName,
            Slug = null!,
            Address = "Calle Dos 2",
            CreatedBy = "test",
        });

        Assert.NotEqual(first.Slug, second.Slug);
        Assert.Equal($"{first.Slug}-2", second.Slug);
    }

    [Fact]
    public async Task GetVenueByIdOrSlugAsync_ResolvesByGuidId()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IVenueService venueService = scope.ServiceProvider.GetRequiredService<IVenueService>();

        Venue created = await venueService.CreateVenueAsync(new Venue
        {
            Name = $"Estadio Por Id {Guid.NewGuid():N}",
            Slug = null!,
            Address = "Ruta 5 km 12",
            CreatedBy = "test",
        });

        Venue? found = await venueService.GetVenueByIdOrSlugAsync(created.Id.ToString());

        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);
    }

    [Fact]
    public async Task GetVenueByIdOrSlugAsync_ResolvesBySlug()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IVenueService venueService = scope.ServiceProvider.GetRequiredService<IVenueService>();

        Venue created = await venueService.CreateVenueAsync(new Venue
        {
            Name = $"Estadio Por Slug {Guid.NewGuid():N}",
            Slug = null!,
            Address = "Ruta 5 km 12",
            CreatedBy = "test",
        });

        Venue? found = await venueService.GetVenueByIdOrSlugAsync(created.Slug);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);
    }

    [Fact]
    public async Task GetVenueByIdOrSlugAsync_UnknownSlug_ReturnsNull()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IVenueService venueService = scope.ServiceProvider.GetRequiredService<IVenueService>();

        Venue? found = await venueService.GetVenueByIdOrSlugAsync($"unknown-slug-{Guid.NewGuid():N}");

        Assert.Null(found);
    }
}
