using Entities.DTOs.Abstract;
using Entities.DTOs.Tournament;
using Entities.Models.Tournaments;

namespace Services.Services.Tournaments;

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
    /// Retrieves a Tournament by its id asynchronously.
    /// </summary>
    /// <param name="tournamentId">The id of the Tournament to retrieve.</param>
    /// <returns>The Tournament with the specified id, or null if not found.</returns>
    Task<Tournament?> GetTournamentByIdAsync(Guid tournamentId);

    /// <summary>
    /// Updates a Tournament asynchronously.
    /// </summary>
    /// <param name="tournamentEntity">The Tournament to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateTournamentAsync(Tournament tournamentEntity);

    /// <summary>
    /// Deletes a Tournament asynchronously.
    /// </summary>
    /// <param name="tournamentEntity">The Tournament to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    Task<bool> DeleteTournamentAsync(Tournament tournamentEntity);

    /// <summary>
    /// Retrieves tournaments with pagination and filtering asynchronously.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the tournaments.</returns>
    Task<PaginatedResponse<Tournament>> GetAllTournamentsAsync(GetTournamentsFilteredRequest filter);
}
