using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Division entities, inheriting generic CRUD and implementing IDivisionRepository.
/// </summary>
public class DivisionRepository(ApplicationDBContext context)
    : GenericRepository<Division>(context), IDivisionRepository
{
}