using Entities.Models.VenueEntity;

namespace Services.Services.VenueService;

/// <summary>
/// Represents a service for managing Venues.
/// </summary>
public interface IVenueService
{
    /// <summary>
    /// Creates a new Venue.
    /// </summary>
    /// <param name="venueEntity">The Venue entity to create.</param>
    /// <returns>The created Venue.</returns>
    Venue CreateVenue(Venue venueEntity);

    /// <summary>
    /// Retrieves a Venue by its id.
    /// </summary>
    /// <param name="venueId">The id of the Venue to retrieve.</param>
    /// <returns>The Venue with the specified id, or null if not found.</returns>
    Venue? GetVenueById(Guid venueId);

    /// <summary>
    /// Updates a Venue asynchronously.
    /// </summary>
    /// <param name="venueEntity">The Venue to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateVenueAsync(Venue venueEntity);

    /// <summary>
    /// Deletes a Venue.
    /// </summary>
    /// <param name="venueEntity">The Venue to delete.</param>
    void DeleteVenue(Venue venueEntity);

    /// <summary>
    /// Retrieves all venus
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the venues.</returns>
    Task<IEnumerable<Venue>> GetAllVenuesAsync();
}
