using Domain.Entities.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents a service for managing Venues.
/// </summary>
public interface IVenueService
{
    /// <summary>
    /// Creates a new Venue asynchronously.
    /// </summary>
    /// <param name="venueEntity">The Venue entity to create.</param>
    /// <returns>The created Venue.</returns>
    Task<Venue> CreateVenueAsync(Venue venueEntity);

    /// <summary>
    /// Retrieves a Venue by its id asynchronously.
    /// </summary>
    /// <param name="venueId">The id of the Venue to retrieve.</param>
    /// <returns>The Venue with the specified id, or null if not found.</returns>
    Task<Venue?> GetVenueByIdAsync(Guid venueId);

    /// <summary>
    /// Updates a Venue asynchronously.
    /// </summary>
    /// <param name="venueEntity">The Venue to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task UpdateVenueAsync(Venue venueEntity);

    Task DeleteVenueAsync(Guid id);

    /// <summary>
    /// Retrieves all venues asynchronously.
    /// </summary>
    /// <returns>A paginated response containing the venues.</returns>
    Task<IEnumerable<Venue>> GetAllVenuesAsync();
}
