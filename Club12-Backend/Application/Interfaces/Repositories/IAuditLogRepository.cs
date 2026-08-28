using Domain.Entities.Models;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing <see cref="AuditLog"/> entries (HU-101).
/// </summary>
public interface IAuditLogRepository : IGenericRepository<AuditLog>
{
}
