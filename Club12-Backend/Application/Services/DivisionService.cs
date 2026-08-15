using Application.DTOs.Abstract.Response;
using Application.DTOs.Divisions.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;
using Domain.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Service class responsible for managing Division entities.
/// Provides methods for creating, updating, deleting, and retrieving divisions,
/// as well as fetching paginated lists of divisions with filtering support.
/// </summary>
public class DivisionService(IDivisionRepository divisionRepository) : IDivisionService
{
    /// <summary>
    /// Creates a new division entity asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division entity to create.</param>
    /// <returns>The created Division entity.</returns>
    public async Task<Division> CreateDivisionAsync(Division divisionEntity)
    {
        await divisionRepository.AddAsync(divisionEntity);
        return divisionEntity;
    }

    /// <summary>
    /// Deletes a division entity by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the division to delete.</param>
    public async Task DeleteDivisionAsync(Guid id)
        => await divisionRepository.RemoveAsync(division => division.Id == id);
    
    /// <summary>
    /// Updates an existing division entity asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division entity with updated values.</param>
    public async Task UpdateDivisionAsync(Division divisionEntity)
        => await divisionRepository.UpdateAsync(divisionEntity);

    /// <summary>
    /// Retrieves a division entity by its unique identifier asynchronously.
    /// Returns only the basic division data.
    /// </summary>
    /// <param name="divisionId">The unique identifier of the division.</param>
    /// <returns>The Division entity if found; otherwise, null.</returns>
    public async Task<Division?> GetSimpleDivisionByIdAsync(Guid divisionId)
        => await divisionRepository.GetByIdAsync(divisionId);

    /// <summary>
    /// Retrieves a division entity by its unique identifier asynchronously,
    /// including related tournament and stages data.
    /// </summary>
    /// <param name="divisionId">The unique identifier of the division.</param>
    /// <returns>The Division entity with related data if found; otherwise, null.</returns>
    public async Task<Division?> GetFullDivisionByIdAsync(Guid divisionId)
        => await divisionRepository.GetByIdAsync(divisionId, includes: [division => division.Tournament, division => division.Stages]);

    /// <summary>
    /// Retrieves a paginated and filtered list of division entities asynchronously.
    /// </summary>
    /// <param name="filter">The filter and pagination parameters.</param>
    /// <returns>A PaginatedResponse{Division} containing the filtered divisions.</returns>
    public async Task<PaginatedResponse<Division>> GetAllDivisionsAsync(GetDivisionsFilteredRequest filter)
    {
        Expression<Func<Division, bool>> expression = QueryableExtensions.ConstructFilterExpression<Division, GetDivisionsFilteredRequest>(filter);
        IEnumerable<Division> filteredDivisions = await divisionRepository.FindAsync(expression, filter: filter);

        int totalCount = await divisionRepository.CountAsync(expression);

        return new PaginatedResponse<Division>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredDivisions
        };
    }
}
