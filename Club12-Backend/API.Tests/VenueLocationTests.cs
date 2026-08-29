using Application.Interfaces.Services;

using Domain.Entities.Models;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies a Venue can carry optional geographic coordinates (Latitude /
/// Longitude). Coordinates are persisted alongside the venue and round-trip
/// through the service layer, so the public map link can be built from them.
/// Exercised at the service layer for the same Supabase-boot reason documented
/// in VenueServiceSlugTests.
/// </summary>
public class VenueLocationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public VenueLocationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateVenueAsync_PersistsLatitudeAndLongitude()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IVenueService venueService = scope.ServiceProvider.GetRequiredService<IVenueService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Venue created = await venueService.CreateVenueAsync(new Venue
        {
            Name = $"Cancha Geo {Guid.NewGuid():N}",
            Slug = null!,
            Address = "Av. Siempre Viva 742",
            Latitude = -34.603722,
            Longitude = -58.381592,
            CreatedBy = "test",
        });

        Venue? persisted = await db.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == created.Id);

        Assert.NotNull(persisted);
        Assert.Equal(-34.603722, persisted!.Latitude);
        Assert.Equal(-58.381592, persisted.Longitude);
    }

    [Fact]
    public async Task CreateVenueAsync_WithoutCoordinates_LeavesThemNull()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IVenueService venueService = scope.ServiceProvider.GetRequiredService<IVenueService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Venue created = await venueService.CreateVenueAsync(new Venue
        {
            Name = $"Cancha SinGeo {Guid.NewGuid():N}",
            Slug = null!,
            Address = "Calle Falsa 123",
            CreatedBy = "test",
        });

        Venue? persisted = await db.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == created.Id);

        Assert.NotNull(persisted);
        Assert.Null(persisted!.Latitude);
        Assert.Null(persisted.Longitude);
    }
}
