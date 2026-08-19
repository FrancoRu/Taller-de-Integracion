using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Stage entities.
/// Inherits generic CRUD operations from GenericRepository{Stage} and implements IStageRepository.
/// Utilizes ApplicationDBContext for data access.
/// </summary>
public class StageRepository(ApplicationDBContext context)
    : GenericRepository<Stage>(context), IStageRepository
{
}
