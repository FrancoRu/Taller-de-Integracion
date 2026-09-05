using Application.DTOs.Abstract.Request;
using Application.Utils.Constants;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Application.Utils.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Builds a combined-AND filter expression by reflecting over <typeparamref name="T"/>'s
    /// properties that also exist on <typeparamref name="TEntity"/>: string properties become
    /// case-insensitive <c>Contains</c>, everything else becomes equality. Pagination/order
    /// properties are skipped automatically.
    /// </summary>
    /// <param name="filter">The filter DTO whose non-empty, non-skipped properties become predicates.</param>
    /// <param name="ignoredProperties">
    /// Names of filter properties whose auto-generated predicate must be
    /// suppressed, letting the caller special-case them (e.g. resolving a
    /// filter through a join instead of the entity's own FK-equality). Purely
    /// additive: callers that pass nothing get the original behavior.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when the 'Contains' method is not found.</exception>
    public static Expression<Func<TEntity, bool>> ConstructFilterExpression<TEntity, T>(T filter, params string[] ignoredProperties) where T : PaginatedFilterRequest
    {
        PropertyInfo[] filterProperties = [.. typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !ShouldSkipProperty(p.Name)
                && !ignoredProperties.Contains(p.Name)
                && typeof(TEntity).GetProperty(p.Name) != null)];

        bool allNullOrEmpty = Array.TrueForAll(filterProperties, property =>
        {
            object? value = property.GetValue(filter);
            return value == null || (property.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string) value));
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
            ?? throw new InvalidOperationException(ErrorMessages.Query.ContainsMethodNotFound);

        MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)
            ?? throw new InvalidCastException(ErrorMessages.Query.ToLowerMethodNotFound);

        foreach (PropertyInfo property in filterProperties)
        {
            object? filterValue = property.GetValue(filter);

            if (filterValue == null)
            {
                continue;
            }

            if (property.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string) filterValue))
            {
                continue;
            }

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
    /// Combines two entity predicates with a logical AND, rebinding the second
    /// predicate's parameter onto the first so the result is a single lambda
    /// EF Core can translate to SQL (unlike Expression.Invoke). Used to append
    /// extra server-side filters (e.g. published-only blog posts, HU-16) to a
    /// dynamically built filter expression.
    /// </summary>
    public static Expression<Func<TEntity, bool>> AndAlso<TEntity>(
        this Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right)
    {
        ParameterExpression parameter = left.Parameters[0];
        Expression reboundRight = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body);
        Expression combined = Expression.AndAlso(left.Body, reboundRight);
        return Expression.Lambda<Func<TEntity, bool>>(combined, parameter);
    }

    private sealed class ReplaceParameterVisitor(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == from ? to : base.VisitParameter(node);
        }
    }

    public static IQueryable<T> Paginate<T>(this IQueryable<T> source, int pageNumber, int pageSize)
    {
        return source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }

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
