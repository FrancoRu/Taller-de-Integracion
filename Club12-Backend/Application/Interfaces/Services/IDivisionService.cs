using Application.DTOs.Abstract.Response;
using Application.DTOs.Divisions.Request;
using Domain.Entities.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

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
    Task<Division?> GetFullDivisionByIdAsync(Guid divisionId);

    Task<Division?> GetSimpleDivisionByIdAsync(Guid divisionId);

    Task DeleteDivisionAsync(Guid id);

    /// <summary>
    /// Updates a division asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division to update.</param>
    /// <param name="userId">The ID of the user updating the division.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task UpdateDivisionAsync(Division divisionEntity);

    /// <summary>
    /// Retrieves divisions with pagination and filtering asynchronously.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the divisions.</returns>
    Task<PaginatedResponse<Division>> GetAllDivisionsAsync(GetDivisionsFilteredRequest filter);
}
