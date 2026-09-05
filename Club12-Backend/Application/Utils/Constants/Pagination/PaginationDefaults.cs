namespace Application.Utils.Constants.Pagination;

/// <summary>
/// Default and boundary values applied to paginated list requests when the
/// caller does not specify a page size, or supplies an out-of-range one.
/// </summary>
public static class PaginationDefaults
{
    public const int DefaultPageSize = 100;

    /// <summary>
    /// Maximum allowed page size; requests above this are clamped down to it.
    /// </summary>
    public const int MaxPageSize = 100;
}
