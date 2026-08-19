namespace Application.DTOs.Abstract.Request;

/// <summary>
/// Defines the ordering request properties.
/// </summary>
public interface IOrderRequest
{
    /// <summary>
    /// The property name to sort by.
    /// </summary>
    string? OrderBy { get; set; }

    /// <summary>
    /// The sort order. Default is Ascending.
    /// </summary>
    SortOrder? Order { get; set; }
}