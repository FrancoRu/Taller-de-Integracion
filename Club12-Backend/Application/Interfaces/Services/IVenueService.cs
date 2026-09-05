using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IVenueService
{
    /// <summary>
    /// Creates a venue and generates its unique slug from the name.
    /// </summary>
    /// <param name="venueEntity">The Venue entity to create.</param>
    /// <returns>The created Venue.</returns>
    Task<Venue> CreateVenueAsync(Venue venueEntity);

    Task<Venue?> GetVenueByIdAsync(Guid venueId);

    /// <summary>
    /// Retrieves a Venue by its id or its slug asynchronously. The value is
    /// treated as an id when it parses as a GUID, otherwise it is looked up as
    /// a slug.
    /// </summary>
    /// <param name="idOrSlug">The venue's GUID id or its slug.</param>
    /// <returns>The matching venue, or null if not found.</returns>
    Task<Venue?> GetVenueByIdOrSlugAsync(string idOrSlug);

    Task UpdateVenueAsync(Venue venueEntity);

    /// <summary>
    /// Deletes a venue. Blocked while any match still references it, so a
    /// match is never left without a venue.
    /// </summary>
    /// <param name="id">The unique identifier of the venue to delete.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the venue is referenced by one or more matches.
    /// </exception>
    Task DeleteVenueAsync(Guid id);

    Task<IEnumerable<Venue>> GetAllVenuesAsync();
}
