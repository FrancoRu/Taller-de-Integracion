using Entities;

using Microsoft.EntityFrameworkCore;

using Persistance;

using System.Linq.Expressions;

namespace Services.DataAccessLayer.GenericEntity.Implementation;

public class GenericDaoService<TEntity> : IGenericDaoService<TEntity> where TEntity : EntityBase
{
    public GenericDaoService(ApplicationDBContext _context)
    {
        GetContext = _context;
        DbSet = GetContext.Set<TEntity>();
    }

    public DbSet<TEntity> DbSet { get; }

    public ApplicationDBContext GetContext { get; }

    public void Insert(TEntity entity)
    {
        try
        {
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

    public Task InsertAsync(IEnumerable<TEntity> entities) => DbSet.AddRangeAsync(entities);

    public async Task InsertAsync(TEntity entity)
    {
        try
        {
            await DbSet.AddAsync(entity);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Exception: {0}", ex);
            throw;
        }
    }
    public void Delete(TEntity entity) => DbSet.Remove(entity);

    public void Delete(IEnumerable<TEntity> entities) => DbSet.RemoveRange(entities);

    public void DeleteUnattached(TEntity entity)
    {
        DbSet.Attach(entity);
        DbSet.Remove(entity);
    }

    public void Update(TEntity entity) => GetContext.Entry(entity).State = EntityState.Modified;

    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
    {
        foreach (TEntity entity in entities)
        {
            GetContext.Entry(entity).State = EntityState.Modified;
        }

        await GetContext.SaveChangesAsync();
    }

    public virtual TEntity Get(params object[] keyValues)
    {
        TEntity? entity = DbSet.Find(keyValues);

        return entity ?? throw new InvalidOperationException("Entity not found.");
    }

    public async Task<IEnumerable<TEntity>> FindAllAsync() => await DbSet.ToListAsync();

    public IQueryable<TEntity> FindAllQueryable() => DbSet;

    public IEnumerable<TEntity> FindAllEnumerable() => [.. DbSet];

    public virtual IQueryable<TEntity> Include(Expression<Func<TEntity, object>> expression) => DbSet.Include(expression);

    public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> expression) => DbSet.Where(expression);

    public async Task SaveAsync() => await GetContext.SaveChangesAsync();

    public void Save() => GetContext.SaveChanges();

    public void GenericEntityDispose() => GetContext.Dispose();
}
