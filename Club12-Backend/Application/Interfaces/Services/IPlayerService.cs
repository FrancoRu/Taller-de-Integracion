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
    /// Creates a new Player and registers them to <paramref name="tournamentId"/>
    /// on the player's team (see <see cref="Domain.Entities.Models.PlayerTeamRegistration"/>) —
    /// the roster membership fact that makes the player show up in that
    /// season's team roster.
    /// </summary>
    /// <param name="playerEntity">The Player entity to create.</param>
    /// <param name="tournamentId">
    /// The season (Tournament) to register the player's team assignment to —
    /// normally the player's team's current TournamentId.
    /// </param>
    /// <returns>The created Player.</returns>
    Task<Player> CreatePlayerAsync(Player playerEntity, Guid tournamentId);

    /// <summary>
    /// Retrieves a Player by its id.
    /// </summary>
    /// <param name="playerId">The id of the Player to retrieve.</param>
    /// <returns>The Player with the specified id, or null if not found.</returns>
    Task<Player?> GetPlayerByIdAsync(Guid playerId);

    /// <summary>
    /// Updates a Player and keeps their season-scoped roster registration in
    /// sync: if the player's TeamId changed, either the registration for
    /// <paramref name="tournamentId"/> is moved to the new team, or (if none
    /// exists yet for that season) a new one is created.
    /// </summary>
    /// <param name="playerEntity">The Player entity, with updated fields already applied.</param>
    /// <param name="tournamentId">The season (Tournament) the player's current team belongs to.</param>
    Task UpdatePlayerAsync(Player playerEntity, Guid tournamentId);

    Task DeletePlayerAsync(Guid id);

    /// <summary>
    /// Retrieves players with pagination and filtering.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the players.</returns>
    Task<PaginatedResponse<Player>> GetAllPlayersAsync(PlayerFilterRequestBase filter);
}
