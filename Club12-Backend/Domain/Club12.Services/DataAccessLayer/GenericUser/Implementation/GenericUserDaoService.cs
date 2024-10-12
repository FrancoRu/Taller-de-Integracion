using Club12.Entities.UserEntity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Club12.Services.DataAccessLayer.GenericUser.Implementation;

public class GenericUserDaoService : IGenericUserDaoService
{
    public GenericUserDaoService(ApplicationDBContext context)
    {
        GetContext = context;
        DbSet = GetContext.Set<User>();
    }

    public DbSet<User> DbSet { get; }

    public ApplicationDBContext GetContext { get; }

    public void Insert(User entity)
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

    public void Insert(IEnumerable<User> entities)
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

    public Task InsertAsync(IEnumerable<User> entities)
    {
        return DbSet.AddRangeAsync(entities);
    }

    public async Task InsertAsync(User entity)
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
    public void Delete(User entity)
    {
        DbSet.Remove(entity);
    }

    public void Delete(IEnumerable<User> entities)
    {
        DbSet.RemoveRange(entities);
    }

    public void DeleteUnattached(User entity)
    {
        DbSet.Attach(entity);
        DbSet.Remove(entity);
    }

    public void Update(User entity)
    {
        GetContext.Entry(entity).State = EntityState.Modified;
    }

    public virtual User Get(params object[] keyValues)
    {
        User? entity = DbSet.Find(keyValues);

        return entity ?? throw new InvalidOperationException("Entity not found.");
    }

    public async Task<IEnumerable<User>> FindAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public IQueryable<User> FindAllQueryable()
    {
        return DbSet;
    }

    public IEnumerable<User> FindAllEnumerable()
    {
        return DbSet.ToList();
    }

    public virtual IQueryable<User> Include(Expression<Func<User, object>> expression)
    {
        return DbSet.Include(expression);
    }

    public virtual IQueryable<User> Where(Expression<Func<User, bool>> expression)
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
