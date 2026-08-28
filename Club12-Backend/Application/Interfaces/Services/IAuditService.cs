using Application.DTOs.Abstract.Response;
using Application.DTOs.AuditLogs.Request;

using Domain.Entities.Models;
using Domain.Enums;

using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Records and reads the sensitive-action audit trail (HU-101). The actor is
/// resolved automatically from <see cref="ICurrentUserAccessor"/>, so callers
/// only describe the action and its target.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Writes an audit entry for <paramref name="action"/>. Non-throwing by
    /// contract at the call site: auditing must never be the reason a
    /// sensitive operation fails, so callers invoke it after the action has
    /// succeeded.
    /// </summary>
    Task LogAsync(
        AuditAction action,
        string? targetType = null,
        string? targetId = null,
        string? detail = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns audit entries, newest first by default, with pagination.
    /// </summary>
    Task<PaginatedResponse<AuditLog>> GetAuditLogsAsync(AuditLogFilteredRequest filter);
}
