using Club12.Entities.DivisionEntity;

namespace Club12.Services.Divisions;

/// <summary>
/// Represents a service for managing divisions.
/// </summary>
public interface IDivisionService
{
    /// <summary>
    /// Creates a new division.
    /// </summary>
    /// <param name="divisionEntity">The division entity to create.</param>
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
    /// <param name="division">The division to delete.</param>
    void DeleteDivision(Division division);

    /// <summary>
    /// Updates a division asynchronously.
    /// </summary>
    /// <param name="division">The division to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateDivision(Division division);
}
