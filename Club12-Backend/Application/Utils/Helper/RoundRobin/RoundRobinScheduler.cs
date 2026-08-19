using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.RoundRobin;

/// <summary>
/// Generates a round-robin fixture (every team plays every other team
/// once per leg) using the standard circle method, then repeats it for
/// additional legs (alternating home/away each repeat) and shuffles the
/// final match order — a fresh random draw every time it runs.
/// </summary>
public static class RoundRobinScheduler
{
    /// <summary>
    /// A placeholder used internally to give an odd team count an even
    /// slot count during pairing; never appears in the returned fixture.
    /// </summary>
    private static readonly Guid Bye = Guid.Empty;

    /// <summary>
    /// Builds the fixture for a group of teams.
    /// </summary>
    /// <param name="teamIds">The teams to schedule. An odd count is handled with a bye (the team sits out that round).</param>
    /// <param name="legs">How many times each pair plays (1 = single round-robin, 2 = double, ...).</param>
    /// <param name="random">Optional source of randomness, for deterministic tests.</param>
    public static List<(Guid HomeTeamId, Guid VisitorTeamId)> GenerateFixture(
        IReadOnlyList<Guid> teamIds, int legs, Random? random = null)
    {
        if (teamIds.Count < 2)
        {
            return [];
        }

        random ??= new Random();

        List<Guid> rotation = [.. teamIds];
        if (rotation.Count % 2 != 0)
        {
            rotation.Add(Bye);
        }

        List<(Guid, Guid)> singleLegPairs = BuildSingleLegPairs(rotation);

        List<(Guid HomeTeamId, Guid VisitorTeamId)> fixture = [];
        for (int leg = 0; leg < legs; leg++)
        {
            bool swapHomeAway = leg % 2 == 1;
            fixture.AddRange(singleLegPairs.Select(pair =>
                swapHomeAway ? (pair.Item2, pair.Item1) : pair));
        }

        return [.. Shuffle(fixture, random)];
    }

    private static List<(Guid, Guid)> BuildSingleLegPairs(List<Guid> rotation)
    {
        int slotCount = rotation.Count;
        int roundsPerLeg = slotCount - 1;
        List<Guid> current = [.. rotation];
        List<(Guid, Guid)> pairs = [];

        for (int round = 0; round < roundsPerLeg; round++)
        {
            for (int i = 0; i < slotCount / 2; i++)
            {
                Guid a = current[i];
                Guid b = current[slotCount - 1 - i];
                if (a != Bye && b != Bye)
                {
                    pairs.Add((a, b));
                }
            }

            RotateInPlace(current);
        }

        return pairs;
    }

    /// <summary>
    /// One round of the circle method: the first slot stays fixed, every
    /// other slot rotates one position clockwise.
    /// </summary>
    private static void RotateInPlace(List<Guid> slots)
    {
        Guid last = slots[^1];
        for (int i = slots.Count - 1; i > 1; i--)
        {
            slots[i] = slots[i - 1];
        }
        slots[1] = last;
    }

    private static List<T> Shuffle<T>(List<T> items, Random random)
    {
        return [.. items.OrderBy(_ => random.Next())];
    }
}
