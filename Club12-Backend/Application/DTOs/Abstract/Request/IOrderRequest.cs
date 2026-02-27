using System.ComponentModel.DataAnnotations;

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

/// <summary>
/// Enum to specify the sort order.
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Ascending order.
    /// </summary>
    [Display(Name = "asc")]
    Ascending,

    /// <summary>
    /// Descending order.
    /// </summary>
    [Display(Name = "desc")]
    Descending
}