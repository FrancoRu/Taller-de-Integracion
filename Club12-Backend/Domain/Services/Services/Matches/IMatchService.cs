using Entities.DTOs.Abstract;
using Entities.DTOs.Match;
using Entities.Models.Matches;
using Entities.Models.PlayoffSeries;
using Entities.Models.Teams;

namespace Services.Services.Matches;

/// <summary>
/// Represents a service for managing matches.
/// </summary>
public interface IMatchService
{
    /// <summary>
    /// Creates a new match asynchronously.
    /// </summary>
    /// <param name="matchEntity">The match entity to create.</param>
    /// <returns>The created match.</returns>
    Task<Match> CreateMatchAsync(Match matchEntity);

    /// <summary>
    /// Retrieves a match by its id asynchronously.
    /// </summary>
    /// <param name="matchId">The id of the match to retrieve.</param>
    /// <returns>The match with the specified id, or null if not found.</returns>
    Task<Match?> GetMatchByIdAsync(Guid matchId);

    /// <summary>
    /// Retrieves a match by its id asynchronously with the scorers.
    /// </summary>
    /// <param name="matchId">The id of the match to retrieve.</param>
    /// <returns>The match with the specified id, or null if not found.</returns>
    Task<Match?> GetMatchByIdWithScorersAsync(Guid matchId);

    /// <summary>
    /// Updates a match asynchronously.
    /// </summary>
    /// <param name="matchEntity">The match to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<Match> UpdateMatchAsync(Match matchEntity);

    /// <summary>
    /// Deletes a match asynchronously.
    /// </summary>
    /// <param name="matchEntity">The match to delete.</param>
    /// <returns>A task representing the asynchronous delete operation and a boolean indicating success.</returns>
    Task<bool> DeleteMatchAsync(Match matchEntity);

    /// <summary>
    /// Retrieves a paginated list of matches based on filtering criteria asynchronously.
    /// </summary>
    /// <param name="filter">The filtering criteria.</param>
    /// <returns>A paginated response containing the filtered matches.</returns>
    Task<PaginatedResponse<Match>> GetAllMatchesAsync(GetMatchesFilteredRequest filter);

    /// <summary>
    /// Generates the fixture (matches) for the given list of teams asynchronously.
    /// </summary>
    /// <param name="divisionId">The division ID to associate with the matches.</param>
    /// <param name="teams">The list of teams for which the fixture should be generated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task GenerateFixtureAsync(Guid divisionId, IEnumerable<Team> teams);

    /// <summary>
    /// Generates playoff matches for the specified division, teams, and playoff series.
    /// </summary>
    /// <param name="divisionId">The ID of the division.</param>
    /// <param name="teams">The teams participating in the playoffs.</param>
    /// <param name="playoffSeries">The playoff series to which the matches belong.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task GeneratePlayoffMatchesAsync(Guid divisionId, IEnumerable<Team> teams, List<PlayoffSerie> playoffSeries);
}
