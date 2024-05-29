using Club12.Entities.UserEntity;
using Persistence;
using System.Linq.Expressions;

namespace Club12.Services.DataAccessLayer.GenericUser.Implementation;

/// <summary>
/// Generic data access service implementation for user entities.
/// </summary>
public class GenericUserService : IGenericUserService
{
    protected readonly GenericUserDaoService genericUserDao;

    public GenericUserService(ApplicationDBContext context)
    {
        genericUserDao = new GenericUserDaoService(context);
    }

    public virtual void Insert(User entity)
    {
        entity.DateCreated = DateTime.UtcNow;
        entity.DateUpdated = DateTime.UtcNow;
        genericUserDao.Insert(entity);
        genericUserDao.Save();
    }

    public virtual void Insert(ICollection<User> entities)
    {
        foreach (User item in entities)
        {
            item.DateCreated = DateTime.UtcNow;
            item.DateUpdated = DateTime.UtcNow;
        }
        genericUserDao.Insert(entities);
        genericUserDao.Save();
    }

    public virtual IQueryable<User> FindAllQueryable()
    {
        return genericUserDao.FindAllQueryable();
    }

    public virtual async Task InsertAsync(User entity)
    {
        entity.DateCreated = DateTime.UtcNow;
        entity.DateUpdated = DateTime.UtcNow;
        await genericUserDao.InsertAsync(entity);
        await genericUserDao.SaveAsync();
    }

    public virtual async Task InsertAsync(ICollection<User> entities)
    {
        foreach (User item in entities)
        {
            item.DateCreated = DateTime.UtcNow;
            item.DateUpdated = DateTime.UtcNow;
        }

        await genericUserDao.InsertAsync(entities);
        await genericUserDao.SaveAsync();
    }

    public virtual void Delete(User entity)
    {
        genericUserDao.Delete(entity);
        genericUserDao.Save();
    }

    public virtual void Delete(IEnumerable<User> entities)
    {
        foreach (User item in entities)
        {
            genericUserDao.Delete(item);
        }
        genericUserDao.Save();
    }

    public virtual async Task DeleteAsync(User entity)
    {
        genericUserDao.Delete(entity);
        await genericUserDao.SaveAsync();
    }

    public virtual async Task DeleteWhereAsync(Expression<Func<User, bool>> expression)
    {
        IQueryable<User> entities = genericUserDao.Where(expression);
        genericUserDao.Delete(entities);
        await genericUserDao.SaveAsync();
    }

    public virtual void DeleteUnattached(User entity)
    {
        genericUserDao.DeleteUnattached(entity);
        genericUserDao.Save();
    }

    public virtual void Update(User entity)
    {
        entity.DateUpdated = DateTime.UtcNow;
        genericUserDao.Update(entity);
        genericUserDao.Save();
    }

    public virtual Task UpdateAsync(User entity)
    {
        entity.DateUpdated = DateTime.UtcNow;
        genericUserDao.Update(entity);
        return genericUserDao.SaveAsync();
    }

    public virtual User? TryGet(params object[] keyValues)
    {
        try
        {
            return genericUserDao.Get(keyValues);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public virtual async Task<IEnumerable<User>> FindAllAsync()
    {
        return await genericUserDao.FindAllAsync();
    }

    public virtual IEnumerable<User> FindAll()
    {
        return genericUserDao.FindAllEnumerable();
    }

    public virtual void EntityDispose()
    {
        genericUserDao.GenericEntityDispose();
    }

    public IQueryable<User> FilterByExpression(Expression<Func<User, bool>> expression)
    {
        return genericUserDao.Where(expression);
    }
}
