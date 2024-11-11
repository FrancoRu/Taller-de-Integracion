using Club12.Entities.UserEntity;
using System.Linq.Expressions;

namespace Club12.Services.DataAccessLayer.GenericUser;

/// <summary>
/// Generic data access Interface for user entities.
/// </summary>
public interface IGenericUserService
{
    /// <summary>
    /// Retrieves all user entities as Queryable.
    /// </summary>
    /// <returns>An IQueryable containing the retrieved user entities with included navigation properties.</returns>
    IQueryable<User> FindAllQueryable();

    /// <summary>
    /// Deletes a user entity.
    /// </summary>
    /// <param name="entity">The user entity to delete.</param>
    void Delete(User entity);

    /// <summary>
    /// Deletes a collection of user entities.
    /// </summary>
    /// <param name="entities">The collection of user entities to be deleted.</param>
    void Delete(IEnumerable<User> entities);

    /// <summary>
    /// Asynchronously deletes user entities based on a specified condition.
    /// </summary>
    /// <param name="expression">An expression that defines the condition for selecting user entities to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteWhereAsync(Expression<Func<User, bool>> expression);

    /// <summary>
    /// Asynchronously deletes a user entity.
    /// </summary>
    /// <param name="entity">The user entity to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(User entity);

    /// <summary>
    /// Deletes an unattached user entity.
    /// </summary>
    /// <param name="entity">The unattached user entity to delete.</param>
    void DeleteUnattached(User entity);

    /// <summary>
    /// Releases resources associated with the user entity.
    /// </summary>
    void EntityDispose();

    /// <summary>
    /// Retrieves all user entities.
    /// </summary>
    /// <returns>A collection of user entities.</returns>
    IEnumerable<User> FindAll();

    /// <summary>
    /// Retrieves all user entities asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, yielding a collection of user entities.</returns>
    Task<IEnumerable<User>> FindAllAsync();

    /// <summary>
    /// Retrieves a user entity based on the specified key values.
    /// </summary>
    /// <param name="keyValues">The key values that uniquely identify the user entity.</param>
    /// <returns>The retrieved user entity, or null if not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user entity is not found in the data source.</exception>
    User? TryGet(params object[] keyValues);

    /// <summary>
    /// Inserts a user entity.
    /// </summary>
    /// <param name="entity">The user entity to insert.</param>
    void Insert(User entity);

    /// <summary>
    /// Inserts a collection of user entities.
    /// </summary>
    /// <param name="entities">The collection of user entities to be inserted.</param>
    void Insert(ICollection<User> entities);

    /// <summary>
    /// Asynchronously inserts a user entity.
    /// </summary>
    /// <param name="entity">The user entity to insert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InsertAsync(User entity);

    /// <summary>
    /// Asynchronously inserts a collection of user entities.
    /// </summary>
    /// <param name="entities">The collection of user entities to be inserted asynchronously.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InsertAsync(ICollection<User> entities);

    /// <summary>
    /// Updates a user entity in the data store.
    /// </summary>
    /// <param name="entity">The user entity to update.</param>
    void Update(User entity);

    /// <summary>
    /// Asynchronously updates a user entity.
    /// </summary>
    /// <param name="entity">The user entity to be updated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(User entity);

    /// <summary>
    /// Filters user entities based on the specified expression.
    /// </summary>
    /// <param name="expression">The filter expression.</param>
    /// <returns>An IQueryable containing filtered user entities.</returns>
    IQueryable<User> FilterByExpression(Expression<Func<User, bool>> expression);
}
