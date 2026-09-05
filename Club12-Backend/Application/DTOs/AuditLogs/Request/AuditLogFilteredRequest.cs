using Application.DTOs.Abstract.Request;
using Application.Utils.Constants.Pagination;

using Domain.Enums;

namespace Application.DTOs.AuditLogs.Request;

/// <summary>
/// Filtering and pagination for the audit-trail listing, defaulting to newest-first.
/// </summary>
public class AuditLogFilteredRequest : PaginatedFilterRequest
{
    public AuditLogFilteredRequest()
    {
        Order = SortOrder.Descending;
    }

    /// <summary>
    /// Optional filter by the actor who performed the action.
    /// </summary>
    public string? Actor { get; set; }

    /// <summary>
    /// Optional filter by the action type.
    /// </summary>
    public AuditAction? Action { get; set; }
}
