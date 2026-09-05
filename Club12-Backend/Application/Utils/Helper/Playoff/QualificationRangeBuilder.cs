using Application.DTOs.Divisions.Response;

using Domain.Entities.Models;

using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Playoff;

/// <summary>
/// Turns a division's playoff mappings into the ordered qualification ranges the public standings table paints.
/// </summary>
public static class QualificationRangeBuilder
{
    /// <summary>
    /// Builds the ordered qualification ranges for a set of playoff mappings.
    /// </summary>
    /// <param name="mappings">The division's playoff mappings, in any order.</param>
    /// <returns>The ordered qualification ranges; empty when there are none.</returns>
    public static List<QualificationRangeResponse> Build(IEnumerable<DivisionPlayoffMapping>? mappings)
    {
        if (mappings is null)
        {
            return [];
        }

        return [.. mappings
            .OrderBy(mapping => mapping.FromPosition)
            .ThenBy(mapping => mapping.ToPosition)
            .Select((mapping, index) => new QualificationRangeResponse
            {
                FromPosition = mapping.FromPosition,
                ToPosition = mapping.ToPosition,
                CupName = mapping.Destination,
                Order = index,
            })];
    }
}
