using Entities.DTOs.Abstract;
using Entities.DTOs.Division;
using Entities.Models.DivisionEntity;
using Entities.Models.TopScorerModel;

namespace Services.Services.DivisionService;

/// <summary>
/// Represents a service for managing divisions.
/// </summary>
public interface IDivisionService
{
    /// <summary>
    /// Creates a new division asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division entity to create.</param>
    /// <param name="userId">The ID of the user creating the division.</param>
    /// <returns>The created division.</returns>
    Task<Division> CreateDivisionAsync(Division divisionEntity);

    /// <summary>
    /// Retrieves a division by its id asynchronously.
    /// </summary>
    /// <param name="divisionId">The id of the division to retrieve.</param>
    /// <returns>The division with the specified id, or null if not found.</returns>
    Task<Division?> GetDivisionByIdAsync(Guid divisionId);

    /// <summary>
    /// Deletes a division asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    Task<bool> DeleteDivisionAsync(Division divisionEntity);

    /// <summary>
    /// Updates a division asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division to update.</param>
    /// <param name="userId">The ID of the user updating the division.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateDivisionAsync(Division divisionEntity);

    /// <summary>
    /// Retrieves divisions with pagination and filtering asynchronously.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the divisions.</returns>
    Task<PaginatedResponse<Division>> GetAllDivisionsAsync(GetDivisionsFilteredRequest filter);

    /// <summary>
    /// Gets the division with the specified ID, including the teams, matches, and stats asynchronously.
    /// </summary>
    /// <param name="divisionId">The id of the division to retrieve with stats.</param>
    /// <returns>A division with its positions table.</returns>
    Task<Division?> GetDivisionWithStatsAsync(Guid divisionId);

    /// <summary>
    /// Gets the top scorers for the division asynchronously.
    /// </summary>
    /// <param name="divisionId">The id of the division to get top scorers for.</param>
    /// <returns>A list of top scorers for the division or an empty list or null if the division is not found.</returns>
    Task<List<TopScorer>?> GetTopScorersByDivisionAsync(Guid divisionId);
}
