using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Venue entities.
/// Inherits generic CRUD operations from GenericRepository{Venue} and implements IVenueRepository interface.
/// Utilizes ApplicationDBContext for data access.
/// </summary>
/// <remarks>
/// This repository provides data access logic specific to venues, enabling separation of concerns and testability.
/// </remarks>
/// <param name="context">The database context used for data access operations.</param>
public class VenueRepository(ApplicationDBContext context) 
    : GenericRepository<Venue>(context), IVenueRepository 
{
}
    