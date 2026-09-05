using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Stage entities, inheriting generic CRUD from GenericRepository and implementing IStageRepository.
/// </summary>
public class StageRepository(ApplicationDBContext context)
    : GenericRepository<Stage>(context), IStageRepository
{
}
