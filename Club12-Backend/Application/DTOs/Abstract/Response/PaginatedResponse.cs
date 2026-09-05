using System.Collections.Generic;

namespace Application.DTOs.Abstract.Response;

/// <summary>
/// A page of results together with the metadata needed to fetch adjacent pages.
/// </summary>
/// <typeparam name="T">The type of item contained in the page.</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// The items in the collection.
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// The current page number.
    /// </summary>
    public required int Page { get; set; }

    /// <summary>
    /// The number of items in a page.
    /// </summary>
    public required int PageSize { get; set; }

    /// <summary>
    /// The total number of items in the collection.
    /// </summary>
    public required int TotalCount { get; set; }

}