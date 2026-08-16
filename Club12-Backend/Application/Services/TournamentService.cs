using Application.DTOs.Abstract.Response;
using Application.DTOs.Tournament.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class TournamentService(ITournamentRepository tournamentRepository) : ITournamentService
{
    public async Task<Tournament> CreateTournamentAsync(Tournament tournamentEntity)
    {
        await tournamentRepository.AddAsync(tournamentEntity);
        return tournamentEntity;
    }

    public async Task<Tournament?> GetTournamentByIdAsync(Guid tournamentId)
    {
        return await tournamentRepository.GetByIdAsync(tournamentId, includes: [tournament => tournament.Divisions]);
    }

    public async Task DeleteTournamentAsync(Guid id)
    {
        await tournamentRepository.RemoveAsync(tournament => tournament.Id == id);
    }

    public async Task UpdateTournamentAsync(Tournament tournamentEntity)
    {
        await tournamentRepository.UpdateAsync(tournamentEntity);
    }

    public async Task<PaginatedResponse<Tournament>> GetAllTournamentsAsync(GetTournamentsFilteredRequest filter)
    {
        Expression<Func<Tournament, bool>> expression = QueryableExtensions.ConstructFilterExpression<Tournament, GetTournamentsFilteredRequest>(filter);
        IEnumerable<Tournament> filteredTournaments = await tournamentRepository.FindAsync(expression, filter: filter);
        int totalCount = await tournamentRepository.CountAsync(expression);

        return new PaginatedResponse<Tournament>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredTournaments
        };
    }
}
