using Application.DTOs.Statistics.Response;
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
/// Historical-statistics aggregations (HU-87 / HU-88). Every read groups a
/// person's rows across seasons by their stable PlayerId: because
/// Player.DocumentNumber is uniquely indexed, one real person is exactly one
/// Player row reused across seasons (season scoping lives in
/// PlayerTeamRegistration, not in duplicate Player rows), so a PlayerId already
/// IS the cross-season person. A "season" is the calendar year of the
/// tournament's StartDate (HU-85). Point aggregates come from PlayerStatistic
/// rows of Type Points — the same table the match-sheet load path writes
/// (HU-71) — scoped to a tournament through Match → Stage → Division →
/// TournamentId, the same chain the goleadores ranking uses.
/// </summary>
public class StatisticsRepository(ApplicationDBContext context) : IStatisticsRepository
{
    private readonly ApplicationDBContext _context = context;

    /// <inheritdoc/>
    public async Task<PlayerStatisticCardResponse?> GetPlayerCardAsync(Guid playerId)
    {
        Player? player = await _context.Set<Player>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player is null)
        {
            return null;
        }

        List<PointStatRow> rows = await _context.Set<PlayerStatistic>()
            .Where(s => s.PlayerId == playerId && s.Type == StatisticType.Points)
            .Select(s => new PointStatRow(s.MatchId, s.Value, s.Match!.Stage.Division.TournamentId))
            .ToListAsync();

        Dictionary<Guid, int> seasonByTournament = await BuildSeasonByTournamentAsync(rows.Select(r => r.TournamentId));

        List<SeasonStatLineResponse> seasons = [.. rows
            .GroupBy(r => seasonByTournament.TryGetValue(r.TournamentId, out int year) ? year : (int?)null)
            .Where(g => g.Key.HasValue)
            .Select(g => BuildSeasonLine(g.Key!.Value, g))
            .OrderByDescending(s => s.Season)];

        int totalPoints = rows.Sum(r => r.Value);
        int gamesPlayed = rows.Select(r => r.MatchId).Distinct().Count();

        return new PlayerStatisticCardResponse
        {
            PlayerId = player.Id,
            FullName = player.FullName,
            TotalPoints = totalPoints,
            GamesPlayed = gamesPlayed,
            AveragePoints = Average(totalPoints, gamesPlayed),
            Seasons = seasons,
        };
    }

    /// <inheritdoc/>
    public async Task<PlayerHistoryResponse?> GetPlayerHistoryAsync(Guid playerId)
    {
        Player? player = await _context.Set<Player>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player is null)
        {
            return null;
        }

        // Season/team come from the season-scoped registrations (HU-88 / D2):
        // the source of truth for "which team, which season", untouched by a
        // team being reassigned to a later tournament.
        List<RegistrationRow> registrations = await _context.Set<PlayerTeamRegistration>()
            .Where(r => r.PlayerId == playerId)
            .Select(r => new RegistrationRow(
                r.TournamentId,
                r.Tournament!.Name,
                r.Tournament!.StartDate,
                r.TeamId,
                r.Team!.Name))
            .ToListAsync();

        List<PointStatRow> statRows = await _context.Set<PlayerStatistic>()
            .Where(s => s.PlayerId == playerId && s.Type == StatisticType.Points)
            .Select(s => new PointStatRow(s.MatchId, s.Value, s.Match!.Stage.Division.TournamentId))
            .ToListAsync();

        Dictionary<Guid, (int Points, int Games)> statsByTournament = statRows
            .GroupBy(r => r.TournamentId)
            .ToDictionary(
                g => g.Key,
                g => (Points: g.Sum(x => x.Value), Games: g.Select(x => x.MatchId).Distinct().Count()));

        List<SanctionRow> sanctionRows = await _context.Set<PlayerSanction>()
            .Where(ps => ps.PlayerId == playerId)
            .Select(ps => new SanctionRow(
                ps.Id,
                ps.Description,
                ps.Duration,
                ps.IssuedDate,
                ps.MatchId,
                ps.Match.Stage.Division.TournamentId))
            .ToListAsync();

        Dictionary<Guid, List<PlayerHistorySanctionResponse>> sanctionsByTournament = sanctionRows
            .GroupBy(s => s.TournamentId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => new PlayerHistorySanctionResponse
                {
                    SanctionId = s.Id,
                    Description = s.Description,
                    Duration = s.Duration,
                    IssuedDate = s.IssuedDate,
                    MatchId = s.MatchId,
                }).ToList());

        List<PlayerHistorySeasonResponse> seasons = [.. registrations
            .Select(r =>
            {
                (int points, int games) = statsByTournament.TryGetValue(r.TournamentId, out (int Points, int Games) stat)
                    ? stat
                    : (0, 0);

                IEnumerable<PlayerHistorySanctionResponse> sanctions =
                    sanctionsByTournament.TryGetValue(r.TournamentId, out List<PlayerHistorySanctionResponse>? list)
                        ? list
                        : [];

                return new PlayerHistorySeasonResponse
                {
                    Season = r.StartDate.Year,
                    TournamentId = r.TournamentId,
                    TournamentName = r.TournamentName,
                    TeamId = r.TeamId,
                    TeamName = r.TeamName,
                    TotalPoints = points,
                    GamesPlayed = games,
                    Sanctions = sanctions,
                };
            })
            .OrderByDescending(s => s.Season)
            .ThenBy(s => s.TournamentName)];

        return new PlayerHistoryResponse
        {
            PlayerId = player.Id,
            FullName = player.FullName,
            Seasons = seasons,
        };
    }

    /// <summary>
    /// Resolves each tournament id to its season (StartDate year) in one query.
    /// The year is computed in memory (StartDate is fetched) so the season
    /// definition is provider-independent.
    /// </summary>
    private async Task<Dictionary<Guid, int>> BuildSeasonByTournamentAsync(IEnumerable<Guid> tournamentIds)
    {
        List<Guid> ids = [.. tournamentIds.Distinct()];
        if (ids.Count == 0)
        {
            return [];
        }

        List<TournamentSeasonRow> rows = await _context.Set<Tournament>()
            .Where(t => ids.Contains(t.Id))
            .Select(t => new TournamentSeasonRow(t.Id, t.StartDate))
            .ToListAsync();

        return rows.ToDictionary(r => r.Id, r => r.StartDate.Year);
    }

    private static SeasonStatLineResponse BuildSeasonLine(int season, IEnumerable<PointStatRow> rows)
    {
        int totalPoints = rows.Sum(r => r.Value);
        int gamesPlayed = rows.Select(r => r.MatchId).Distinct().Count();

        return new SeasonStatLineResponse
        {
            Season = season,
            TotalPoints = totalPoints,
            GamesPlayed = gamesPlayed,
            AveragePoints = Average(totalPoints, gamesPlayed),
        };
    }

    private static double Average(int totalPoints, int gamesPlayed) =>
        gamesPlayed == 0 ? 0 : Math.Round((double)totalPoints / gamesPlayed, 2);

    private sealed record PointStatRow(Guid MatchId, int Value, Guid TournamentId);

    private sealed record RegistrationRow(Guid TournamentId, string TournamentName, DateTime StartDate, Guid TeamId, string TeamName);

    private sealed record SanctionRow(Guid Id, string Description, int Duration, DateTime IssuedDate, Guid MatchId, Guid TournamentId);

    private sealed record TournamentSeasonRow(Guid Id, DateTime StartDate);
}
