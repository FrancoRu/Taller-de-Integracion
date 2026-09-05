using Application.Utils.Constants;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Playoff;

/// <summary>
/// Pairs a ranked list of teams into first-round playoff matchups using the standard bracket seeding order.
/// </summary>
public static class PlayoffSeeder
{
    /// <summary>
    /// Builds the first-round pairs for a ranked list of teams, best seed first.
    /// </summary>
    public static List<(Guid HomeTeamId, Guid? VisitorTeamId)> SeedPairs(IReadOnlyList<Guid> orderedTeamIds)
    {
        int count = orderedTeamIds.Count;

        if (count < 2)
        {
            throw new InvalidOperationException(ErrorMessages.Playoff.NotEnoughRankedTeams);
        }

        int bracketSize = NextPowerOfTwo(count);
        List<Guid?> paddedSeeds = [.. orderedTeamIds.Select(id => (Guid?) id)];
        paddedSeeds.AddRange(Enumerable.Repeat((Guid?) null, bracketSize - count));

        List<int> bracketOrder = BuildSeedOrder(bracketSize);

        List<(Guid, Guid?)> pairs = [];
        for (int i = 0; i < bracketSize; i += 2)
        {
            Guid? first = paddedSeeds[bracketOrder[i] - 1];
            Guid? second = paddedSeeds[bracketOrder[i + 1] - 1];

            (Guid? home, Guid? visitor) = first is null ? (second, first) : (first, second);

            pairs.Add((home!.Value, visitor));
        }

        return pairs;
    }

    /// <summary>
    /// Smallest power of two greater than or equal to value, the bracket size a seed pool of that many teams pads up to.
    /// </summary>
    public static int NextPowerOfTwo(int value)
    {
        int power = 1;
        while (power < value)
        {
            power *= 2;
        }
        return power;
    }

    /// <summary>
    /// Recursively builds the classic bracket seed-position order.
    /// </summary>
    private static List<int> BuildSeedOrder(int size)
    {
        if (size == 1)
        {
            return [1];
        }

        List<int> previous = BuildSeedOrder(size / 2);
        List<int> current = [];

        foreach (int seed in previous)
        {
            current.Add(seed);
            current.Add(size + 1 - seed);
        }

        return current;
    }
}
