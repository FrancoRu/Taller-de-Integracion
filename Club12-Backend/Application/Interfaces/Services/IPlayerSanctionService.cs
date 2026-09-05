using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerSanction.Request;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IPlayerSanctionService
{
    /// <summary>
    /// Creates a sanction and assigns it a unique slug derived from its subject's resolved name and issue date, so a player, team, or staff sanction all get a readable slug regardless of subject type.
    /// </summary>
    /// <param name="playerSanctionEntity">The player sanction entity to create.</param>
    /// <returns>The created player sanction.</returns>
    Task<PlayerSanction> CreatePlayerSanctionAsync(PlayerSanction playerSanctionEntity);

    Task<PlayerSanction?> GetPlayerSanctionByIdAsync(Guid playerSanctionId);

    /// <summary>
    /// Retrieves a player sanction by its id or its slug, treating the value as an id when it parses as a GUID and otherwise looking it up as a slug.
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
    /// Computes how many FECHAS, jornadas, of a sanction are still to be served, based on the subject team's finished rounds since the sanction was issued.
    /// </summary>
    /// <param name="sanction">The sanction to evaluate.</param>
    /// <returns>The fechas remaining, or null when not computable by rounds.</returns>
    Task<int?> GetFechasRemainingAsync(PlayerSanction sanction);

    /// <summary>
    /// Determines whether a player has any active sanction, one with fechas still to be served, so eligibility stays consistent with the fechas-based rule.
    /// </summary>
    /// <param name="playerId">The player to check.</param>
    /// <returns>True when the player has at least one active sanction.</returns>
    Task<bool> HasActiveSanctionAsync(Guid playerId);

    /// <summary>
    /// Retrieves player sanctions with pagination and filtering asynchronously.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the player sanctions.</returns>
    Task<PaginatedResponse<PlayerSanction>> GetPlayerSanctionsAsync(GetPlayerSanctionsFilteredRequest filter);

    /// <summary>
    /// Resolves the human-readable subject of a sanction into the three mutually-exclusive display fields the response exposes.
    /// </summary>
    /// <param name="sanction">The sanction whose subject to resolve.</param>
    /// <returns>
    /// The player, team and staff display names; exactly one is non-null for a
    /// well-formed sanction, the others are null.
    /// </returns>
    Task<(string? PlayerFullName, string? TeamName, string? StaffName)> ResolveSubjectAsync(PlayerSanction sanction);
}
