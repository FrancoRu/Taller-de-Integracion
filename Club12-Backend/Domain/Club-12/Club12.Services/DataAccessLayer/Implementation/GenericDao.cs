using Club12.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Club12.Services.DataAccessLayer.Implementation;

public class GenericDao<TEntity> : IGenericDao<TEntity> where TEntity : EntityBase
{
    public GenericDao(ApplicationDBContext context)
    {
        GetContext = context;
        DbSet = GetContext.Set<TEntity>();
    }

    public DbSet<TEntity> DbSet { get; }

    public ApplicationDBContext GetContext { get; }

    public void Insert(TEntity entity)
    {
        try
        {
            entity.DateCreated = DateTime.UtcNow;
            DbSet.Add(entity);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Exception: {0}", ex);
            throw;
        }
    }

    public void Insert(IEnumerable<TEntity> entities)
    {
        try
        {
            DbSet.AddRange(entities);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Exception: {0}", ex);
            throw;
        }
    }

    public Task InsertAsync(IEnumerable<TEntity> entities)
    {
        return DbSet.AddRangeAsync(entities);
    }

    public async Task InsertAsync(TEntity entity)
    {
        try
        {
            entity.DateCreated = DateTime.UtcNow;
            await DbSet.AddAsync(entity);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Exception: {0}", ex);
            throw;
        }
    }

    public void InsertOrUpdate(TEntity entity)
    {
        PropertyInfo[] properties = entity.GetType().GetProperties();
        PropertyInfo? propertyId = null;

        foreach (PropertyInfo item in properties)
        {
            if (item.GetCustomAttribute(typeof(KeyAttribute)) != null)
            {
                propertyId = item;
                break;
            }
        }

        if (propertyId == null || propertyId.GetMethod == null)
        {
            throw new InvalidOperationException("Entity does not have a KeyAttribute or the KeyAttribute does not have a getter method.");
        }

        object? idValue = propertyId.GetMethod.Invoke(entity, null);

        if (idValue is int id)
        {
            if (id == 0)
            {
                entity.DateCreated = DateTime.Now;
            }
            else
            {
                entity.DateUpdated = DateTime.Now;
                GetContext.Entry(entity).State = EntityState.Modified;
            }
        }
        else
        {
            throw new InvalidOperationException("The value of the key property is null or not of type 'int'.");
        }
    }

    public void Delete(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    public void Delete(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
    }

    public void DeleteUnattached(TEntity entity)
    {
        DbSet.Attach(entity);
        DbSet.Remove(entity);
    }

    public void Update(TEntity entity)
    {
        GetContext.Entry(entity).State = EntityState.Modified;
        entity.DateUpdated = DateTime.UtcNow;
    }

    public virtual TEntity Get(params object[] keyValues)
    {
        TEntity? entity = DbSet.Find(keyValues);

        return entity ?? throw new InvalidOperationException("Entity not found.");
    }

    public async Task<IEnumerable<TEntity>> FindAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public IQueryable<TEntity> FindAllQueryable()
    {
        return DbSet;
    }

    public IEnumerable<TEntity> FindAllEnumerable()
    {
        return DbSet.ToList();
    }

    public virtual IQueryable<TEntity> Include(Expression<Func<TEntity, object>> expression)
    {
        return DbSet.Include(expression);
    }

    public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> expression)
    {
        return DbSet.Where(expression);
    }

    public async Task SaveAsync()
    {
        await GetContext.SaveChangesAsync();
    }

    public void Save()
    {
        GetContext.SaveChanges();
    }

    public void GenericEntityDispose()
    {
        GetContext.Dispose();
    }
}
