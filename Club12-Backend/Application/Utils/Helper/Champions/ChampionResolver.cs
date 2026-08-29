using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Champions;

/// <summary>
/// Pure logic that resolves a division's podium from its elimination bracket,
/// with no data access. It decides which bracket is the division's TOP cup
/// (the one seeded from position #1, e.g. "Copa de Oro"), then reads that
/// bracket's Final and third-place outcomes. Best-of-N rounds are decided by
/// the <see cref="MatchSeries"/> winner; single-game rounds by the match's
/// <see cref="Match.WinningTeamId"/>. Nothing is guessed: an undecided final
/// yields a null champion.
/// </summary>
public static class ChampionResolver
{
    /// <summary>A team occupying a podium place.</summary>
    public sealed record TeamRef(Guid TeamId, string TeamName, string LogoUrl);

    /// <summary>The three podium places; any place may be null when undecided.</summary>
    public sealed class Podium
    {
        public TeamRef? First { get; init; }
        public TeamRef? Second { get; init; }
        public TeamRef? Third { get; init; }
    }

    /// <summary>
    /// Resolves the podium of a division that has a playoff. The top cup is the
    /// bracket seeded from standings position #1: when the division maps several
    /// cups (HU-45/HU-81) it is the destination of the mapping with the lowest
    /// <see cref="DivisionPlayoffMapping.FromPosition"/>; when there is a single
    /// bracket (the default cup or a cross-division cup) that one bracket is the
    /// top cup. 1st = the top cup's Final winner, 2nd = the Final loser, 3rd =
    /// the winner of that cup's <see cref="StageType.ThirdPlace"/> stage when one
    /// exists.
    /// </summary>
    /// <param name="eliminationStages">Every non-group stage of the division.</param>
    /// <param name="mappings">The division's position-range → cup mappings (may be empty).</param>
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

        return new Podium
        {
            First = finalOutcome is null ? null : Lookup(teamsById, finalOutcome.Value.Winner),
            Second = finalOutcome is null ? null : Lookup(teamsById, finalOutcome.Value.Loser),
            Third = thirdOutcome is null ? null : Lookup(teamsById, thirdOutcome.Value.Winner),
        };
    }

    /// <summary>
    /// Picks the division's top cup name. A single-bracket division (default cup
    /// or cross-division cup) has one distinct <see cref="Stage.BracketName"/>
    /// (often null) which is returned as-is. When several named cups coexist, the
    /// top cup is the one that receives seed #1 — the destination of the mapping
    /// with the lowest FromPosition — falling back to the first bracket name in
    /// ordinal order if the mappings do not name a known bracket.
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
    /// Reads a single elimination stage's decided outcome as a (winner, loser)
    /// pair. A best-of-N stage is decided by its <see cref="MatchSeries"/>: if a
    /// series exists for the stage it is authoritative, so an undecided series
    /// yields no outcome even when its individual games are finished. A
    /// single-game stage is decided by the finished match's winner (the latest
    /// by date, defensively, though a Final holds a single pairing). Returns null
    /// when nothing is decided yet.
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
