using Application.DTOs.Abstract.Response;
using Application.DTOs.Player.Request;
using Domain.Entities.Models;
using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents a service for managing Players.
/// </summary>
public interface IPlayerService
{
    /// <summary>
    /// Creates a new Player.
    /// </summary>
    /// <param name="playerEntity">The Player entity to create.</param>
    /// <returns>The created Player.</returns>
    Task<Player> CreatePlayerAsync(Player playerEntity);

    /// <summary>
    /// Retrieves a Player by its id.
    /// </summary>
    /// <param name="playerId">The id of the Player to retrieve.</param>
    /// <returns>The Player with the specified id, or null if not found.</returns>
    Task<Player?> GetPlayerByIdAsync(Guid playerId);

    Task UpdatePlayerAsync(Player playerEntity);

    Task DeletePlayerAsync(Guid id);

    /// <summary>
    /// Retrieves players with pagination and filtering.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the players.</returns>
    Task<PaginatedResponse<Player>> GetAllPlayersAsync(PlayerFilterRequestBase filter);
}
