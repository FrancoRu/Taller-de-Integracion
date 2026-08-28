using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Helper.Slug;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

public class VenueService(IVenueRepository venueRepository) : IVenueService
{
    public async Task<Venue> CreateVenueAsync(Venue venueEntity)
    {
        venueEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            venueEntity.Name,
            candidate => venueRepository.ExistsAsync(venue => venue.Slug == candidate));

        await venueRepository.AddAsync(venueEntity);
        return venueEntity;
    }

    public async Task<Venue?> GetVenueByIdAsync(Guid venueId)
    {
        return await venueRepository.GetByIdAsync(venueId);
    }

    /// <summary>
    /// Retrieves a venue by its id or its slug. The value is treated as an id
    /// when it parses as a GUID, otherwise it is looked up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The venue's GUID id or its slug.</param>
    /// <returns>The matching venue, or null if not found.</returns>
    public async Task<Venue?> GetVenueByIdOrSlugAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out Guid venueId))
        {
            return await GetVenueByIdAsync(venueId);
        }

        IEnumerable<Venue> matches = await venueRepository.FindAsync(venue => venue.Slug == idOrSlug);
        return matches.FirstOrDefault();
    }

    public async Task DeleteVenueAsync(Guid id)
    {
        await venueRepository.RemoveAsync(venue => venue.Id == id);
    }

    public async Task UpdateVenueAsync(Venue venueEntity)
    {
        await venueRepository.UpdateAsync(venueEntity);
    }

    public async Task<IEnumerable<Venue>> GetAllVenuesAsync()
    {
        return await venueRepository.FindAsync(venue => true);
    }
}
