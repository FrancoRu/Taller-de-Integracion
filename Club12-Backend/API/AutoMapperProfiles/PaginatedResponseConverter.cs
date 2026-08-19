using Application.DTOs.Abstract.Response;

using AutoMapper;

using System.Collections.Generic;

namespace API.AutoMapperProfiles;

/// <summary>
/// Converts a paginated response's items from the source entity type to
/// the destination DTO type while preserving its paging metadata.
/// </summary>
/// <typeparam name="TSource">The source item type.</typeparam>
/// <typeparam name="TDestination">The destination item type.</typeparam>
public class PaginatedResponseConverter<TSource, TDestination>
    : ITypeConverter<PaginatedResponse<TSource>, PaginatedResponse<TDestination>>
{
    /// <summary>
    /// Maps the source paginated response's items to the destination type.
    /// </summary>
    /// <param name="source">The paginated response being converted.</param>
    /// <param name="destination">The unused destination instance supplied by AutoMapper.</param>
    /// <param name="context">The mapping context used to convert the items.</param>
    /// <returns>A new paginated response with mapped items and the source paging metadata.</returns>
    public PaginatedResponse<TDestination> Convert(
        PaginatedResponse<TSource> source,
        PaginatedResponse<TDestination> destination,
        ResolutionContext context)
    {
        return new()
        {
            Page = source.Page,
            PageSize = source.PageSize,
            TotalCount = source.TotalCount,
            Items = context.Mapper.Map<List<TDestination>>(source.Items)
        };
    }
}
