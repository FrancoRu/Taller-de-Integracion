using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="Stage"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{Stage}"/> and implements <see cref="IStageRepository"/>.
/// Utilizes <see cref="ApplicationDBContext"/> for data access.
/// </summary>
public class StageRepository(ApplicationDBContext context)
    : GenericRepository<Stage>(context), IStageRepository
{
}
