using Application.DTOs.Abstract.Response;
using Application.DTOs.AuditLogs.Request;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Test double for IAuditService. Records every call so tests can assert on
/// what got logged without a real repository/database.
/// </summary>
public sealed class FakeAuditService : IAuditService
{
    public List<AuditAction> LoggedActions { get; } = [];

    public Task LogAsync(
        AuditAction action,
        string? targetType = null,
        string? targetId = null,
        string? targetName = null,
        string? detail = null,
        CancellationToken ct = default)
    {
        LoggedActions.Add(action);
        return Task.CompletedTask;
    }

    public Task<PaginatedResponse<AuditLog>> GetAuditLogsAsync(AuditLogFilteredRequest filter)
    {
        return Task.FromResult(new PaginatedResponse<AuditLog>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = 0,
            Items = [],
        });
    }
}
