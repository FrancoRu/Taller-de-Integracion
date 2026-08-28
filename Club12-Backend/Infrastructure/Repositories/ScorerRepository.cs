using Application.DTOs.Scorer.Request;
using Application.DTOs.Scorer.Response;
using Application.Interfaces.Repositories;

using Domain.Entities.Models;
using Domain.Enums;

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
        {
            playersQuery = playersQuery.Where(p => p.Team.TournamentId == filter.TournamentId.Value);
        }

        if (filter.MatchId.HasValue)
        {
            Guid matchId = filter.MatchId.Value;
            playersQuery = playersQuery.Where(p =>
                _context.Set<Match>().Any(m => m.Id == matchId &&
                    (m.HomeTeamId == p.TeamId || m.VisitorTeamId == p.TeamId)));
        }

        if (filter.TeamId.HasValue)
        {
            playersQuery = playersQuery.Where(p => p.TeamId == filter.TeamId.Value);
        }

        if (filter.PlayerId.HasValue)
        {
            playersQuery = playersQuery.Where(p => p.Id == filter.PlayerId.Value);
        }

        if (filter.DivisionId.HasValue)
        {
            Guid divisionId = filter.DivisionId.Value;
            playersQuery = playersQuery.Where(p =>
                _context.Set<StageTeamMatch>().Any(stm => stm.TeamId == p.TeamId && stm.Stage!.DivisionId == divisionId));
        }

        if (filter.StageId.HasValue)
        {
            Guid stageId = filter.StageId.Value;
            playersQuery = playersQuery.Where(p =>
                _context.Set<StageTeamMatch>().Any(stm => stm.TeamId == p.TeamId && stm.StageId == stageId));
        }

        // HU-72: the goleadores ranking aggregates from PlayerStatistic — the
        // same table the per-match loading path (HU-71) writes to — so there is
        // no orphan Scorer write-gap and the ranking reflects real loaded
        // points. Only Points-type statistics count toward the scoring ranking.
        IQueryable<PlayerStatistic> scoreStatsQuery = _context.Set<PlayerStatistic>()
            .Where(s => s.Type == StatisticType.Points);

        if (filter.TournamentId.HasValue)
        {
            scoreStatsQuery = scoreStatsQuery.Where(s => s.Match!.Stage.Division.TournamentId == filter.TournamentId.Value);
        }

        if (filter.DivisionId.HasValue)
        {
            scoreStatsQuery = scoreStatsQuery.Where(s => s.Match!.Stage.DivisionId == filter.DivisionId.Value);
        }

        if (filter.StageId.HasValue)
        {
            scoreStatsQuery = scoreStatsQuery.Where(s => s.Match!.StageId == filter.StageId.Value);
        }

        if (filter.MatchId.HasValue)
        {
            scoreStatsQuery = scoreStatsQuery.Where(s => s.MatchId == filter.MatchId.Value);
        }

        // HU-85 season / all-time scopes. A "season" is the calendar year of a
        // tournament's StartDate (the simplest consistent value, no schema
        // change). When a Season is given, restrict BOTH the ranked players and
        // the summed points to tournaments that started that year; when neither
        // TournamentId nor Season is given, nothing here narrows the sum, so the
        // same player row aggregates every point across every season — the
        // all-time ranking. Grouping is by PlayerId: because Player.DocumentNumber
        // is uniquely indexed, one real person is exactly one Player row reused
        // across seasons (season scoping lives in PlayerTeamRegistration, not in
        // duplicate Player rows), so a PlayerId already IS the cross-season person.
        if (filter.Season.HasValue)
        {
            int season = filter.Season.Value;

            scoreStatsQuery = scoreStatsQuery.Where(s =>
                _context.Set<Tournament>().Any(t =>
                    t.Id == s.Match!.Stage.Division.TournamentId && t.StartDate.Year == season));

            playersQuery = playersQuery.Where(p => scoreStatsQuery.Any(s => s.PlayerId == p.Id));
        }

        IQueryable<ScorerByPlayerResponse> query = playersQuery.Select(player => new ScorerByPlayerResponse
        {
            PlayerId = player.Id,
            FullName = (player.SecondName == null || player.SecondName == "")
                ? player.LastName.ToUpper() + " " + player.FirstName
                : player.LastName.ToUpper() + " " + player.FirstName + " " + player.SecondName,
            Points = scoreStatsQuery
                .Where(s => s.PlayerId == player.Id)
                .Sum(s => (int?) s.Value) ?? 0
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
