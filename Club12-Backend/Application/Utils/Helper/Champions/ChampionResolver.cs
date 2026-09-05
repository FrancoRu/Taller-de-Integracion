using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Champions;

/// <summary>
/// Pure logic that resolves a division's podium from its elimination bracket, with no data access.
/// </summary>
public static class ChampionResolver
{
    /// <summary>
    /// A team occupying a podium place.
    /// </summary>
    public sealed record TeamRef(Guid TeamId, string TeamName, string LogoUrl);

    /// <summary>
    /// The three podium places; any place may be null when undecided.
    /// </summary>
    public sealed class Podium
    {
        public TeamRef? First { get; init; }
        public TeamRef? Second { get; init; }
        public TeamRef? Third { get; init; }

        /// <summary>
        /// True when Third is null only because the top cup never had a real bracket round to draw a third place from.
        /// </summary>
        public bool ImplicitThirdFromStandings { get; init; }
    }

    private static readonly HashSet<StageType> BracketDepthStageTypes =
    [
        StageType.RoundOf16,
        StageType.QuarterFinal,
        StageType.SemiFinal,
    ];

    /// <summary>
    /// The champion of a single sub-cup, a playoff bracket, of a division.
    /// </summary>
    public sealed record CupChampion(string? CupName, int SeedOrder, TeamRef Champion);

    /// <summary>
    /// Resolves the podium of a division that has a playoff.
    /// </summary>
    /// <param name="eliminationStages">Every non-group stage of the division.</param>
    /// <param name="mappings">The division's position-range to cup mappings, which may be empty.</param>
    /// <param name="eliminationMatches">The finished/seeded matches of the elimination stages, with their team navigations loaded.</param>
    /// <param name="series">The division's best-of-N series, with their team navigations loaded.</param>
    public static Podium ResolvePlayoffPodium(
        IReadOnlyList<Stage> eliminationStages,
        IReadOnlyList<DivisionPlayoffMapping> mappings,
        IReadOnlyList<Match> eliminationMatches,
        IReadOnlyList<MatchSeries> series)
    {
        string? topBracket = ResolveTopBracketName(eliminationStages, mappings);

        Stage? finalStage = eliminationStages
            .Where(stage => stage.BracketName == topBracket && stage.StageType == StageType.Final)
            .OrderBy(stage => stage.Order)
            .FirstOrDefault();

        Stage? thirdPlaceStage = eliminationStages
            .Where(stage => stage.BracketName == topBracket && stage.StageType == StageType.ThirdPlace)
            .OrderBy(stage => stage.Order)
            .FirstOrDefault();

        Dictionary<Guid, TeamRef> teamsById = BuildTeamLookup(eliminationMatches, series);

        (Guid Winner, Guid Loser)? finalOutcome = finalStage is null
            ? null
            : ResolveStageOutcome(finalStage, eliminationMatches, series);

        (Guid Winner, Guid Loser)? thirdOutcome = thirdPlaceStage is null
            ? null
            : ResolveStageOutcome(thirdPlaceStage, eliminationMatches, series);

        TeamRef? third = thirdOutcome is null ? null : Lookup(teamsById, thirdOutcome.Value.Winner);

        bool hasBracketDepth = eliminationStages
            .Any(stage => stage.BracketName == topBracket && BracketDepthStageTypes.Contains(stage.StageType));

        return new Podium
        {
            First = finalOutcome is null ? null : Lookup(teamsById, finalOutcome.Value.Winner),
            Second = finalOutcome is null ? null : Lookup(teamsById, finalOutcome.Value.Loser),
            Third = third,
            ImplicitThirdFromStandings = third is null && !hasBracketDepth,
        };
    }

    /// <summary>
    /// Resolves the champion of every sub-cup, a playoff bracket, of a division, not just the top one.
    /// </summary>
    public static IReadOnlyList<CupChampion> ResolveCupChampions(
        IReadOnlyList<Stage> eliminationStages,
        IReadOnlyList<DivisionPlayoffMapping> mappings,
        IReadOnlyList<Match> eliminationMatches,
        IReadOnlyList<MatchSeries> series)
    {
        List<string?> bracketNames = [.. eliminationStages
            .Select(stage => stage.BracketName)
            .Distinct()];

        bool singleBracket = bracketNames.Count <= 1;
        Dictionary<Guid, TeamRef> teamsById = BuildTeamLookup(eliminationMatches, series);

        // The cup receiving the lowest FromPosition is the top seed tier; brackets unnamed by any mapping sort after, by name.
        Dictionary<string, int> seedByBracket = mappings
            .GroupBy(mapping => mapping.Destination)
            .ToDictionary(group => group.Key, group => group.Min(m => m.FromPosition));

        List<CupChampion> champions = [];

        foreach (string? bracketName in bracketNames)
        {
            Stage? finalStage = eliminationStages
                .Where(stage => stage.BracketName == bracketName && stage.StageType == StageType.Final)
                .OrderBy(stage => stage.Order)
                .FirstOrDefault();

            if (finalStage is null)
            {
                continue;
            }

            (Guid Winner, Guid Loser)? outcome = ResolveStageOutcome(finalStage, eliminationMatches, series);
            TeamRef? champion = outcome is null ? null : Lookup(teamsById, outcome.Value.Winner);

            if (champion is null)
            {
                continue;
            }

            int seedOrder = bracketName is not null && seedByBracket.TryGetValue(bracketName, out int seed)
                ? seed
                : int.MaxValue;

            champions.Add(new CupChampion(singleBracket ? null : bracketName, seedOrder, champion));
        }

        return [.. champions
            .OrderBy(c => c.SeedOrder)
            .ThenBy(c => c.CupName, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Picks the division's top cup name.
    /// </summary>
    private static string? ResolveTopBracketName(
        IReadOnlyList<Stage> eliminationStages,
        IReadOnlyList<DivisionPlayoffMapping> mappings)
    {
        List<string?> bracketNames = [.. eliminationStages
            .Select(stage => stage.BracketName)
            .Distinct()];

        if (bracketNames.Count <= 1)
        {
            return bracketNames.FirstOrDefault();
        }

        DivisionPlayoffMapping? topMapping = mappings
            .OrderBy(mapping => mapping.FromPosition)
            .FirstOrDefault();

        if (topMapping is not null && bracketNames.Contains(topMapping.Destination))
        {
            return topMapping.Destination;
        }

        return bracketNames
            .Where(name => name is not null)
            .OrderBy(name => name, StringComparer.Ordinal)
            .FirstOrDefault() ?? bracketNames[0];
    }

    /// <summary>
    /// Reads a single elimination stage's decided outcome as a winner and loser pair.
    /// </summary>
    private static (Guid Winner, Guid Loser)? ResolveStageOutcome(
        Stage stage,
        IReadOnlyList<Match> eliminationMatches,
        IReadOnlyList<MatchSeries> series)
    {
        List<MatchSeries> stageSeries = [.. series.Where(s => s.StageId == stage.Id)];

        if (stageSeries.Count > 0)
        {
            MatchSeries? decided = stageSeries.Find(s => s.WinningTeamId.HasValue);
            if (decided is null)
            {
                return null;
            }

            Guid seriesWinner = decided.WinningTeamId!.Value;
            Guid seriesLoser = seriesWinner == decided.HomeTeamId ? decided.VisitorTeamId : decided.HomeTeamId;
            return (seriesWinner, seriesLoser);
        }

        Match? match = eliminationMatches
            .Where(m => m.StageId == stage.Id
                && m.IsFinished
                && m.WinningTeamId.HasValue
                && m.HomeTeamId.HasValue
                && m.VisitorTeamId.HasValue)
            .OrderByDescending(m => m.MatchDate)
            .FirstOrDefault();

        if (match is null)
        {
            return null;
        }

        Guid winner = match.WinningTeamId!.Value;
        Guid loser = winner == match.HomeTeamId!.Value ? match.VisitorTeamId!.Value : match.HomeTeamId!.Value;
        return (winner, loser);
    }

    private static Dictionary<Guid, TeamRef> BuildTeamLookup(
        IReadOnlyList<Match> eliminationMatches,
        IReadOnlyList<MatchSeries> series)
    {
        Dictionary<Guid, TeamRef> teamsById = [];

        foreach (Match match in eliminationMatches)
        {
            AddTeam(teamsById, match.HomeTeam);
            AddTeam(teamsById, match.VisitorTeam);
            AddTeam(teamsById, match.WinningTeam);
        }

        foreach (MatchSeries s in series)
        {
            AddTeam(teamsById, s.HomeTeam);
            AddTeam(teamsById, s.VisitorTeam);
            AddTeam(teamsById, s.WinningTeam);
        }

        return teamsById;
    }

    private static void AddTeam(Dictionary<Guid, TeamRef> teamsById, Team? team)
    {
        if (team is null || teamsById.ContainsKey(team.Id))
        {
            return;
        }

        teamsById[team.Id] = new TeamRef(team.Id, team.Name, team.LogoUrl);
    }

    private static TeamRef? Lookup(Dictionary<Guid, TeamRef> teamsById, Guid teamId)
    {
        return teamsById.TryGetValue(teamId, out TeamRef? team) ? team : null;
    }
}
