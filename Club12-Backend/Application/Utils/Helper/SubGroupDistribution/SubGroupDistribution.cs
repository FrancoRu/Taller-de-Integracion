using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.SubGroupDistribution;

/// <summary>
/// Deals a division's roster into evenly balanced sub-groups, the shared algorithm behind rebuild and auto-distribute.
/// </summary>
public static class SubGroupDistribution
{
    /// <summary>
    /// Minimum number of teams a single sub-group must receive for the split to stay playable.
    /// </summary>
    public const int MinTeamsPerSubGroup = 4;

    /// <summary>
    /// Whether subGroupCount groups over totalTeams teams keeps every group at or above the minimum size, treating an empty roster as always valid.
    /// </summary>
    public static bool MeetsMinimumSize(int totalTeams, int subGroupCount)
    {
        if (totalTeams == 0)
        {
            return true;
        }

        return totalTeams / subGroupCount >= MinTeamsPerSubGroup;
    }

    /// <summary>
    /// Randomly deals rosterTeamIds into subGroupCount groups by round robin, so no two groups differ in size by more than one team.
    /// </summary>
    public static List<List<Guid>> Distribute(IReadOnlyList<Guid> rosterTeamIds, int subGroupCount)
    {
        List<Guid> shuffled = [.. rosterTeamIds];
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Shared.Next(i + 1);
            (shuffled[swapIndex], shuffled[i]) = (shuffled[i], shuffled[swapIndex]);
        }

        List<List<Guid>> groups = [.. Enumerable.Range(0, subGroupCount).Select(_ => new List<Guid>())];

        for (int i = 0; i < shuffled.Count; i++)
        {
            groups[i % subGroupCount].Add(shuffled[i]);
        }

        return groups;
    }
}
