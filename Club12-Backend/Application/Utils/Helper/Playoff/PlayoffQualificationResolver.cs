using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Playoff;

/// <summary>
/// Resolves which teams qualify for each playoff cup from the final group-stage standings and the per-division position-range mappings.
/// </summary>
public static class PlayoffQualificationResolver
{
    /// <summary>
    /// One division's contribution to the playoffs: its final standings, best team first, and its position-range to destination mappings.
    /// </summary>
    public sealed class DivisionStandings
    {
        public required IReadOnlyList<Position> Standings { get; init; }
        public required IReadOnlyList<DivisionPlayoffMapping> Mappings { get; init; }
        public int DivisionOrder { get; init; }
    }

    /// <summary>
    /// Returns, per destination cup, the ordered list of qualifying team ids gathered across every contributing division, best seed first.
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
