using Application.DTOs.Abstract.Response;
using Application.DTOs.AuditLogs.Request;

using Domain.Entities.Models;
using Domain.Enums;

using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Records and reads the sensitive-action audit trail.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Writes an audit entry for action without ever throwing at the call site.
    /// </summary>
    Task LogAsync(
        AuditAction action,
        string? targetType = null,
        string? targetId = null,
        string? targetName = null,
        string? detail = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns audit entries, newest first by default, with pagination.
    /// </summary>
    Task<PaginatedResponse<AuditLog>> GetAuditLogsAsync(AuditLogFilteredRequest filter);
}
