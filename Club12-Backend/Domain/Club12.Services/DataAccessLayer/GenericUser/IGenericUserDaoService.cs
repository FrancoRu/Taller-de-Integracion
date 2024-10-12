using Club12.Entities.UserEntity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Club12.Services.DataAccessLayer.GenericUser;

/// <summary>
/// Generic data access interface implementation for user entities.
/// </summary>
public interface IGenericUserDaoService
{
    /// <summary>
    /// Gets the DbSet for user entities in the database context.
    /// </summary>
    DbSet<User> DbSet { get; }

    /// <summary>
    /// Gets the database context associated with this DAO.
    /// </summary>
    ApplicationDBContext GetContext { get; }

    /// <summary>
    /// Retrieves all user entities from a data source without including related navigation properties.
    /// </summary>
    /// <returns>An IQueryable containing all retrieved user entities.</returns>
    IQueryable<User> FindAllQueryable();

    /// <summary>
    /// Deletes a user entity from the data store.
    /// </summary>
    /// <param name="entity">The user entity to delete.</param>
    void Delete(User entity);

    /// <summary>
    /// Deletes multiple user entities from the data store.
    /// </summary>
    /// <param name="entities">The collection of user entities to delete.</param>
    void Delete(IEnumerable<User> entities);

    /// <summary>
    /// Deletes an unattached user entity from the data store.
    /// </summary>
    /// <param name="entity">The unattached user entity to delete.</param>
    void DeleteUnattached(User entity);

    /// <summary>
    /// Releases resources associated with the user entity.
    /// </summary>
    void GenericEntityDispose();

    /// <summary>
    /// Retrieves all user entities from the data store.
    /// </summary>
    /// <returns>A collection of user entities.</returns>
    IEnumerable<User> FindAllEnumerable();

    /// <summary>
    /// Retrieves all user entities from the data store asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, yielding a collection of user entities.</returns>
    Task<IEnumerable<User>> FindAllAsync();

    /// <summary>
    /// Retrieves a user entity by its primary key values.
    /// </summary>
    /// <param name="keyValues">The primary key values of the user entity.</param>
    /// <returns>The retrieved user entity.</returns>
    User Get(params object[] keyValues);

    /// <summary>
    /// Retrieves an IQueryable containing included related entities based on the specified expression.
    /// </summary>
    /// <param name="expression">The expression specifying related entities to include.</param>
    /// <returns>An IQueryable with included related entities.</returns>
    IQueryable<User> Include(Expression<Func<User, object>> expression);

    /// <summary>
    /// Inserts a user entity into the data store.
    /// </summary>
    /// <param name="entity">The user entity to insert.</param>
    void Insert(User entity);

    /// <summary>
    /// Inserts multiple user entities into the data store.
    /// </summary>
    /// <param name="entities">The collection of user entities to insert.</param>
    void Insert(IEnumerable<User> entities);

    /// <summary>
    /// Inserts multiple user entities into the data store asynchronously.
    /// </summary>
    /// <param name="entities">The collection of user entities to insert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InsertAsync(IEnumerable<User> entities);

    /// <summary>
    /// Inserts a user entity into the data store asynchronously.
    /// </summary>
    /// <param name="entity">The user entity to insert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InsertAsync(User entity);

    /// <summary>
    /// Saves changes to the data store.
    /// </summary>
    void Save();

    /// <summary>
    /// Saves changes to the data store asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveAsync();

    /// <summary>
    /// Updates a user entity in the data store.
    /// </summary>
    /// <param name="entity">The user entity to update.</param>
    void Update(User entity);

    /// <summary>
    /// Retrieves an IQueryable containing filtered user entities based on the specified expression.
    /// </summary>
    /// <param name="expression">The filter expression.</param>
    /// <returns>An IQueryable containing filtered user entities.</returns>
    IQueryable<User> Where(Expression<Func<User, bool>> expression);
}
