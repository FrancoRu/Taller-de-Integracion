using Application.Utils.Constants;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Playoff;

/// <summary>
/// Resolves the single bracket seed order for a multi-group cross-division cup
/// (HU-110). A cross cup runs N internal group stages of variable size; the
/// bracket is seeded by pooling the top-K teams of every group and ordering
/// that pool by group-stage strength.
///
/// The ordering reuses the same base criteria the group standings use
/// (<see cref="Standings.PositionCalculator"/>): table Points, then Wins, then
/// whole-group PointsDifference. Two further keys make the cross-group order
/// total and deterministic (the standings' head-to-head tiebreaker is
/// meaningless across groups that never played each other): PointsFor, then
/// TeamId. So a group winner with a stronger record seeds ahead of a weaker
/// group winner, and the top seeds receive the byes when the pool is not a
/// power of two.
/// </summary>
public static class CrossCupGroupSeeder
{
    /// <summary>
    /// Pools the top <paramref name="qualifiersPerGroup"/> teams of each
    /// group's standings and returns them as one ordered seed list (best seed
    /// first). Each group's standings must already be ordered best-first (as
    /// produced by <see cref="Standings.PositionCalculator.CalculatePositions"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="qualifiersPerGroup"/> is less than 1, or
    /// when fewer than two teams are pooled across all groups (a bracket needs
    /// at least two seeds).
    /// </exception>
    public static List<Guid> ResolveSeedOrder(
        IEnumerable<IReadOnlyList<Position>> groupStandings,
        int qualifiersPerGroup)
    {
        if (qualifiersPerGroup < 1)
        {
            throw new InvalidOperationException(ErrorMessages.Playoff.InvalidQualifiersPerGroup(qualifiersPerGroup));
        }

        List<Position> pooled = [.. groupStandings.SelectMany(standings => standings.Take(qualifiersPerGroup))];

        if (pooled.Count < 2)
        {
            throw new InvalidOperationException(ErrorMessages.Playoff.NotEnoughRankedTeams);
        }

        return [.. pooled
            .OrderByDescending(position => position.Points)
            .ThenByDescending(position => position.Wins)
            .ThenByDescending(position => position.PointsDifference)
            .ThenByDescending(position => position.PointsFor)
            .ThenBy(position => position.TeamId)
            .Select(position => position.TeamId)];
    }
}
