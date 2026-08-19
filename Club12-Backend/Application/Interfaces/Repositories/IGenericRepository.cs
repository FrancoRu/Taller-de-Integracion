using Application.DTOs.Abstract.Request;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories;


/// <summary>
/// Generic repository interface for common CRUD operations on entities.
/// Provides methods for retrieving, adding, updating, and removing entities,
/// as well as querying and checking existence or count of entities.
/// </summary>
/// <typeparam name="TEntity">The entity type, which must inherit from EntityBase.</typeparam>
public interface IGenericRepository<TEntity> where TEntity : EntityBase
{
    /// <summary>
    /// Gets an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="includes">Optional navigation property expressions to include related data.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the entity if found, otherwise null.
    /// </returns>
    Task<TEntity?> GetByIdAsync(Guid id, IEnumerable<Expression<Func<TEntity, object>>>? includes = null);

    /// <summary>
    /// Returns a queryable collection of entities for advanced querying scenarios.
    /// </summary>
    /// <remarks>
    /// This method exposes the underlying IQueryable{TEntity} for LINQ operations.
    /// </remarks>
    IQueryable<TEntity> GetQueryable();

    /// <summary>
    /// Gets all entities of type <typeparamref name="TEntity"/>.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a collection of all entities.
    /// </returns>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// Finds entities matching the given predicate.
    /// </summary>
    /// <param name="predicate">The filter expression to match entities.</param>
    /// <param name="includes">Optional navigation property expressions to include related data.</param>
    /// <param name="filter">Optional pagination and filtering request.</param>
    /// <param name="asSplitQuery">
    /// Set true when including 2+ collection navigations in the same query —
    /// otherwise EF Core joins them into one cartesian-product result set,
    /// which is both slow and, combined with pagination, can return
    /// incorrect rows.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a collection of matching entities.
    /// </returns>
    Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        IEnumerable<Expression<Func<TEntity, object>>>? includes = null,
        PaginatedFilterRequest? filter = null,
        bool asSplitQuery = false);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the added entity.
    /// </returns>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Adds a range of entities to the repository.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the added entities.
    /// </returns>
    Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Removes a single entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(TEntity entity);

    /// <summary>
    /// Removes all entities matching the given expression asynchronously using ExecuteDeleteAsync.
    /// </summary>
    /// <param name="expression">The filter expression to match entities for removal.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the number of entities deleted.
    /// </returns>
    Task<int> RemoveAsync(Expression<Func<TEntity, bool>> expression);

    /// <summary>
    /// Counts the number of entities matching the given predicate, or all if predicate is null.
    /// </summary>
    /// <param name="predicate">The filter expression (optional).</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the count of matching entities.
    /// </returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);

    /// <summary>
    /// Checks if any entity matches the given predicate.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is true if any entity matches, otherwise false.
    /// </returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Updates a range of entities in the repository.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Updates a single entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    Task UpdateAsync(TEntity entity);
}