using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for AuditLog entries, inheriting generic CRUD from GenericRepository.
/// </summary>
/// <param name="context">The application's database context.</param>
public class AuditLogRepository(ApplicationDBContext context)
    : GenericRepository<AuditLog>(context), IAuditLogRepository
{
}
