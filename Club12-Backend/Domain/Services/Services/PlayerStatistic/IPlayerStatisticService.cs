using Entities.Models.PlayerStatisticEntity;
using Entities.Models.TopScorerModel;

namespace Services.Services.PlayerStatisticService;

/// <summary>
/// Represents a service for managing player statistics.
/// </summary>
public interface IPlayerStatisticService
{
    /// <summary>
    /// Creates a new player statistic.
    /// </summary>
    /// <param name="playerStatisticEntity">The player statistic entity to create.</param>
    /// <returns>The created player statistic.</returns>
    PlayerStatistic CreatePlayerStatistic(PlayerStatistic playerStatisticEntity);

    /// <summary>
    /// Retrieves a player statistic by its ID.
    /// </summary>
    /// <param name="playerStatisticId">The ID of the player statistic to retrieve.</param>
    /// <returns>The player statistic with the specified ID, or null if not found.</returns>
    PlayerStatistic? GetPlayerStatisticById(Guid playerStatisticId);

    /// <summary>
    /// Deletes a player statistic.
    /// </summary>
    /// <param name="playerStatisticEntity">The player statistic to delete.</param>
    void DeletePlayerStatistic(PlayerStatistic playerStatisticEntity);

    /// <summary>
    /// Updates a player statistic asynchronously.
    /// </summary>
    /// <param name="playerStatisticEntity">The player statistic to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity);

    /// <summary>
    /// Retrieves the top scorers for the given set of matches within a division.
    /// The method aggregates player statistics based on match performance and organizes them by player and match week.
    /// </summary>
    /// <param name="matchIds">A list of match IDs corresponding to the matches within the division.</param>
    /// <param name="totalMatches" >The total number of matches in the division.</param>
    /// <returns>
    /// A list of <see cref="TopScorer"/> models that contain the aggregated performance
    /// data (e.g., total points scored by each player) for the specified matches.
    /// Each <see cref="TopScorer"/> includes the player's information and their performance data grouped by match week.
    /// </returns>
    List<TopScorer> GetTopScorersByDivision(IEnumerable<Guid> matchIds, int totalMatches);
}
