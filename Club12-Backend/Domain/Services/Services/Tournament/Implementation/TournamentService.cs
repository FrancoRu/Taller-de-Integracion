using Entities.DTOs.Abstract;
using Entities.DTOs.Tournament;
using Entities.Models.TournamentEntity;
using Microsoft.EntityFrameworkCore;
using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;
using System.Linq.Expressions;

namespace Services.Services.TournamentService.Implementation;

public class TournamentService(IGenericService<Tournament> genericTournamentService) : ITournamentService
{
    public Tournament CreateTournament(Tournament tournamentEntity)
    {
        genericTournamentService.Insert(tournamentEntity);
        return tournamentEntity;
    }

    public Tournament? GetTournamentById(Guid tournamentId)
    {
        return genericTournamentService.FilterByExpression(tournament => tournament.Id == tournamentId)
                                       .Include(tournament => tournament.Divisions)
                                       .FirstOrDefault();
    }

    public void DeleteTournament(Tournament tournamentEntity)
    {
        genericTournamentService.Delete(tournamentEntity);
    }

    public async Task<bool> UpdateTournament(Tournament tournamentEntity)
    {
        try
        {
            await genericTournamentService.UpdateAsync(tournamentEntity);
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
        IQueryable<Tournament> filteredTournaments = genericTournamentService.FilterByExpressionWithPagination(expression, filter).SortBy(filter);
        int totalCount = await genericTournamentService.GetCountAsync(expression);

        return new PaginatedResponse<Tournament>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredTournaments.ToListAsync()
        };
    }
}
