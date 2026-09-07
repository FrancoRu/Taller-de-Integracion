using Application.Utils.Constants;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Playoff;

/// <summary>
/// Resolves the single bracket seed order for a multi-group cross-division cup.
/// </summary>
public static class CrossCupGroupSeeder
{
    /// <summary>
    /// Pools the top qualifiersPerGroup teams of each group's standings and returns them as one ordered seed list, best seed first, keeping each team's full Position (not just its id) so a caller can still apply position-range cup mappings over the pooled order.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when qualifiersPerGroup is less than 1, or when fewer than two teams are pooled across all groups, since a bracket needs at least two seeds.
    /// </exception>
    public static List<Position> PoolAndOrder(
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
            .ThenBy(position => position.TeamId)];
    }

    /// <summary>
    /// Pools the top qualifiersPerGroup teams of each group's standings and returns them as one ordered seed list of team ids, best seed first.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when qualifiersPerGroup is less than 1, or when fewer than two teams are pooled across all groups, since a bracket needs at least two seeds.
    /// </exception>
    public static List<Guid> ResolveSeedOrder(
        IEnumerable<IReadOnlyList<Position>> groupStandings,
        int qualifiersPerGroup) =>
        [.. PoolAndOrder(groupStandings, qualifiersPerGroup).Select(position => position.TeamId)];
}
