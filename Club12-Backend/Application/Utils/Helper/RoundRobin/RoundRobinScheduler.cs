using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.RoundRobin;

/// <summary>
/// Generates a round-robin fixture (every team plays every other team once
/// per leg) using the standard circle method. The schedule is organised into
/// rounds (jornadas, HU-63/HU-65): within a single round every team plays at
/// most once, and with an odd number of teams exactly one team is idle
/// ("libre") each round. A fresh random draw is produced every time by
/// shuffling the seat assignment before applying the circle method, while the
/// round structure itself stays intact so it can be used as the canonical
/// matchday grouping.
/// </summary>
public static class RoundRobinScheduler
{
    /// <summary>
    /// A placeholder used internally to give an odd team count an even
    /// slot count during pairing; never appears in the returned fixture.
    /// </summary>
    private static readonly Guid Bye = Guid.Empty;

    /// <summary>
    /// Builds the fixture for a group of teams, tagging every pairing with its
    /// 1-based round (jornada) number. Rounds are numbered consecutively across
    /// legs: with R rounds per leg, leg 0 owns rounds 1..R, leg 1 owns rounds
    /// R+1..2R, and so on.
    /// </summary>
    /// <param name="teamIds">The teams to schedule. An odd count is handled with a bye (one team sits out each round).</param>
    /// <param name="legs">How many times each pair plays (1 = single round-robin, 2 = double, ...).</param>
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
    /// Builds the fixture as a flat list of pairings, discarding the round
    /// number. Retained for callers that only need the set of matchups; new
    /// code that needs the matchday grouping should use
    /// <see cref="GenerateRounds"/>.
    /// </summary>
    public static List<(Guid HomeTeamId, Guid VisitorTeamId)> GenerateFixture(
        IReadOnlyList<Guid> teamIds, int legs, Random? random = null)
    {
        return [.. GenerateRounds(teamIds, legs, random)
            .Select(pairing => (pairing.HomeTeamId, pairing.VisitorTeamId))];
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
