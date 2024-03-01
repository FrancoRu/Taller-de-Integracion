using Club12.Entities;
using Persistence;
using System.Linq.Expressions;

namespace Club12.Services.DataAccessLayer.Implementation;

public class GenericService<TEntity> : IGenericService<TEntity> where TEntity : EntityBase
{
    protected GenericDao<TEntity> genericDao;

    public GenericService(ApplicationDBContext context)
    {
        genericDao = new GenericDao<TEntity>(context);
    }

    public virtual void Insert(TEntity entity)
    {
        entity.DateCreated = DateTime.Now;
        entity.DateUpdated = DateTime.Now;
        genericDao.Insert(entity);
        genericDao.Save();
    }

    public virtual void Insert(ICollection<TEntity> entities)
    {
        foreach (TEntity item in entities)
        {
            item.DateCreated = DateTime.UtcNow;
            item.DateUpdated = DateTime.Now;
        }
        genericDao.Insert(entities);
        genericDao.Save();
    }

    public virtual IQueryable<TEntity> FindAllQueryable()
    {
        return genericDao.FindAllQueryable();
    }

    public virtual async Task InsertAsync(TEntity entity)
    {
        await genericDao.InsertAsync(entity);
        await genericDao.SaveAsync();
    }

    public virtual async Task InsertAsync(ICollection<TEntity> entities)
    {
        foreach (TEntity item in entities)
        {
            item.DateCreated = DateTime.UtcNow;
        }

        await genericDao.InsertAsync(entities);
        await genericDao.SaveAsync();
    }

    public virtual void InsertOrUpdate(TEntity entity)
    {
        genericDao.InsertOrUpdate(entity);
        genericDao.Save();
    }

    public virtual void Delete(TEntity entity)
    {
        genericDao.Delete(entity);
        genericDao.Save();
    }

    public virtual void Delete(IEnumerable<TEntity> entities)
    {
        foreach (TEntity item in entities)
        {
            genericDao.Delete(item);
        }
        genericDao.Save();
    }

    public virtual async Task DeleteAsync(TEntity entity)
    {
        genericDao.Delete(entity);
        await genericDao.SaveAsync();
    }

    public virtual async Task DeleteWhereAsync(Expression<Func<TEntity, bool>> expression)
    {
        IQueryable<TEntity> entities = genericDao.Where(expression);
        genericDao.Delete(entities);
        await genericDao.SaveAsync();
    }

    public virtual void DeleteUnattached(TEntity entity)
    {
        genericDao.DeleteUnattached(entity);
        genericDao.Save();
    }

    public virtual void Update(TEntity entity)
    {
        genericDao.Update(entity);
        genericDao.Save();
    }

    public virtual Task UpdateAsync(TEntity entity)
    {
        genericDao.Update(entity);
        return genericDao.SaveAsync();
    }

    public virtual TEntity? TryGet(params object[] keyValues)
    {
        try
        {
            return genericDao.Get(keyValues);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public virtual async Task<IEnumerable<TEntity>> FindAllAsync()
    {
        return await genericDao.FindAllAsync();
    }

    public virtual IEnumerable<TEntity> FindAll()
    {
        return genericDao.FindAllEnumerable();
    }

    public virtual void EntityDispose()
    {
        genericDao.GenericEntityDispose();
    }

    public IQueryable<TEntity> FilterByExpression(Expression<Func<TEntity, bool>> expression)
    {
        return genericDao.Where(expression);
    }
}