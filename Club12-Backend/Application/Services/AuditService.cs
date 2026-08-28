using Application.DTOs.Abstract.Response;
using Application.DTOs.AuditLogs.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;

using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services;

/// <inheritdoc cref="IAuditService"/>
public class AuditService(IAuditLogRepository auditLogRepository, ICurrentUserAccessor currentUserAccessor)
    : IAuditService
{
    public async Task LogAsync(
        AuditAction action,
        string? targetType = null,
        string? targetId = null,
        string? detail = null,
        CancellationToken ct = default)
    {
        string actor = currentUserAccessor.Actor;

        await auditLogRepository.AddAsync(new AuditLog
        {
            Id = Guid.Empty,
            Action = action,
            Actor = actor,
            TargetType = targetType,
            TargetId = targetId,
            Detail = detail,
            DateCreated = DateTime.UtcNow,
            // EntityBase.CreatedBy is required; mirror the actor so the base
            // audit columns and the semantic Actor field agree.
            CreatedBy = actor,
        });
    }

    public async Task<PaginatedResponse<AuditLog>> GetAuditLogsAsync(AuditLogFilteredRequest filter)
    {
        Expression<Func<AuditLog, bool>> expression =
            QueryableExtensions.ConstructFilterExpression<AuditLog, AuditLogFilteredRequest>(filter);

        IEnumerable<AuditLog> entries = await auditLogRepository.FindAsync(expression, filter: filter);
        int totalCount = await auditLogRepository.CountAsync(expression);

        return new PaginatedResponse<AuditLog>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = entries,
        };
    }
}
