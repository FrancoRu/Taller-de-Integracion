using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Venue entities, inheriting generic CRUD from GenericRepository and implementing IVenueRepository.
/// </summary>
/// <param name="context">The database context used for data access operations.</param>
public class VenueRepository(ApplicationDBContext context)
    : GenericRepository<Venue>(context), IVenueRepository
{
}
