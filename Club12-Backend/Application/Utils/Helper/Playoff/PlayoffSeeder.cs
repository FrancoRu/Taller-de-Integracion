using Application.Utils.Constants;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Playoff;

/// <summary>
/// Pairs a ranked list of teams (e.g. group-stage standings) into
/// first-round playoff matchups using the standard bracket seeding order
/// (1v8, 4v5, 2v7, 3v6 for 8 teams) — the placement that keeps the top two
/// seeds apart until the final, not naive adjacent pairing.
/// </summary>
public static class PlayoffSeeder
{
    /// <summary>
    /// Builds the first-round pairs for a ranked list of teams (best seed
    /// first). When the count is not a power of two, the bracket is padded
    /// to the next power of two with byes; byes always go to the best
    /// seeds (a null <c>VisitorTeamId</c> means that pair's
    /// <c>HomeTeamId</c> has a bye and advances automatically).
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

    private static int NextPowerOfTwo(int value)
    {
        int power = 1;
        while (power < value)
        {
            power *= 2;
        }
        return power;
    }

    /// <summary>
    /// Recursively builds the classic bracket seed-position order (e.g.
    /// [1,8,4,5,2,7,3,6] for 8), so pairing consecutive slots and letting
    /// winners advance in slot order naturally produces seed 1 vs seed 2
    /// in the final.
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
