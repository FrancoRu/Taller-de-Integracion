using Entities.DTOs.Abstract;
using Entities.DTOs.Match;
using Entities.Models.MatchEntity;

namespace Services.Services.MatchService;

/// <summary>
/// Represents a service for managing matches.
/// </summary>
public interface IMatchService
{
    /// <summary>
    /// Creates a new match.
    /// </summary>
    /// <param name="matchEntity">The match entity to create.</param>
    /// <returns>The created match.</returns>
    Match CreateMatch(Match matchEntity);

    /// <summary>
    /// Retrieves a match by its id.
    /// </summary>
    /// <param name="matchId">The id of the match to retrieve.</param>
    /// <returns>The match with the specified id, or null if not found.</returns>
    Match? GetMatchById(Guid matchId);

    /// <summary>
    /// Updates a match asynchronously.
    /// </summary>
    /// <param name="matchEntity">The match to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateMatchAsync(Match matchEntity);

    /// <summary>
    /// Deletes a match.
    /// </summary>
    /// <param name="matchEntity">The match to delete.</param>
    void DeleteMatch(Match matchEntity);

    /// <summary>
    /// Retrieves a paginated list of matches based on filtering criteria.
    /// </summary>
    /// <param name="filter">The filtering criteria.</param>
    /// <returns>A paginated response containing the filtered matches.</returns>
    Task<PaginatedResponse<Match>> GetAllMatchesAsync(GetMatchesFilteredRequest filter);
}
