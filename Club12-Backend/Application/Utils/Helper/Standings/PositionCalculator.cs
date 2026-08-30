using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Standings;

/// <summary>
/// Computes division standings from a set of matches. Basketball has no
/// draws: every finished, fully-seeded match has exactly one winner, so a
/// team either wins or loses — never a tie. The points awarded for a win and
/// a loss are configurable per division (HU-79, defaulting to 2/1), and ties
/// on table points are broken with the club's fixed group-stage tiebreaker
/// order (HU-80).
/// </summary>
public static class PositionCalculator
{
    public const int DefaultPointsForWin = 2;
    public const int DefaultPointsForLoss = 1;

    /// <summary>
    /// Builds one Position per team that appears in at least one finished,
    /// fully-seeded match, ordered by the HU-80 group-stage tiebreaker chain.
    /// Unfinished matches and matches missing a home or visitor team (not yet
    /// seeded) are ignored.
    /// </summary>
    /// <param name="matches">The zone's matches.</param>
    /// <param name="pointsForWin">Table points for a win (HU-79). Defaults to 2.</param>
    /// <param name="pointsForLoss">Table points for a loss (HU-79). Defaults to 1.</param>
    /// <param name="deductions">
    /// Disciplinary point deductions to subtract from the affected teams'
    /// totals before ranking. Each team's deductions are summed and subtracted
    /// from its <see cref="Position.Points"/>; the amount and reason are
    /// surfaced on <see cref="Position.PointDeduction"/>. The subtraction is
    /// raw and is never clamped — a heavy penalty may legitimately drop a
    /// team's total below zero and below other teams. Null/empty leaves every
    /// total untouched.
    /// </param>
    /// <param name="rosterTeams">
    /// Optional teams assigned to the zone. When provided, each is seeded into
    /// the table at 0-0 so every assigned team shows from the start, before any
    /// match is played. Null keeps the legacy behavior (only teams with a
    /// finished match appear).
    /// </param>
    public static List<Position> CalculatePositions(
        IEnumerable<Match> matches,
        int pointsForWin = DefaultPointsForWin,
        int pointsForLoss = DefaultPointsForLoss,
        IEnumerable<TeamPointDeduction>? deductions = null,
        IEnumerable<Team>? rosterTeams = null)
    {
        List<Match> finishedMatches = [.. matches.Where(IsSeededAndFinished)];

        Dictionary<Guid, Position> positionsByTeamId = [];

        // Seed every team assigned to the zone so the table shows all of them
        // (at 0-0) from the moment the tournament starts, before any match is
        // played — instead of an empty standings table until the first result.
        if (rosterTeams is not null)
        {
            foreach (Team team in rosterTeams)
            {
                EnsurePosition(positionsByTeamId, team);
            }
        }

        foreach (Match match in finishedMatches)
        {
            bool homeWon = match.WinningTeamId == match.HomeTeamId;

            ApplyResult(positionsByTeamId, match.HomeTeam!, match.HomeScore!.Value, match.VisitorScore!.Value, homeWon, pointsForWin, pointsForLoss);
            ApplyResult(positionsByTeamId, match.VisitorTeam!, match.VisitorScore.Value, match.HomeScore.Value, !homeWon, pointsForWin, pointsForLoss);
        }

        foreach (Position position in positionsByTeamId.Values)
        {
            position.PointsDifference = position.PointsFor - position.PointsAgainst;
        }

        // Subtract disciplinary deductions BEFORE ranking so the full HU-80
        // tiebreaker chain (PTS/PG/DG/H2H) sees the penalised totals and the
        // table re-orders correctly around a deducted team.
        ApplyDeductions(positionsByTeamId.Values, deductions);

        return OrderWithTiebreakers(positionsByTeamId.Values, finishedMatches, pointsForWin, pointsForLoss);
    }

    /// <summary>
    /// Subtracts each team's accumulated disciplinary deductions from its
    /// standings total (deducción de puntos). Deductions are summed per team,
    /// their reasons combined, and the result is recorded on
    /// <see cref="Position.PointDeduction"/>. Only teams that appear in the
    /// standings (i.e. have played at least one finished match) are affected.
    /// The subtraction is intentionally NOT clamped at zero: a penalty may sink
    /// a team below zero, mirroring real disciplinary rulings, and the value is
    /// displayed as-is.
    /// </summary>
    public static void ApplyDeductions(
        IEnumerable<Position> positions,
        IEnumerable<TeamPointDeduction>? deductions)
    {
        if (deductions is null)
        {
            return;
        }

        Dictionary<Guid, List<TeamPointDeduction>> byTeam = deductions
            .GroupBy(deduction => deduction.TeamId)
            .ToDictionary(group => group.Key, group => group.ToList());

        if (byTeam.Count == 0)
        {
            return;
        }

        foreach (Position position in positions)
        {
            if (!byTeam.TryGetValue(position.TeamId, out List<TeamPointDeduction>? teamDeductions))
            {
                continue;
            }

            int totalDeducted = teamDeductions.Sum(deduction => deduction.Points);
            position.Points -= totalDeducted;
            position.PointDeduction = new AppliedPointDeduction
            {
                Points = totalDeducted,
                Reason = string.Join("; ", teamDeductions.Select(deduction => deduction.Reason)),
            };
        }
    }

    private static bool IsSeededAndFinished(Match match)
    {
        return match.IsFinished
            && match.HomeTeam is not null && match.VisitorTeam is not null
            && match.HomeScore is not null && match.VisitorScore is not null;
    }

    /// <summary>
    /// Orders the positions applying the HU-80 chain stepwise: teams are
    /// first sorted by PTS, then PG, then whole-zone DG. Any remaining group
    /// of teams still tied on all three is broken by the head-to-head
    /// mini-table built only from the games among that tied group, and — only
    /// when those teams played each other more than once — by the points
    /// difference within those same games.
    /// </summary>
    private static List<Position> OrderWithTiebreakers(
        IEnumerable<Position> positions,
        IReadOnlyList<Match> matches,
        int pointsForWin,
        int pointsForLoss)
    {
        List<Position> ranked = [.. positions
            .OrderByDescending(p => p.Points)
            .ThenByDescending(p => p.Wins)
            .ThenByDescending(p => p.PointsDifference)];

        List<Position> result = [];

        int index = 0;
        while (index < ranked.Count)
        {
            int groupEnd = index + 1;
            while (groupEnd < ranked.Count && SharesBaseCriteria(ranked[index], ranked[groupEnd]))
            {
                groupEnd++;
            }

            List<Position> tiedGroup = ranked.GetRange(index, groupEnd - index);

            if (tiedGroup.Count == 1)
            {
                result.Add(tiedGroup[0]);
            }
            else
            {
                result.AddRange(BreakTie(tiedGroup, matches, pointsForWin, pointsForLoss));
            }

            index = groupEnd;
        }

        AssignResolvedBy(result);

        return result;
    }

    private static bool SharesBaseCriteria(Position a, Position b)
    {
        return a.Points == b.Points && a.Wins == b.Wins && a.PointsDifference == b.PointsDifference;
    }

    /// <summary>
    /// Breaks a set of teams tied on PTS/PG/DG using the head-to-head
    /// mini-table (points earned only in games among the tied set) and then,
    /// when the tied set played each other more than once, the points
    /// difference within those games.
    /// </summary>
    private static List<Position> BreakTie(
        List<Position> tiedGroup,
        IReadOnlyList<Match> matches,
        int pointsForWin,
        int pointsForLoss)
    {
        HashSet<Guid> tiedIds = [.. tiedGroup.Select(p => p.TeamId)];

        List<Match> h2hMatches = [.. matches.Where(m =>
            tiedIds.Contains(m.HomeTeamId!.Value) && tiedIds.Contains(m.VisitorTeamId!.Value))];

        Dictionary<Guid, int> h2hPoints = tiedGroup.ToDictionary(p => p.TeamId, _ => 0);
        Dictionary<Guid, int> h2hDifference = tiedGroup.ToDictionary(p => p.TeamId, _ => 0);

        foreach (Match match in h2hMatches)
        {
            Guid winnerId = match.WinningTeamId!.Value;
            Guid homeId = match.HomeTeamId!.Value;
            Guid visitorId = match.VisitorTeamId!.Value;

            h2hPoints[winnerId] += pointsForWin;
            h2hPoints[winnerId == homeId ? visitorId : homeId] += pointsForLoss;

            int margin = match.HomeScore!.Value - match.VisitorScore!.Value;
            h2hDifference[homeId] += margin;
            h2hDifference[visitorId] -= margin;
        }

        // "DG en H2H" only applies when the tied teams played each other more
        // than once (HU-80, criterion 5).
        bool playedMoreThanOnce = h2hMatches.Count > 1;

        return [.. tiedGroup
            .OrderByDescending(p => h2hPoints[p.TeamId])
            .ThenByDescending(p => playedMoreThanOnce ? h2hDifference[p.TeamId] : 0)];
    }

    /// <summary>
    /// Records, for each team, the criterion that separated it from the team
    /// ranked immediately above it, so the standings UI can show why each tie
    /// was broken (HU-80). The top team and any team not sharing table points
    /// with the one above it carry no reason.
    /// </summary>
    private static void AssignResolvedBy(List<Position> ordered)
    {
        for (int i = 1; i < ordered.Count; i++)
        {
            Position above = ordered[i - 1];
            Position current = ordered[i];

            if (above.Points != current.Points)
            {
                current.ResolvedBy = null;
            }
            else if (above.Wins != current.Wins)
            {
                current.ResolvedBy = TiebreakerCriterion.GamesWon;
            }
            else if (above.PointsDifference != current.PointsDifference)
            {
                current.ResolvedBy = TiebreakerCriterion.PointsDifference;
            }
            else
            {
                // Tied on all whole-zone criteria: the head-to-head chain put
                // them in this order. We cannot cheaply tell PG-in-H2H from
                // DG-in-H2H apart here, so we report HeadToHead as the family.
                current.ResolvedBy = TiebreakerCriterion.HeadToHead;
            }
        }
    }

    /// <summary>
    /// Returns the team's <see cref="Position"/>, creating a zeroed one on first
    /// touch. Used both to seed the assigned roster (0-0) and lazily when a
    /// finished match introduces a team.
    /// </summary>
    private static Position EnsurePosition(
        Dictionary<Guid, Position> positionsByTeamId,
        Team team)
    {
        if (!positionsByTeamId.TryGetValue(team.Id, out Position? position))
        {
            position = new Position
            {
                TeamId = team.Id,
                TeamName = team.Name,
                LogoUrl = team.LogoUrl,
                MatchesPlayed = 0,
                Wins = 0,
                Losses = 0,
                PointsFor = 0,
                PointsAgainst = 0,
                PointsDifference = 0,
                Points = 0,
            };
            positionsByTeamId[team.Id] = position;
        }

        return position;
    }

    private static void ApplyResult(
        Dictionary<Guid, Position> positionsByTeamId,
        Team team,
        int pointsFor,
        int pointsAgainst,
        bool won,
        int pointsForWin,
        int pointsForLoss)
    {
        Position position = EnsurePosition(positionsByTeamId, team);

        position.MatchesPlayed += 1;
        position.PointsFor += pointsFor;
        position.PointsAgainst += pointsAgainst;

        if (won)
        {
            position.Wins += 1;
            position.Points += pointsForWin;
        }
        else
        {
            position.Losses += 1;
            position.Points += pointsForLoss;
        }
    }
}
