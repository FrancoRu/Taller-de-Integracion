using Application.Utils.Constants;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Playoff;

/// <summary>
/// Validates a division's position-range → playoff-destination mappings
/// (HU-45): every range must be well-formed (1-based, from ≤ to, non-empty
/// destination) and no two ranges within the same division may overlap, so
/// each standings position resolves to at most one cup.
/// </summary>
public static class PlayoffMappingValidator
{
    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if any range is
    /// malformed or if two ranges overlap. An empty set is valid (the
    /// division simply has no playoff mapping yet).
    /// </summary>
    public static void Validate(IEnumerable<DivisionPlayoffMapping> mappings)
    {
        List<DivisionPlayoffMapping> ordered = [.. mappings.OrderBy(m => m.FromPosition)];

        foreach (DivisionPlayoffMapping mapping in ordered)
        {
            if (mapping.FromPosition < 1 || mapping.ToPosition < mapping.FromPosition)
            {
                throw new InvalidOperationException(
                    ErrorMessages.Playoff.InvalidRange(mapping.FromPosition, mapping.ToPosition));
            }

            if (string.IsNullOrWhiteSpace(mapping.Destination))
            {
                throw new InvalidOperationException(ErrorMessages.Playoff.EmptyDestination);
            }
        }

        for (int i = 1; i < ordered.Count; i++)
        {
            // Ordered by FromPosition, so an overlap exists iff the current
            // range starts at or before the previous range's end.
            if (ordered[i].FromPosition <= ordered[i - 1].ToPosition)
            {
                throw new InvalidOperationException(
                    ErrorMessages.Playoff.OverlappingRanges(
                        ordered[i - 1].FromPosition, ordered[i - 1].ToPosition,
                        ordered[i].FromPosition, ordered[i].ToPosition));
            }
        }
    }
}
