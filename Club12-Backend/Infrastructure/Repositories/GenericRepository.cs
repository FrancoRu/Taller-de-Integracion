using Application.DTOs.Abstract.Request;
using Application.Interfaces.Repositories;
using Application.Utils.Extensions;
using Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class GenericRepository<TEntity>(ApplicationDBContext context) 
    : IGenericRepository<TEntity> where TEntity : EntityBase
{
    protected readonly ApplicationDBContext _context = context;
    protected readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
    {
        _dbSet.UpdateRange(entities);
        await _context.SaveChangesAsync();
    }

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
        return entity;
    }

    ///<inheritdoc />
    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await _dbSet.AddRangeAsync(entities);
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
