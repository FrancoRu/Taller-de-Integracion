using Application.DTOs.Abstract.Response;
using Application.DTOs.Tournament.Request;

using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents a service for managing Tournaments.
/// </summary>
public interface ITournamentService
{
    /// <summary>
    /// Creates a new Tournament asynchronously.
    /// </summary>
    /// <param name="tournamentEntity">The Tournament entity to create.</param>
    /// <returns>The created Tournament.</returns>
    Task<Tournament> CreateTournamentAsync(Tournament tournamentEntity);

    /// <summary>
    /// HU-38: creates a whole tournament (base fields + every division with its
    /// points, playoff mappings and stages) in a SINGLE transaction, reusing
    /// the granular create logic. A failure at any point rolls the entire graph
    /// back, so no partial tournament is ever left behind. The tournament is
    /// created <see cref="TournamentStatus.OpenForRegistration"/> so its
    /// structure is valid to build (structural creation is part of creation) and
    /// it is ready to register teams; the fixture is still generated later by
    /// the canonical transition to RegistrationClosed.
    /// </summary>
    /// <param name="request">The full wizard payload.</param>
    /// <returns>The created Tournament, including its divisions.</returns>
    Task<Tournament> CreateFullTournamentAsync(CreateFullTournamentRequest request);

    /// <summary>
    /// Retrieves a Tournament by its id asynchronously.
    /// </summary>
    /// <param name="tournamentId">The id of the Tournament to retrieve.</param>
    /// <returns>The Tournament with the specified id, or null if not found.</returns>
    Task<Tournament?> GetTournamentByIdAsync(Guid tournamentId);

    /// <summary>
    /// Retrieves a Tournament by its id or its slug asynchronously. The value is
    /// treated as an id when it parses as a GUID, otherwise it is looked up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The Tournament's GUID id or its slug.</param>
    /// <returns>The matching Tournament, or null if not found.</returns>
    Task<Tournament?> GetTournamentByIdOrSlugAsync(string idOrSlug);

    /// <summary>
    /// Updates a Tournament asynchronously. Does NOT change the lifecycle
    /// status — use <see cref="ChangeStatusAsync"/> for that.
    /// </summary>
    /// <param name="tournamentEntity">The Tournament to update.</param>
    /// <returns>A Tournament entity indicating whether the update was successful.</returns>
    Task UpdateTournamentAsync(Tournament tournamentEntity);

    /// <summary>
    /// Moves a tournament to a new lifecycle status, enforcing the forward-only
    /// state machine (see <see cref="Domain.Enums.TournamentStatusTransitions"/>).
    /// A no-op when the tournament is already in the target status. Transitioning
    /// into <see cref="TournamentStatus.RegistrationClosed"/> also auto-generates
    /// the fixture (matches) for every stage of every division that does not yet
    /// have matches, making it the canonical fixture trigger.
    /// </summary>
    /// <param name="tournamentId">The id of the tournament to transition.</param>
    /// <param name="newStatus">The target lifecycle status.</param>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">No tournament exists with the given id.</exception>
    /// <exception cref="System.InvalidOperationException">The requested transition is not allowed by the state machine.</exception>
    Task ChangeStatusAsync(Guid tournamentId, TournamentStatus newStatus);

    /// <summary>
    /// Deletes a Tournament asynchronously.
    /// </summary>
    /// <param name="id">The id of the Tournament to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    Task DeleteTournamentAsync(Guid id);

    /// <summary>
    /// Retrieves tournaments with pagination and filtering asynchronously.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the tournaments.</returns>
    Task<PaginatedResponse<Tournament>> GetAllTournamentsAsync(GetTournamentsFilteredRequest filter);
}
