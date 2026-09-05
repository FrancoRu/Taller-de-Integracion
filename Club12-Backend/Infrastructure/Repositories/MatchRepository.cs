using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Match entities, inheriting generic CRUD and implementing IMatchRepository.
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
                .ThenInclude(team => team!.Players)
                    .ThenInclude(player => player.PlayerTeamRegistrations)
            .Include(match => match.VisitorTeam)
                .ThenInclude(team => team!.Players)
                    .ThenInclude(player => player.PlayerTeamRegistrations)
            .Include(match => match.Venue)
            // Stage → Division gives the match's tournament, used to pick each
            // scorer's jersey number from that tournament's roster registration.
            .Include(match => match.Stage)
                .ThenInclude(stage => stage.Division)
            .Include(match => match.Scorers)
                .ThenInclude(scorer => scorer.Player)
                    .ThenInclude(player => player!.PlayerTeamRegistrations)
            .AsSplitQuery();

        return Guid.TryParse(idOrSlug, out Guid matchId)
            ? await query.FirstOrDefaultAsync(match => match.Id == matchId)
            : await query.FirstOrDefaultAsync(match => match.Slug == idOrSlug);
    }
}
