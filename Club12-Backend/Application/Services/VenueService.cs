using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Helper.Slug;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Manages venues, blocking deletion while any match still references the venue.
/// </summary>
public class VenueService(IVenueRepository venueRepository, IMatchRepository matchRepository) : IVenueService
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
    /// Retrieves a venue by id or slug, auto-detecting which one was passed via GUID parsing.
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

    /// <summary>
    /// Deletes a venue. Blocked while any match still references it.
    /// </summary>
    /// <param name="id">The unique identifier of the venue to delete.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown as a 409 when the venue is referenced by one or more matches.
    /// </exception>
    public async Task DeleteVenueAsync(Guid id)
    {
        // Integrity: a venue referenced by one or more matches cannot be deleted, since that would leave those matches without their venue.
        bool referencedByMatches = await matchRepository.ExistsAsync(match => match.VenueId == id);

        if (referencedByMatches)
        {
            throw new InvalidOperationException(ErrorMessages.Venue.ReferencedByMatches);
        }

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
