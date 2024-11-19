using Entities;
using Entities.DTOs.Abstract;

using Microsoft.EntityFrameworkCore;

using Persistence;

using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

namespace Services.DataAccessLayer.GenericEntity.Implementation;

public class GenericService<TEntity>(ApplicationDBContext context) : IGenericService<TEntity> where TEntity : EntityBase
{
    protected GenericDaoService<TEntity> _genericDao = new(context);

    public virtual void Insert(TEntity entity)
    {
        entity.DateCreated = DateTime.UtcNow;
        entity.DateUpdated = DateTime.UtcNow;
        _genericDao.Insert(entity);
        _genericDao.Save();
    }

    public virtual void Insert(ICollection<TEntity> entities)
    {
        foreach (TEntity item in entities)
        {
            item.DateCreated = DateTime.UtcNow;
            item.DateUpdated = DateTime.UtcNow;
        }
        _genericDao.Insert(entities);
        _genericDao.Save();
    }

    public virtual IQueryable<TEntity> FindAllQueryable()
    {
        return _genericDao.FindAllQueryable();
    }

    public virtual async Task InsertAsync(TEntity entity)
    {
        entity.DateCreated = DateTime.UtcNow;
        entity.DateUpdated = DateTime.UtcNow;
        await _genericDao.InsertAsync(entity);
        await _genericDao.SaveAsync();
    }

    public virtual async Task InsertAsync(ICollection<TEntity> entities)
    {
        foreach (TEntity item in entities)
        {
            item.DateCreated = DateTime.UtcNow;
            item.DateUpdated = DateTime.UtcNow;
        }

        await _genericDao.InsertAsync(entities);
        await _genericDao.SaveAsync();
    }

    public virtual void Delete(TEntity entity)
    {
        _genericDao.Delete(entity);
        _genericDao.Save();
    }

    public virtual void DeleteBatch(IEnumerable<TEntity> entities)
    {
        foreach (TEntity item in entities)
        {
            _genericDao.Delete(item);
        }
        _genericDao.Save();
    }

    public virtual async Task DeleteAsync(TEntity entity)
    {
        _genericDao.Delete(entity);
        await _genericDao.SaveAsync();
    }

    public virtual async Task DeleteWhereAsync(Expression<Func<TEntity, bool>> expression)
    {
        IQueryable<TEntity> entities = _genericDao.Where(expression);
        _genericDao.Delete(entities);
        await _genericDao.SaveAsync();
    }

    public virtual void DeleteUnattached(TEntity entity)
    {
        _genericDao.DeleteUnattached(entity);
        _genericDao.Save();
    }

    public virtual void Update(TEntity entity)
    {
        entity.DateUpdated = DateTime.UtcNow;
        _genericDao.Update(entity);
        _genericDao.Save();
    }

    public virtual Task UpdateAsync(TEntity entity)
    {
        entity.DateUpdated = DateTime.UtcNow;
        _genericDao.Update(entity);
        return _genericDao.SaveAsync();
    }

    public virtual TEntity? TryGet(params object[] keyValues)
    {
        try
        {
            return _genericDao.Get(keyValues);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public virtual async Task<IEnumerable<TEntity>> FindAllAsync()
    {
        return await _genericDao.FindAllAsync();
    }

    public virtual IEnumerable<TEntity> FindAll()
    {
        return _genericDao.FindAllEnumerable();
    }

    public virtual void EntityDispose()
    {
        _genericDao.GenericEntityDispose();
    }

    public IQueryable<TEntity> FilterByExpressionWithPagination(
    Expression<Func<TEntity, bool>> expression,
    IPaginationRequest paginationRequest,
    params Expression<Func<TEntity, object?>>[] includes)
    {
        IQueryable<TEntity> query = _genericDao.Where(expression);

        foreach (Expression<Func<TEntity, object?>> include in includes)
        {
            query = query.Include(include);
        }

        return query.Paginate(paginationRequest.PageNumber, paginationRequest.PageSize);
    }

    public IQueryable<TEntity> FilterByExpression(Expression<Func<TEntity, bool>> expression)
    {
        return _genericDao.Where(expression);
    }

    public async Task<int> GetCountAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _genericDao.Where(predicate).AsNoTracking().CountAsync();
    }

    public virtual async Task InsertRangeAsync(ICollection<TEntity> entities)
    {
        if (entities.Count is 0) return;

        foreach (TEntity entity in entities)
        {
            entity.DateCreated = DateTime.UtcNow;
            entity.DateUpdated = DateTime.UtcNow;
        }

        await _genericDao.InsertAsync(entities);
        await _genericDao.SaveAsync();
    }
}