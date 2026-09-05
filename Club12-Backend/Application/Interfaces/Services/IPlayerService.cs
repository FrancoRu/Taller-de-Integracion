using Application.DTOs.Abstract.Response;
using Application.DTOs.Player.Request;

using Domain.Entities.Models;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IPlayerService
{
    /// <summary>
    /// Creates a new Player and registers them to tournamentId on the player's team.
    /// </summary>
    /// <param name="playerEntity">The Player entity to create.</param>
    /// <param name="tournamentId">
    /// The season, Tournament, to register the player's team assignment to, normally the player's team's current TournamentId.
    /// </param>
    /// <returns>The created Player.</returns>
    Task<Player> CreatePlayerAsync(Player playerEntity, Guid tournamentId);

    Task<Player?> GetPlayerByIdAsync(Guid playerId);

    /// <summary>
    /// Retrieves a Player by its id or its slug, treating the value as an id when it parses as a GUID and otherwise looking it up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The player's GUID id or its slug.</param>
    /// <returns>The matching player, or null if not found.</returns>
    Task<Player?> GetPlayerByIdOrSlugAsync(string idOrSlug);

    /// <summary>
    /// Updates a Player and keeps their season-scoped roster registration in sync.
    /// </summary>
    /// <param name="playerEntity">The Player entity, with updated fields already applied.</param>
    /// <param name="tournamentId">The season, Tournament, the player's current team belongs to.</param>
    Task UpdatePlayerAsync(Player playerEntity, Guid tournamentId);

    Task DeletePlayerAsync(Guid id);

    /// <summary>
    /// Registers a player onto a team's roster for a specific tournament, enforcing the roster invariants.
    /// </summary>
    /// <param name="playerId">The player to register.</param>
    /// <param name="teamId">The team to register the player onto.</param>
    /// <param name="tournamentId">The tournament, season, the registration belongs to.</param>
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
