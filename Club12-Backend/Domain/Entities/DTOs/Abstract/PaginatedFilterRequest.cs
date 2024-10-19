namespace Entities.DTOs.Abstract;

/// <summary>
/// Represents a request for paginated and filtered data.
/// </summary>
public class PaginatedFilterRequest : IPaginationRequest, IOrderRequest
{
    /// <summary>
    /// The page number for pagination. Default is 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    private int _pageSize = 10;

    /// <summary>
    /// The page size for pagination. Default is 10.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : value > 100 ? 100 : value;
    }

    /// <summary>
    /// The property name to sort by.
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// The sort order. Default is Ascending.
    /// </summary>
    public SortOrder Order { get; set; } = SortOrder.Ascending;
}