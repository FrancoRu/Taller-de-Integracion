using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Abstract.Request;

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
