using Application.DTOs.Scorer.Request;
using Application.DTOs.Scorer.Response;
using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class ScorerRepository(ApplicationDBContext context)
    : GenericRepository<Scorer>(context), IScorerRepository
{
    public async Task<(IEnumerable<ScorerByPlayerResponse> Items, int TotalCount)> GetPlayerScoresAsync(GetScorerFilteredRequest filter)
    {
        IQueryable<Player> playersQuery = _context.Set<Player>();

        if (filter.TournamentId.HasValue)
            playersQuery = playersQuery.Where(p => p.Team.TournamentId == filter.TournamentId.Value);

        if (filter.MatchId.HasValue)
        {
            Guid matchId = filter.MatchId.Value;
            playersQuery = playersQuery.Where(p =>
                _context.Set<Match>().Any(m => m.Id == matchId &&
                    (m.HomeTeamId == p.TeamId || m.VisitorTeamId == p.TeamId)));
        }

        if (filter.TeamId.HasValue)
            playersQuery = playersQuery.Where(p => p.TeamId == filter.TeamId.Value);

        if (filter.PlayerId.HasValue)
            playersQuery = playersQuery.Where(p => p.Id == filter.PlayerId.Value);

        IQueryable<Scorer> scorersQuery = _dbSet;

        if (filter.TournamentId.HasValue)
            scorersQuery = scorersQuery.Where(s => s.Match!.Stage.Division.TournamentId == filter.TournamentId.Value);

        if (filter.MatchId.HasValue)
            scorersQuery = scorersQuery.Where(s => s.MatchId == filter.MatchId.Value);

        IQueryable<ScorerByPlayerResponse> query = playersQuery.Select(player => new ScorerByPlayerResponse
        {
            PlayerId = player.Id,
            FullName = (player.SecondName == null || player.SecondName == "")
                ? player.LastName.ToUpper() + " " + player.FirstName
                : player.LastName.ToUpper() + " " + player.FirstName + " " + player.SecondName,
            Points = scorersQuery
                .Where(s => s.PlayerId == player.Id)
                .Sum(s => (int?)s.Points) ?? 0
        });

        int totalCount = await query.CountAsync();

        List<ScorerByPlayerResponse> items = await query
            .OrderByDescending(x => x.Points)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
