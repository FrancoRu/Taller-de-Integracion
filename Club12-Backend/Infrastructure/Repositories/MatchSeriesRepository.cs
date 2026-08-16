using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing MatchSeries entities.
/// Inherits generic CRUD operations from GenericRepository{MatchSeries} and implements IMatchSeriesRepository interface.
/// </summary>
/// <param name="context">The application's database context used for data access.</param>
public class MatchSeriesRepository(ApplicationDBContext context)
    : GenericRepository<MatchSeries>(context), IMatchSeriesRepository
{
}
