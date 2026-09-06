using Application.Utils.Helper.SubGroupDistribution;

namespace API.Tests;

/// <summary>
/// Pure-logic tests for the balanced round-robin dealer behind sub-group
/// distribution (HU-121/122): every group stays within one team of every
/// other group, the minimum-size check matches the floor(T/G) &gt;= 4 rule,
/// and the dealt result is always a valid permutation of the input roster.
/// </summary>
public class SubGroupDistributionTests
{
    private static List<Guid> MakeRoster(int count)
    {
        return [.. Enumerable.Range(0, count).Select(_ => Guid.NewGuid())];
    }

    [Fact]
    public void Distribute_16TeamsInto3Groups_Balances5_5_6()
    {
        List<Guid> roster = MakeRoster(16);

        List<List<Guid>> groups = SubGroupDistribution.Distribute(roster, 3);

        List<int> sizes = [.. groups.Select(g => g.Count).OrderBy(size => size)];
        Assert.Equal([5, 5, 6], sizes);
        Assert.True(sizes[^1] - sizes[0] < 2);
    }

    [Fact]
    public void Distribute_16TeamsInto4Groups_AllExactlyFour()
    {
        List<Guid> roster = MakeRoster(16);

        List<List<Guid>> groups = SubGroupDistribution.Distribute(roster, 4);

        Assert.All(groups, g => Assert.Equal(4, g.Count));
    }

    [Fact]
    public void Distribute_ResultIsPermutationOfRoster_NoTeamDroppedOrDuplicated()
    {
        List<Guid> roster = MakeRoster(13);

        List<List<Guid>> groups = SubGroupDistribution.Distribute(roster, 3);

        List<Guid> dealt = [.. groups.SelectMany(g => g)];
        Assert.Equal(roster.Count, dealt.Count);
        Assert.Equal([.. roster.OrderBy(id => id)], [.. dealt.OrderBy(id => id)]);
    }

    [Fact]
    public void MeetsMinimumSize_TenTeamsThreeGroups_ReturnsFalse()
    {
        Assert.False(SubGroupDistribution.MeetsMinimumSize(10, 3));
    }

    [Fact]
    public void MeetsMinimumSize_TwelveTeamsThreeGroups_ReturnsTrue()
    {
        Assert.True(SubGroupDistribution.MeetsMinimumSize(12, 3));
    }

    [Fact]
    public void MeetsMinimumSize_EmptyRoster_SkipsCheckAndReturnsTrue()
    {
        Assert.True(SubGroupDistribution.MeetsMinimumSize(0, 5));
    }
}
