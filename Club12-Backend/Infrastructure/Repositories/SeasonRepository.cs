using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Season entities, inheriting generic CRUD from GenericRepository and implementing ISeasonRepository.
/// </summary>
/// <param name="context">The database context used for data access operations.</param>
public class SeasonRepository(ApplicationDBContext context)
    : GenericRepository<Season>(context), ISeasonRepository
{
}
