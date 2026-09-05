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
/// Repository implementation for the "goleadores" (top scorers) ranking.
/// Inherits generic CRUD for <see cref="Scorer"/> from GenericRepository{Scorer},
/// but the ranking itself is built from PlayerStatistic rows, not the Scorer table
/// (see <see cref="GetPlayerScoresAsync"/>).
/// </summary>
public class ScorerRepository(ApplicationDBContext context)
    : GenericRepository<Scorer>(context), IScorerRepository
{
    /// <summary>
    /// Builds the top-scorers ranking by summing each player's Points-type
    /// <see cref="PlayerStatistic"/> rows (HU-72) rather than reading the Scorer
    /// table, so it always reflects what the per-match loading path (HU-71)
    /// actually wrote. When <paramref name="filter"/> narrows to a
    /// tournament/division/stage/match, both the candidate players and the
    /// summed points are scoped accordingly; when neither TournamentId nor
    /// Season is set, points aggregate across every season for that player
    /// (an all-time ranking) since Player rows are reused across seasons.
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
                .Sum(s => (int?) s.Value) ?? 0,
            TeamId = player.TeamId,
            TeamName = player.Team.Name,
            TeamLogoUrl = player.Team.LogoUrl,
            TeamShirtColor = player.Team.ShirtColor,
            TeamJerseyStyle = player.Team.JerseyStyle,
            TeamShirtSecondaryColor = player.Team.ShirtSecondaryColor,
            TeamShirtTertiaryColor = player.Team.ShirtTertiaryColor,
            // The dorsal is season-scoped (PlayerTeamRegistration), not a
            // property of Player itself: when the ranking is scoped to one
            // tournament, use that season's registration; otherwise (a
            // season/all-time aggregate spanning several registrations) fall
            // back to the player's most recent registration with their
            // current team, since there is no single "the" dorsal to show.
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
