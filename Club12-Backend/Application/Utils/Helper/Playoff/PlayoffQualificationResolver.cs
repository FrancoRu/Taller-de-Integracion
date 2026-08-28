using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Playoff;

/// <summary>
/// Resolves which teams qualify for each playoff cup from the final
/// group-stage standings and the per-division position-range mappings
/// (HU-45/HU-81). A cup can draw teams from several divisions; within a cup
/// they are seeded by their finishing position in their own division (a
/// division winner outranks a division runner-up), so a single division maps
/// straight to its standings order.
/// </summary>
public static class PlayoffQualificationResolver
{
    /// <summary>
    /// One division's contribution to the playoffs: its final standings
    /// (best team first) and its position-range → destination mappings.
    /// <see cref="DivisionOrder"/> breaks ties between equally-placed teams
    /// from different divisions when they land in the same cup (lower first).
    /// </summary>
    public sealed class DivisionStandings
    {
        public required IReadOnlyList<Position> Standings { get; init; }
        public required IReadOnlyList<DivisionPlayoffMapping> Mappings { get; init; }
        public int DivisionOrder { get; init; }
    }

    /// <summary>
    /// Returns, per destination cup, the ordered list of qualifying team ids
    /// (best seed first) gathered across every contributing division. A team
    /// qualifies when its 1-based finishing position falls inside one of its
    /// division's ranges; the range's destination is the cup it joins.
    /// </summary>
    public static Dictionary<string, List<Guid>> Resolve(IEnumerable<DivisionStandings> divisions)
    {
        List<(string Destination, int Position, int DivisionOrder, Guid TeamId)> qualifiers = [];

        foreach (DivisionStandings division in divisions)
        {
            for (int i = 0; i < division.Standings.Count; i++)
            {
                int position = i + 1;

                DivisionPlayoffMapping? mapping = division.Mappings
                    .FirstOrDefault(m => position >= m.FromPosition && position <= m.ToPosition);

                if (mapping is null)
                {
                    continue;
                }

                qualifiers.Add((mapping.Destination, position, division.DivisionOrder, division.Standings[i].TeamId));
            }
        }

        return qualifiers
            .GroupBy(q => q.Destination)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(q => q.Position)
                    .ThenBy(q => q.DivisionOrder)
                    .Select(q => q.TeamId)
                    .ToList());
    }
}
