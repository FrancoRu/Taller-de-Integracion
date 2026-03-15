using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for <see cref="Venue"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{Venue}"/> and implements <see cref="IVenueRepository"/> interface.
/// Utilizes <see cref="ApplicationDBContext"/> for data access.
/// </summary>
/// <remarks>
/// This repository provides data access logic specific to venues, enabling separation of concerns and testability.
/// </remarks>
/// <param name="context">The database context used for data access operations.</param>
public class VenueRepository(ApplicationDBContext context) 
    : GenericRepository<Venue>(context), IVenueRepository 
{
}
    