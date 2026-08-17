using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerSanction.Request;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents a service for managing player sanctions.
/// </summary>
public interface IPlayerSanctionService
{
    /// <summary>
    /// Creates a new player sanction asynchronously.
    /// </summary>
    /// <param name="playerSanctionEntity">The player sanction entity to create.</param>
    /// <returns>The created player sanction.</returns>
    Task<PlayerSanction> CreatePlayerSanctionAsync(PlayerSanction playerSanctionEntity);

    /// <summary>
    /// Retrieves a player sanction by its ID asynchronously.
    /// </summary>
    /// <param name="playerSanctionId">The ID of the player sanction to retrieve.</param>
    /// <returns>The player sanction with the specified ID, or null if not found.</returns>
    Task<PlayerSanction?> GetPlayerSanctionByIdAsync(Guid playerSanctionId);

    /// <summary>
    /// Retrieves a player sanction by its id or its slug. The value is
    /// treated as an id when it parses as a GUID, otherwise it is looked up
    /// as a slug.
    /// </summary>
    /// <param name="idOrSlug">The sanction's GUID id or its slug.</param>
    /// <returns>The matching player sanction, or null if not found.</returns>
    Task<PlayerSanction?> GetPlayerSanctionByIdOrSlugAsync(string idOrSlug);

    Task DeletePlayerSanctionAsync(Guid id);

    Task UpdatePlayerSanctionAsync(PlayerSanction playerSanctionEntity);

    /// <summary>
    /// Retrieves expired player sanctions as of a specific date asynchronously.
    /// </summary>
    /// <param name="date">The date to check for expired sanctions.</param>
    /// <returns>A collection of expired player sanctions.</returns>
    Task<IEnumerable<PlayerSanction>> GetExpiredSanctionsAsync(DateTime date);

    /// <summary>
    /// Retrieves player sanctions with pagination and filtering asynchronously.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the player sanctions.</returns>
    Task<PaginatedResponse<PlayerSanction>> GetPlayerSanctionsAsync(GetPlayerSanctionsFilteredRequest filter);
}
