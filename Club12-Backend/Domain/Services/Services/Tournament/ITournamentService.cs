using Entities.DTOs.Abstract;
using Entities.DTOs.Tournament;
using Entities.Models.TournamentEntity;

namespace Services.Services.TournamentService;

/// <summary>
/// Represents a service for managing Tournaments.
/// </summary>
public interface ITournamentService
{
    /// <summary>
    /// Creates a new Tournament.
    /// </summary>
    /// <param name="tournamentEntity">The Tournament entity to create.</param>
    /// <returns>The created Tournament.</returns>
    Tournament CreateTournament(Tournament tournamentEntity);

    /// <summary>
    /// Retrieves a Tournament by its id.
    /// </summary>
    /// <param name="tournamentId">The id of the Tournament to retrieve.</param>
    /// <returns>The Tournament with the specified id, or null if not found.</returns>
    Tournament? GetTournamentById(Guid tournamentId);

    /// <summary>
    /// Updates a Tournament asynchronously.
    /// </summary>
    /// <param name="tournamentEntity">The Tournament to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateTournament(Tournament tournamentEntity);

    /// <summary>
    /// Deletes a Tournament.
    /// </summary>
    /// <param name="tournamentEntity">The Tournament to delete.</param>
    void DeleteTournament(Tournament tournamentEntity);

    /// <summary>
    /// Retrieves tournaments with pagination and filtering.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the tournaments.</returns>
    Task<PaginatedResponse<Tournament>> GetAllTournamentsAsync(GetTournamentsFilteredRequest filter);
}
