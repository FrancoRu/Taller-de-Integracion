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
    /// Retrieves a Player by its id or its slug. The value is treated as an id
    /// when it parses as a GUID, otherwise it is looked up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The player's GUID id or its slug.</param>
    /// <returns>The matching player, or null if not found.</returns>
    Task<Player?> GetPlayerByIdOrSlugAsync(string idOrSlug);

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
    /// Registers a player onto a team's roster for a specific tournament
    /// (season), enforcing the HU-54 roster invariants:
    /// <list type="bullet">
    /// <item>a player cannot be registered to two teams in the same tournament;</item>
    /// <item>the team's roster may not exceed the configured maximum size;</item>
    /// <item>a jersey number (dorsal), when given, must be unique within the
    /// team + tournament.</item>
    /// </list>
    /// Re-registering the same player to the same team updates their dorsal
    /// (idempotent), so this is safe to call for both add and edit.
    /// </summary>
    /// <param name="playerId">The player to register.</param>
    /// <param name="teamId">The team to register the player onto.</param>
    /// <param name="tournamentId">The tournament (season) the registration belongs to.</param>
    /// <param name="jerseyNumber">The player's dorsal for this team/season, or null.</param>
    /// <returns>The created or updated registration.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when any of the roster invariants is violated.
    /// </exception>
    Task<PlayerTeamRegistration> RegisterPlayerToTeamAsync(
        Guid playerId, Guid teamId, Guid tournamentId, int? jerseyNumber = null);

    /// <summary>
    /// Retrieves players with pagination and filtering.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the players.</returns>
    Task<PaginatedResponse<Player>> GetAllPlayersAsync(PlayerFilterRequestBase filter);
}
