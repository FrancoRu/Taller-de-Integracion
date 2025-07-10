using Entities.DTOs.Abstract;
using Entities.DTOs.Tournament;
using Entities.Models.Tournaments;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

namespace Services.Services.Tournaments.Implementation;

public class TournamentService(IGenericService<Tournament> _genericTournamentService) : ITournamentService
{
    public async Task<Tournament> CreateTournamentAsync(Tournament tournamentEntity)
    {
        await _genericTournamentService.InsertAsync(tournamentEntity);
        return tournamentEntity;
    }

    public async Task<Tournament?> GetTournamentByIdAsync(Guid tournamentId) => await _genericTournamentService.FilterByExpression(tournament => tournament.Id == tournamentId)
                                             .Include(tournament => tournament.Divisions)
                                             .FirstOrDefaultAsync();

    public async Task<bool> DeleteTournamentAsync(Tournament tournamentEntity)
    {
        try
        {
            await _genericTournamentService.DeleteAsync(tournamentEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateTournamentAsync(Tournament tournamentEntity)
    {
        try
        {
            await _genericTournamentService.UpdateAsync(tournamentEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PaginatedResponse<Tournament>> GetAllTournamentsAsync(GetTournamentsFilteredRequest filter)
    {
        Expression<Func<Tournament, bool>> expression = QueryableExtensions.ConstructFilterExpression<Tournament, GetTournamentsFilteredRequest>(filter);
        IQueryable<Tournament> filteredTournaments = _genericTournamentService.FilterByExpressionWithPagination(expression, filter, tournament => tournament.Divisions)
                                                                                                                                   .SortBy(filter);
        int totalCount = await _genericTournamentService.GetCountAsync(expression);

        return new PaginatedResponse<Tournament>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredTournaments.ToListAsync()
        };
    }
}
