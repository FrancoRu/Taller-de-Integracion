using Application.DTOs.Abstract.Request;
using Domain.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories;


/// <summary>
/// Generic repository interface for common CRUD operations.
/// </summary>
public interface IGenericRepository<TEntity> where TEntity : EntityBase
{
    /// <summary>
    /// Gets an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <returns>The entity if found, otherwise null.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, IEnumerable<Expression<Func<TEntity, object>>>? includes = null);

    IQueryable<TEntity> GetQueryable();

    /// <summary>
    /// Gets all entities of type <typeparamref name="TEntity"/>.
    /// </summary>
    /// <returns>A collection of all entities.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// Finds entities matching the given predicate.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <returns>A collection of matching entities.</returns>
    Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate, 
        IEnumerable<Expression<Func<TEntity, object>>>? includes = null, 
        PaginatedFilterRequest? filter = null);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Adds a range of entities to the repository.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Removes a single entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(TEntity entity);

    /// <summary>
    /// Removes all entities matching the given expression asynchronously using ExecuteDeleteAsync.
    /// </summary>
    /// <param name="expression">The filter expression.</param>
    /// <returns>The number of entities deleted.</returns>
    Task<int> RemoveAsync(Expression<Func<TEntity, bool>> expression);

    /// <summary>
    /// Counts the number of entities matching the given predicate, or all if predicate is null.
    /// </summary>
    /// <param name="predicate">The filter expression (optional).</param>
    /// <returns>The count of matching entities.</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);

    /// <summary>
    /// Checks if any entity matches the given predicate.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <returns>True if any entity matches, otherwise false.</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

    Task UpdateRangeAsync(IEnumerable<TEntity> entities);
    Task UpdateAsync(TEntity entity);
}
