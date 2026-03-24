using Application.DTOs.Abstract.Request;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Application.Utils.Extensions;

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
        PropertyInfo[] filterProperties = [.. typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !ShouldSkipProperty(p.Name) && typeof(TEntity).GetProperty(p.Name) != null)];

        bool allNullOrEmpty = filterProperties.All(property =>
        {
            object? value = property.GetValue(filter);
            if (value == null) return true;
            if (property.PropertyType == typeof(string))
            {
                return string.IsNullOrWhiteSpace((string)value);
            }
            return false;
        });

        if (allNullOrEmpty)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "x");
            Expression trueExpr = Expression.Constant(true);
            return Expression.Lambda<Func<TEntity, bool>>(trueExpr, parameter);
        }

        ParameterExpression parameterExpr = Expression.Parameter(typeof(TEntity), "x");
        Expression? finalExpression = null;

        MethodInfo containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])
            ?? throw new InvalidOperationException("The 'Contains' method could not be found on the string class.");

        MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)
            ?? throw new InvalidCastException("The 'ToLower' method could not be found on the string class.");

        foreach (PropertyInfo property in filterProperties)
        {
            object? filterValue = property.GetValue(filter);

            if (filterValue == null) continue;
            if (property.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string)filterValue)) continue;

            MemberExpression propertyAccess = Expression.Property(parameterExpr, property.Name);
            Expression currentExpr;

            if (property.PropertyType == typeof(string) && filterValue is string stringValue)
            {
                Expression toLowerProperty = Expression.Call(propertyAccess, toLowerMethod);
                Expression toLowerFilterValue = Expression.Constant(stringValue.ToLower());
                currentExpr = Expression.Call(toLowerProperty, containsMethod, toLowerFilterValue);
            }
            else
            {
                Expression constantExpr = Expression.Constant(filterValue, filterValue.GetType());

                if (constantExpr.Type != propertyAccess.Type)
                {
                    constantExpr = Expression.Convert(constantExpr, propertyAccess.Type);
                }

                currentExpr = Expression.Equal(propertyAccess, constantExpr);
            }

            finalExpression = finalExpression == null ? currentExpr : Expression.AndAlso(finalExpression, currentExpr);
        }

        finalExpression ??= Expression.Constant(true);

        return Expression.Lambda<Func<TEntity, bool>>(finalExpression, parameterExpr);
    }

    /// <summary>
    /// Paginates the given source sequence based on the specified page number and page size.
    /// </summary>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> source, int pageNumber, int pageSize) => source.Skip((pageNumber - 1) * pageSize).Take(pageSize);

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

        return (IQueryable<T>)genericMethod.Invoke(null, [source, lambda])!;
    }

    private static bool ShouldSkipProperty(string propertyName) => propertyName is
            nameof(PaginatedFilterRequest.PageSize) or
            nameof(PaginatedFilterRequest.PageNumber) or
            nameof(PaginatedFilterRequest.OrderBy) or
            nameof(PaginatedFilterRequest.Order);
}
