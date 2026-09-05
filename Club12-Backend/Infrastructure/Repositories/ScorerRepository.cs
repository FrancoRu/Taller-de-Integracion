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

/// <summary>
/// Repository implementation for the top-scorers "goleadores" ranking, built from PlayerStatistic rows instead of the Scorer table.
/// </summary>
public class ScorerRepository(ApplicationDBContext context)
    : GenericRepository<Scorer>(context), IScorerRepository
{
    /// <summary>
    /// Builds the top-scorers ranking by summing each player's Points-type PlayerStatistic rows instead of reading the Scorer table, keeping it in sync with the per-match loading path.
    /// </summary>
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

        // The goleadores ranking aggregates from PlayerStatistic, the same table the per-match loading path writes to, so there is no orphan Scorer write-gap and the ranking reflects real loaded points; only Points-type statistics count toward it.
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

        // A season is the calendar year of a tournament's StartDate, and grouping is safe by PlayerId because Player.DocumentNumber is uniquely indexed, so one real person is exactly one Player row reused across seasons rather than duplicated per season.
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
                .Sum(s => (int?) s.Value) ?? 0,
            TeamId = player.TeamId,
            TeamName = player.Team.Name,
            TeamLogoUrl = player.Team.LogoUrl,
            TeamShirtColor = player.Team.ShirtColor,
            TeamJerseyStyle = player.Team.JerseyStyle,
            TeamShirtSecondaryColor = player.Team.ShirtSecondaryColor,
            TeamShirtTertiaryColor = player.Team.ShirtTertiaryColor,
            // The dorsal is season-scoped through PlayerTeamRegistration rather than a property of Player itself, so a tournament-scoped ranking uses that season's registration while a season or all-time aggregate spanning several registrations falls back to the player's most recent registration with their current team, since there is no single dorsal to show.
            JerseyNumber = filter.TournamentId.HasValue
                ? _context.Set<PlayerTeamRegistration>()
                    .Where(r => r.PlayerId == player.Id && r.TournamentId == filter.TournamentId.Value)
                    .Select(r => r.JerseyNumber)
                    .FirstOrDefault()
                : _context.Set<PlayerTeamRegistration>()
                    .Where(r => r.PlayerId == player.Id && r.TeamId == player.TeamId)
                    .OrderByDescending(r => r.DateCreated)
                    .Select(r => r.JerseyNumber)
                    .FirstOrDefault(),
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
