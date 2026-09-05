using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.RoundRobin;

/// <summary>
/// Generates a round-robin fixture using the standard circle method.
/// </summary>
public static class RoundRobinScheduler
{
    /// <summary>
    /// A placeholder used internally to give an odd team count an even slot count during pairing; never appears in the returned fixture.
    /// </summary>
    private static readonly Guid Bye = Guid.Empty;

    /// <summary>
    /// Builds the fixture for a group of teams, tagging every pairing with its 1-based round, jornada, number.
    /// </summary>
    /// <param name="teamIds">The teams to schedule. An odd count is handled with a bye, one team sits out each round.</param>
    /// <param name="legs">How many times each pair plays: 1 for single round-robin, 2 for double, and so on.</param>
    /// <param name="random">Optional source of randomness, for deterministic tests.</param>
    /// <returns>
    /// One entry per match, each carrying the home/visitor teams and the round
    /// it is played in. Empty when fewer than two teams are supplied.
    /// </returns>
    public static List<(Guid HomeTeamId, Guid VisitorTeamId, int Round)> GenerateRounds(
        IReadOnlyList<Guid> teamIds, int legs, Random? random = null)
    {
        if (teamIds.Count < 2)
        {
            return [];
        }

        random ??= new Random();

        // Fresh random draw: shuffle the seating before applying the circle
        // method, so each generation yields a different-but-valid fixture
        // while preserving the round structure.
        List<Guid> rotation = [.. Shuffle([.. teamIds], random)];
        if (rotation.Count % 2 != 0)
        {
            rotation.Add(Bye);
        }

        int slotCount = rotation.Count;
        int roundsPerLeg = slotCount - 1;

        List<(Guid HomeTeamId, Guid VisitorTeamId, int Round)> fixture = [];

        for (int leg = 0; leg < legs; leg++)
        {
            // Alternate home/away on odd legs so a double round-robin gives each
            // pair one home and one away game.
            bool swapHomeAway = leg % 2 == 1;
            List<Guid> current = [.. rotation];

            for (int round = 0; round < roundsPerLeg; round++)
            {
                int roundNumber = (leg * roundsPerLeg) + round + 1;

                for (int i = 0; i < slotCount / 2; i++)
                {
                    Guid a = current[i];
                    Guid b = current[slotCount - 1 - i];
                    if (a != Bye && b != Bye)
                    {
                        fixture.Add(swapHomeAway
                            ? (b, a, roundNumber)
                            : (a, b, roundNumber));
                    }
                }

                RotateInPlace(current);
            }
        }

        return fixture;
    }

    /// <summary>
    /// Builds the fixture as a flat list of pairings, discarding the round number.
    /// </summary>
    public static List<(Guid HomeTeamId, Guid VisitorTeamId)> GenerateFixture(
        IReadOnlyList<Guid> teamIds, int legs, Random? random = null)
    {
        return [.. GenerateRounds(teamIds, legs, random)
            .Select(pairing => (pairing.HomeTeamId, pairing.VisitorTeamId))];
    }

    /// <summary>
    /// One round of the circle method: the first slot stays fixed, every other slot rotates one position clockwise.
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
