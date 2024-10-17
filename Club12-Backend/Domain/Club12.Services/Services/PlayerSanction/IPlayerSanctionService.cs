using Club12.Entities.SanctionPlayerEntity;
using Club12.Services.DTOs.Abstract;

namespace Club12.Services.Services.PlayerSanctionService;

/// <summary>
/// Represents a service for managing player sanctions.
/// </summary>
public interface IPlayerSanctionService
{
    /// <summary>
    /// Creates a new player sanction.
    /// </summary>
    /// <param name="playerSanctionEntity">The player sanction entity to create.</param>
    /// <returns>The created player sanction.</returns>
    Task<PlayerSanction> CreatePlayerSanctionAsync(PlayerSanction playerSanctionEntity);

    /// <summary>
    /// Retrieves a player sanction by its ID.
    /// </summary>
    /// <param name="playerSanctionId">The ID of the player sanction to retrieve.</param>
    /// <returns>The player sanction with the specified ID, or null if not found.</returns>
    PlayerSanction? GetPlayerSanctionByIdAsync(Guid playerSanctionId);

    /// <summary>
    /// Deletes a player sanction.
    /// </summary>
    /// <param name="playerSanctionEntity">The player sanction to delete.</param>
    Task DeletePlayerSanctionAsync(PlayerSanction playerSanctionEntity);

    /// <summary>
    /// Updates a player sanction asynchronously.
    /// </summary>
    /// <param name="playerSanctionEntity">The player sanction to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdatePlayerSanctionAsync(PlayerSanction playerSanctionEntity);

    /// <summary>
    /// Retrieves expired player sanctions as of a specific date.
    /// </summary>
    /// <param name="date">The date to check for expired sanctions.</param>
    /// <returns>A collection of expired player sanctions.</returns>
    Task<IEnumerable<PlayerSanction>> GetExpiredSanctionsAsync(DateTime date);

    /// <summary>
    /// Retrieves player sanctions with pagination and filtering.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the player sanctions.</returns>
    Task<PaginatedResponse<PlayerSanction>> GetPlayerSanctionsAsync(GetPlayerSanctionsFilteredRequest filter);
}
