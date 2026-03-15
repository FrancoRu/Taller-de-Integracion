using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="Match"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{Match}"/> and implements <see cref="IMatchRepository"/> interface.
/// </summary>
/// <param name="context">The application's database context used for data access.</param>
public class MatchRepository(ApplicationDBContext context)
    : GenericRepository<Match>(context), IMatchRepository
{
}