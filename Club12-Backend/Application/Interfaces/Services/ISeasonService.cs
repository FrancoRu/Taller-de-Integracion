using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents a service for managing Seasons ("Temporadas").
/// </summary>
public interface ISeasonService
{
    /// <summary>
    /// Creates a new Season asynchronously, generating its unique slug from the name.
    /// </summary>
    /// <param name="seasonEntity">The Season entity to create.</param>
    /// <returns>The created Season.</returns>
    Task<Season> CreateSeasonAsync(Season seasonEntity);

    /// <summary>
    /// Retrieves a Season by its id asynchronously, including its tournaments.
    /// </summary>
    /// <param name="seasonId">The id of the Season to retrieve.</param>
    /// <returns>The Season with the specified id, or null if not found.</returns>
    Task<Season?> GetSeasonByIdAsync(Guid seasonId);

    /// <summary>
    /// Retrieves a Season by its id or its slug asynchronously. The value is
    /// treated as an id when it parses as a GUID, otherwise it is looked up as
    /// a slug.
    /// </summary>
    /// <param name="idOrSlug">The season's GUID id or its slug.</param>
    /// <returns>The matching season, or null if not found.</returns>
    Task<Season?> GetSeasonByIdOrSlugAsync(string idOrSlug);

    /// <summary>
    /// Updates a Season asynchronously.
    /// </summary>
    /// <param name="seasonEntity">The Season to update.</param>
    Task UpdateSeasonAsync(Season seasonEntity);

    /// <summary>
    /// Deletes a Season by its id asynchronously.
    /// </summary>
    /// <param name="id">The id of the Season to delete.</param>
    Task DeleteSeasonAsync(Guid id);

    /// <summary>
    /// Retrieves all seasons asynchronously, including their tournaments.
    /// </summary>
    /// <returns>All seasons.</returns>
    Task<IEnumerable<Season>> GetAllSeasonsAsync();
}
