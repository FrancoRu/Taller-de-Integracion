using Domain.Entities.Models;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing AuditLog entries.
/// </summary>
public interface IAuditLogRepository : IGenericRepository<AuditLog>
{
}
