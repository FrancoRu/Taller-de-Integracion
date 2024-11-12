using Entities.DTOs.Abstract;
using Entities.DTOs.Division;
using Entities.Models.DivisionEntity;

namespace Services.Services.DivisionService;

/// <summary>
/// Represents a service for managing divisions.
/// </summary>
public interface IDivisionService
{
    /// <summary>
    /// Creates a new division.
    /// </summary>
    /// <param name="divisionEntity">The division entity to create.</param>
    /// <param name="userId">The ID of the user creating the division.</param>
    /// <returns>The created division.</returns>
    Division CreateDivision(Division divisionEntity);

    /// <summary>
    /// Retrieves a division by its id.
    /// </summary>
    /// <param name="divisionId">The id of the division to retrieve.</param>
    /// <returns>The division with the specified id, or null if not found.</returns>
    Division? GetDivisionById(Guid divisionId);

    /// <summary>
    /// Deletes a division.
    /// </summary>
    /// <param name="divisionEntity">The division to delete.</param>
    void DeleteDivision(Division divisionEntity);

    /// <summary>
    /// Updates a division asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division to update.</param>
    /// <param name="userId">The ID of the user updating the division.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateDivisionAsync(Division divisionEntity);

    /// <summary>
    /// Retrieves divisions with pagination and filtering.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the divisions.</returns>
    Task<PaginatedResponse<Division>> GetAllDivisionsAsync(GetDivisionsFilteredRequest filter);
}
