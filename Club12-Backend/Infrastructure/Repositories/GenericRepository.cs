using Application.DTOs.Abstract.Request;
using Application.Interfaces.Repositories;
using Application.Utils.Extensions;
using Domain.Entities.Models;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

/// <summary>
/// Provides a generic repository implementation for CRUD operations and querying entities in the database.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type managed by the repository. Must inherit from EntityBase.
/// </typeparam>
/// <remarks>
/// This repository uses Entity Framework Core to interact with the database and supports asynchronous operations.
/// It implements IGenericRepository{TEntity} and provides methods for adding, updating, removing,
/// and querying entities, including support for eager loading and pagination.
/// </remarks>
public class GenericRepository<TEntity>(ApplicationDBContext context)
    : IGenericRepository<TEntity> where TEntity : EntityBase
{
    /// <summary>
    /// The database context used for data access.
    /// </summary>
    protected readonly ApplicationDBContext _context = context;

    /// <summary>
    /// The DbSet{TEntity} representing the collection of entities.
    /// </summary>
    protected readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();


    ///<inheritdoc />
    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
    {
        _dbSet.UpdateRange(entities);
        await _context.SaveChangesAsync();
    }

    ///<inheritdoc />
    public virtual async Task UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }
    ///<inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(Guid id, IEnumerable<Expression<Func<TEntity, object>>>? includes = null)
    {
        IQueryable<TEntity> query = _dbSet;

        foreach (Expression<Func<TEntity, object>> include in includes ?? [])
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    ///<inheritdoc />
    public virtual IQueryable<TEntity> GetQueryable() => _dbSet;

    ///<inheritdoc />
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync() => await _dbSet.ToListAsync();

    ///<inheritdoc />
    public virtual async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate, 
        IEnumerable<Expression<Func<TEntity, object>>>? includes = null,
        PaginatedFilterRequest? filter = null)
    {
        IQueryable<TEntity> query = _dbSet.Where(predicate);

        foreach (Expression<Func<TEntity, object>> include in includes ?? [])
        {
            query = query.Include(include);
        }

        if (filter != null)
        {
            query = query.SortBy(filter).Paginate(filter.PageNumber, filter.PageSize);
        }

        return await query.ToListAsync();
    }

    ///<inheritdoc />
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    ///<inheritdoc />
    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
        return entities;
    }

    ///<inheritdoc />
    public virtual void Remove(TEntity entity) => _dbSet.Remove(entity);

    ///<inheritdoc />
    public virtual async Task<int> RemoveAsync(Expression<Func<TEntity, bool>> expression)
        => await _dbSet.Where(expression).ExecuteDeleteAsync();

    ///<inheritdoc />
    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null) =>
        predicate == null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(predicate);

    ///<inheritdoc />
    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate) =>
        await _dbSet.AnyAsync(predicate);
}
