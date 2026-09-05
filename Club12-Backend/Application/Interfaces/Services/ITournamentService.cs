using Application.DTOs.Abstract.Response;
using Application.DTOs.Tournament.Request;
using Application.DTOs.Tournament.Response;

using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface ITournamentService
{
    /// <summary>
    /// Creates a tournament and generates its unique slug from the name.
    /// </summary>
    /// <param name="tournamentEntity">The Tournament entity to create.</param>
    /// <returns>The created Tournament.</returns>
    Task<Tournament> CreateTournamentAsync(Tournament tournamentEntity);

    /// <summary>
    /// Creates a whole tournament in a single transaction, reusing the granular create logic.
    /// </summary>
    /// <param name="request">The full wizard payload.</param>
    /// <returns>The created Tournament, including its divisions.</returns>
    Task<Tournament> CreateFullTournamentAsync(CreateFullTournamentRequest request);

    /// <summary>
    /// Adds one division to an already-existing tournament, in a single transaction.
    /// </summary>
    /// <param name="tournament">The already-loaded parent tournament.</param>
    /// <param name="divisionRequest">The division's structure, zone or cross-cup.</param>
    /// <returns>The created Division.</returns>
    Task<Division> AddFullDivisionAsync(Tournament tournament, CreateFullDivisionRequest divisionRequest);

    Task<Tournament?> GetTournamentByIdAsync(Guid tournamentId);

    /// <summary>
    /// Retrieves a Tournament by its id or its slug asynchronously, treating the value as an id when it parses as a GUID and otherwise looking it up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The Tournament's GUID id or its slug.</param>
    /// <returns>The matching Tournament, or null if not found.</returns>
    Task<Tournament?> GetTournamentByIdOrSlugAsync(string idOrSlug);

    /// <summary>
    /// Updates a Tournament without changing the lifecycle status; use ChangeStatusAsync for that.
    /// </summary>
    /// <param name="tournamentEntity">The Tournament to update.</param>
    Task UpdateTournamentAsync(Tournament tournamentEntity);

    /// <summary>
    /// Moves a tournament to a new lifecycle status, enforcing the forward-only state machine.
    /// </summary>
    /// <param name="tournamentId">The id of the tournament to transition.</param>
    /// <param name="newStatus">The target lifecycle status.</param>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">No tournament exists with the given id.</exception>
    /// <exception cref="System.InvalidOperationException">The requested transition is not allowed by the state machine.</exception>
    Task ChangeStatusAsync(Guid tournamentId, TournamentStatus newStatus);

    /// <summary>
    /// Reports whether a tournament can be COMPLETED once started, and lists the blocking issues when it cannot.
    /// </summary>
    /// <param name="tournamentId">The id of the tournament to evaluate.</param>
    /// <returns>The completability report, with CanStart and Issues.</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">No tournament exists with the given id.</exception>
    Task<TournamentCompletabilityResponse> GetCompletabilityAsync(Guid tournamentId);

    /// <summary>
    /// Deletes a tournament, blocked once it has started or has any finished match, so competitive history is never silently destroyed.
    /// </summary>
    /// <param name="id">The id of the Tournament to delete.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the tournament has already started or has any finished match.
    /// </exception>
    Task DeleteTournamentAsync(Guid id);

    /// <summary>
    /// Retrieves tournaments with pagination and filtering asynchronously.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the tournaments.</returns>
    Task<PaginatedResponse<Tournament>> GetAllTournamentsAsync(GetTournamentsFilteredRequest filter);
}
