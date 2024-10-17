namespace Club12.Services.DTOs.Abstract;

/// <summary>
/// Defines the pagination request properties.
/// </summary>
public interface IPaginationRequest
{
    /// <summary>
    /// The page number for pagination. Default is 1.
    /// </summary>
    int PageNumber { get; set; }

    /// <summary>
    /// The page size for pagination. Default is 10.
    /// </summary>
    int PageSize { get; set; }
}
