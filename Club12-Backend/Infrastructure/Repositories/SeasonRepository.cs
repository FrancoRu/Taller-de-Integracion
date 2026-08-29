using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Season entities.
/// Inherits generic CRUD operations from GenericRepository{Season} and implements ISeasonRepository interface.
/// Utilizes ApplicationDBContext for data access.
/// </summary>
/// <remarks>
/// This repository provides data access logic specific to seasons, enabling separation of concerns and testability.
/// </remarks>
/// <param name="context">The database context used for data access operations.</param>
public class SeasonRepository(ApplicationDBContext context)
    : GenericRepository<Season>(context), ISeasonRepository
{
}
