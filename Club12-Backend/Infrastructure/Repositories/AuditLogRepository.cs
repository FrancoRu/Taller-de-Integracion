using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for <see cref="AuditLog"/> entries (HU-101).
/// Inherits generic CRUD from GenericRepository{AuditLog}.
/// </summary>
/// <param name="context">The application's database context.</param>
public class AuditLogRepository(ApplicationDBContext context)
    : GenericRepository<AuditLog>(context), IAuditLogRepository
{
}
