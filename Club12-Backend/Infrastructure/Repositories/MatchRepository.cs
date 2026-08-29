using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Match entities.
/// Inherits generic CRUD operations from GenericRepository{Match} and implements IMatchRepository interface.
/// </summary>
/// <param name="context">The application's database context used for data access.</param>
public class MatchRepository(ApplicationDBContext context)
    : GenericRepository<Match>(context), IMatchRepository
{
    /// <inheritdoc />
    public async Task<Match?> GetDetailByIdOrSlugAsync(string idOrSlug)
    {
        IQueryable<Match> query = _context.Set<Match>()
            .Include(match => match.HomeTeam)
            .Include(match => match.VisitorTeam)
            .Include(match => match.Venue)
            .Include(match => match.Scorers)
                .ThenInclude(scorer => scorer.Player)
            .AsSplitQuery();

        return Guid.TryParse(idOrSlug, out Guid matchId)
            ? await query.FirstOrDefaultAsync(match => match.Id == matchId)
            : await query.FirstOrDefaultAsync(match => match.Slug == idOrSlug);
    }
}
