using Entities.DTOs.Abstract;

using System.Linq.Expressions;
using System.Reflection;

namespace Services.Utils.OrderFiltering;

/// <summary>
/// Queryable extensions for filtering and sorting entities.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Constructs a filter expression based on the provided filter request.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to filter.</typeparam>
    /// <typeparam name="T">The type of the filter request.</typeparam>
    /// <param name="filter">The filter request containing the filter criteria.</param>
    /// <returns>An expression that represents the filter criteria.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the 'Contains' method is not found.</exception>
    public static Expression<Func<TEntity, bool>> ConstructFilterExpression<TEntity, T>(T filter) where T : PaginatedFilterRequest
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "x");
        Expression finalExpression = Expression.Constant(true);
        MethodInfo? containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])
            ?? throw new InvalidOperationException("The 'Contains' method could not be found on the string class.");

        PropertyInfo[] filterProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in filterProperties)
        {
            if (ShouldSkipProperty(property.Name))
            {
                continue;
            }

            object? filterValue = property.GetValue(filter);
            if (filterValue is not null)
            {
                MemberExpression propertyAccess = Expression.Property(parameter, property.Name);
                if (property.PropertyType == typeof(string) && filterValue is string stringValue)
                {
                    MethodInfo? toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes) ?? throw new InvalidCastException("The 'ToLower' method could not be found on the string class.");
                    Expression toLowerProperty = Expression.Call(propertyAccess, toLowerMethod);
                    Expression toLowerFilterValue = Expression.Constant(stringValue.ToLower());
                    Expression containsExpression = Expression.Call(toLowerProperty, containsMethod, toLowerFilterValue);
                    finalExpression = Expression.AndAlso(finalExpression, containsExpression);
                }
                else
                {
                    ConstantExpression constantValue = Expression.Constant(filterValue);
                    Expression equalityExpression = Expression.Equal(propertyAccess, constantValue);
                    finalExpression = Expression.AndAlso(finalExpression, equalityExpression);
                }
            }
        }

        return Expression.Lambda<Func<TEntity, bool>>(finalExpression, parameter);
    }


    /// <summary>
    /// Paginates the given source sequence based on the specified page number and page size.
    /// </summary>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> source, int pageNumber, int pageSize)
    {
        return source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Sorts the source sequence by the specified property name in either ascending or descending order.
    /// </summary>
    public static IQueryable<T> SortBy<T>(this IQueryable<T> source, IOrderRequest orderRequest)
    {
        if (string.IsNullOrEmpty(orderRequest.OrderBy))
        {
            return source;
        }

        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        MemberExpression property = Expression.Property(parameter, orderRequest.OrderBy);
        LambdaExpression lambda = Expression.Lambda(property, parameter);

        MethodInfo method = orderRequest.Order == SortOrder.Ascending
            ? typeof(Queryable).GetMethods().First(m => m.Name == "OrderBy" && m.GetParameters().Length == 2)
            : typeof(Queryable).GetMethods().First(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);

        MethodInfo genericMethod = method.MakeGenericMethod(typeof(T), property.Type);

        return (IQueryable<T>) genericMethod.Invoke(null, [source, lambda])!;
    }

    private static bool ShouldSkipProperty(string propertyName)
    {
        return propertyName is
            nameof(PaginatedFilterRequest.PageSize) or
            nameof(PaginatedFilterRequest.PageNumber) or
            nameof(PaginatedFilterRequest.OrderBy) or
            nameof(PaginatedFilterRequest.Order);
    }
}
