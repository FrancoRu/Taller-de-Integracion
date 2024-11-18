namespace Entities.DTOs.Abstract;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
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