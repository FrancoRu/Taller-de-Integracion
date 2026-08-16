using Application.Utils.Constants.Pagination;

namespace Application.DTOs.Abstract.Request;

/// <summary>
/// Represents a request for paginated and filtered data.
/// </summary>
public class PaginatedFilterRequest : IPaginationRequest, IOrderRequest
{
    /// <summary>
    /// The page number for pagination. Default is 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    private int _pageSize = PaginationDefaults.DefaultPageSize;

    /// <summary>
    /// The page size for pagination. Default is 10.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = ClampPageSize(value);
    }

    private static int ClampPageSize(int requestedPageSize)
    {
        return requestedPageSize < 1
            ? PaginationDefaults.DefaultPageSize
            : requestedPageSize > PaginationDefaults.MaxPageSize
            ? PaginationDefaults.MaxPageSize
            : requestedPageSize;
    }

    /// <summary>
    /// The property name to sort by.
    /// </summary>
    public string? OrderBy { get; set; } = "DateCreated";

    /// <summary>
    /// The sort order. Default is Ascending.
    /// </summary>
    public SortOrder? Order { get; set; } = SortOrder.Ascending;
}