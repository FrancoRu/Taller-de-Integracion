using Application.DTOs.Champions.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Helper.Champions;

using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Computes the champion and podium of each competition (a zone division or the
/// cross-division cup) and the champions history over finished tournaments.
/// A division with an elimination bracket is crowned by its top cup's Final;
/// a group-only division is crowned by its standings leader. Standings reuse
/// <see cref="IDivisionService.GetPositionsByDivisionIdAsync"/> and bracket
/// resolution reuses <see cref="ChampionResolver"/>, so no ranking or bracket
/// logic is reinvented here.
/// </summary>
public class ChampionService(
    IDivisionRepository divisionRepository,
    IMatchRepository matchRepository,
    IMatchSeriesRepository matchSeriesRepository,
    ITournamentRepository tournamentRepository,
    IDivisionService divisionService) : IChampionService
{
    /// <summary>
    /// Computes a division's podium (see <see cref="IChampionService.GetDivisionPodiumAsync"/>).
    /// </summary>
    public async Task<PodiumResponse?> GetDivisionPodiumAsync(Guid divisionId)
    {
        Division? division = await divisionRepository.GetByIdAsync(
            divisionId,
            includes: [d => d.Stages, d => d.PlayoffMappings]);

        if (division is null)
        {
            return null;
        }

        return await BuildDivisionPodiumAsync(division);
    }

    /// <summary>
    /// Computes the podium of every division of a tournament
    /// (see <see cref="IChampionService.GetTournamentChampionsAsync"/>).
    /// </summary>
    public async Task<List<PodiumResponse>> GetTournamentChampionsAsync(Guid tournamentId)
    {
        List<Division> divisions = [.. await divisionRepository.FindAsync(
            division => division.TournamentId == tournamentId,
            includes: [d => d.Stages, d => d.PlayoffMappings])];

        List<PodiumResponse> podiums = [];

        foreach (Division division in divisions.OrderBy(d => d.Name, StringComparer.Ordinal))
        {
            podiums.Add(await BuildDivisionPodiumAsync(division));
        }

        return podiums;
    }

    /// <summary>
    /// Returns the champion of every division of every finished tournament
    /// (see <see cref="IChampionService.GetChampionsHistoryAsync"/>).
    /// </summary>
    public async Task<List<ChampionHistoryResponse>> GetChampionsHistoryAsync(Guid? seasonId)
    {
        IEnumerable<Tournament> finishedTournaments = seasonId.HasValue
            ? await tournamentRepository.FindAsync(
                tournament => tournament.Status == TournamentStatus.Finished && tournament.SeasonId == seasonId,
                includes: [t => t.Season!, t => t.Divisions])
            : await tournamentRepository.FindAsync(
                tournament => tournament.Status == TournamentStatus.Finished,
                includes: [t => t.Season!, t => t.Divisions]);

        List<ChampionHistoryResponse> history = [];

        foreach (Tournament tournament in finishedTournaments)
        {
            foreach (Division division in tournament.Divisions)
            {
                // One row PER sub-cup (Copa Oro, Copa Plata, …). A single-bracket
                // or group-only division yields exactly one row with a null CupName.
                List<(string? CupName, PodiumTeamResponse Champion)> champions =
                    await GetDivisionChampionsAsync(division.Id);

                foreach ((string? cupName, PodiumTeamResponse champion) in champions)
                {
                    history.Add(new ChampionHistoryResponse
                    {
                        TournamentId = tournament.Id,
                        TournamentName = tournament.Name,
                        SeasonName = tournament.Season?.Name,
                        Category = division.Category.ToString(),
                        DivisionName = division.Name,
                        CupName = cupName,
                        ChampionTeam = champion,
                    });
                }
            }
        }

        return history;
    }

    /// <summary>
    /// Resolves the champion of every sub-cup of a division (Copa Oro, Copa
    /// Plata, …), or the standings leader for a group-only division. Each entry's
    /// CupName is null when the division crowns a single champion. Undecided cups
    /// are omitted.
    /// </summary>
    private async Task<List<(string? CupName, PodiumTeamResponse Champion)>> GetDivisionChampionsAsync(Guid divisionId)
    {
        Division? division = await divisionRepository.GetByIdAsync(
            divisionId,
            includes: [d => d.Stages, d => d.PlayoffMappings]);

        if (division is null)
        {
            return [];
        }

        List<Stage> eliminationStages = [.. division.Stages.Where(stage => stage.StageType != StageType.Group)];

        if (eliminationStages.Count > 0)
        {
            (List<Match> matches, List<MatchSeries> series) = await LoadEliminationDataAsync(eliminationStages);

            IReadOnlyList<ChampionResolver.CupChampion> cupChampions = ChampionResolver.ResolveCupChampions(
                eliminationStages,
                [.. division.PlayoffMappings],
                matches,
                series);

            return [.. cupChampions.Select(cup => (cup.CupName, ToTeamResponse(cup.Champion)!))];
        }

        List<Position> standings = await divisionService.GetPositionsByDivisionIdAsync(division.Id);
        PodiumTeamResponse? leader = ToTeamResponse(standings.ElementAtOrDefault(0));

        return leader is null ? [] : [((string?)null, leader)];
    }

    /// <summary>
    /// Builds one division's podium from an already-loaded division (with its
    /// stages and playoff mappings). Divisions with any elimination stage are
    /// resolved through the bracket; the rest through the standings.
    /// </summary>
    private async Task<PodiumResponse> BuildDivisionPodiumAsync(Division division)
    {
        List<Stage> eliminationStages = [.. division.Stages.Where(stage => stage.StageType != StageType.Group)];

        if (eliminationStages.Count > 0)
        {
            return await BuildPlayoffPodiumAsync(division, eliminationStages);
        }

        return await BuildStandingsPodiumAsync(division);
    }

    /// <summary>
    /// Resolves a playoff division's podium from its top cup's Final and
    /// third-place match, loading only the elimination stages' matches and
    /// series.
    /// </summary>
    private async Task<PodiumResponse> BuildPlayoffPodiumAsync(Division division, List<Stage> eliminationStages)
    {
        (List<Match> eliminationMatches, List<MatchSeries> series) = await LoadEliminationDataAsync(eliminationStages);

        ChampionResolver.Podium podium = ChampionResolver.ResolvePlayoffPodium(
            eliminationStages,
            [.. division.PlayoffMappings],
            eliminationMatches,
            series);

        return new PodiumResponse
        {
            DivisionId = division.Id,
            DivisionName = division.Name,
            HasPlayoff = true,
            First = ToTeamResponse(podium.First),
            Second = ToTeamResponse(podium.Second),
            Third = ToTeamResponse(podium.Third),
        };
    }

    /// <summary>
    /// Resolves a group-only division's podium from the top three of its group
    /// standings, reusing the shared position calculator.
    /// </summary>
    private async Task<PodiumResponse> BuildStandingsPodiumAsync(Division division)
    {
        List<Position> standings = await divisionService.GetPositionsByDivisionIdAsync(division.Id);

        return new PodiumResponse
        {
            DivisionId = division.Id,
            DivisionName = division.Name,
            HasPlayoff = false,
            First = ToTeamResponse(standings.ElementAtOrDefault(0)),
            Second = ToTeamResponse(standings.ElementAtOrDefault(1)),
            Third = ToTeamResponse(standings.ElementAtOrDefault(2)),
        };
    }

    /// <summary>
    /// Loads the matches and best-of-N series of a set of elimination stages,
    /// with their team navigations, so the bracket resolver can read outcomes.
    /// </summary>
    private async Task<(List<Match> Matches, List<MatchSeries> Series)> LoadEliminationDataAsync(
        List<Stage> eliminationStages)
    {
        List<Guid> eliminationStageIds = [.. eliminationStages.Select(stage => stage.Id)];

        List<Match> eliminationMatches = [.. await matchRepository.FindAsync(
            match => eliminationStageIds.Contains(match.StageId),
            includes: [m => m.HomeTeam!, m => m.VisitorTeam!, m => m.WinningTeam!])];

        List<MatchSeries> series = [.. await matchSeriesRepository.FindAsync(
            matchSeries => eliminationStageIds.Contains(matchSeries.StageId),
            includes: [s => s.HomeTeam!, s => s.VisitorTeam!, s => s.WinningTeam!])];

        return (eliminationMatches, series);
    }

    private static PodiumTeamResponse? ToTeamResponse(ChampionResolver.TeamRef? team)
    {
        return team is null
            ? null
            : new PodiumTeamResponse
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                LogoUrl = team.LogoUrl,
            };
    }

    private static PodiumTeamResponse? ToTeamResponse(Position? position)
    {
        return position is null
            ? null
            : new PodiumTeamResponse
            {
                TeamId = position.TeamId,
                TeamName = position.TeamName,
                LogoUrl = position.LogoUrl,
            };
    }
}
