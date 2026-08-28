using Application.Utils.Helper.Playoff;

using Domain.Entities.Models;

namespace API.Tests;

/// <summary>
/// Covers CrossCupGroupSeeder (HU-110): pooling the top-K qualifiers of each
/// internal group of a multi-group cross-division cup and ordering the pool
/// into a single seed list by group-stage strength
/// (Points → Wins → PointsDifference → PointsFor → TeamId), so a group winner
/// with a stronger record seeds ahead of a weaker one.
/// </summary>
public class CrossCupGroupSeederTests
{
    [Fact]
    public void ResolveSeedOrder_DefaultOnePerGroup_PoolsWinnersOrderedByStrength()
    {
        Guid a1 = Guid.NewGuid();
        Guid b1 = Guid.NewGuid();
        Guid c1 = Guid.NewGuid();

        // Group A winner strongest (bigger point difference), then B, then C
        // (fewer points). Each list's runner-up must be dropped by K = 1.
        IReadOnlyList<Position> groupA = [Pos(a1, points: 6, wins: 3, diff: 30, pointsFor: 100), Pos(Guid.NewGuid(), 4, 2, 10, 80)];
        IReadOnlyList<Position> groupB = [Pos(b1, points: 6, wins: 3, diff: 20, pointsFor: 90), Pos(Guid.NewGuid(), 4, 2, 5, 70)];
        IReadOnlyList<Position> groupC = [Pos(c1, points: 4, wins: 2, diff: 40, pointsFor: 120), Pos(Guid.NewGuid(), 2, 1, 0, 60)];

        List<Guid> order = CrossCupGroupSeeder.ResolveSeedOrder([groupA, groupB, groupC], qualifiersPerGroup: 1);

        Assert.Equal([a1, b1, c1], order);
    }

    [Fact]
    public void ResolveSeedOrder_TieOnPointsWinsAndDifference_BreaksByPointsFor()
    {
        Guid strong = Guid.NewGuid();
        Guid weak = Guid.NewGuid();

        IReadOnlyList<Position> groupA = [Pos(weak, points: 6, wins: 3, diff: 20, pointsFor: 80)];
        IReadOnlyList<Position> groupB = [Pos(strong, points: 6, wins: 3, diff: 20, pointsFor: 95)];

        List<Guid> order = CrossCupGroupSeeder.ResolveSeedOrder([groupA, groupB], qualifiersPerGroup: 1);

        Assert.Equal([strong, weak], order);
    }

    [Fact]
    public void ResolveSeedOrder_KGreaterThanOne_PoolsTopKPerGroup()
    {
        Guid a1 = Guid.NewGuid();
        Guid a2 = Guid.NewGuid();
        Guid b1 = Guid.NewGuid();
        Guid b2 = Guid.NewGuid();

        IReadOnlyList<Position> groupA =
        [
            Pos(a1, points: 6, wins: 3, diff: 90, pointsFor: 300),
            Pos(a2, points: 4, wins: 2, diff: 30, pointsFor: 270),
            Pos(Guid.NewGuid(), 2, 1, -30, 240),
        ];
        IReadOnlyList<Position> groupB =
        [
            Pos(b1, points: 6, wins: 3, diff: 30, pointsFor: 270),
            Pos(b2, points: 4, wins: 2, diff: 10, pointsFor: 260),
            Pos(Guid.NewGuid(), 2, 1, -10, 250),
        ];

        List<Guid> order = CrossCupGroupSeeder.ResolveSeedOrder([groupA, groupB], qualifiersPerGroup: 2);

        // Pool: a1(6,90), b1(6,30), a2(4,30), b2(4,10) — ordered by strength.
        Assert.Equal([a1, b1, a2, b2], order);
    }

    [Fact]
    public void ResolveSeedOrder_FewerThanTwoQualifiers_Throws()
    {
        IReadOnlyList<Position> onlyGroup = [Pos(Guid.NewGuid(), points: 6, wins: 3, diff: 30, pointsFor: 100)];

        Assert.Throws<InvalidOperationException>(
            () => CrossCupGroupSeeder.ResolveSeedOrder([onlyGroup], qualifiersPerGroup: 1));
    }

    [Fact]
    public void ResolveSeedOrder_QualifiersPerGroupLessThanOne_Throws()
    {
        IReadOnlyList<Position> groupA = [Pos(Guid.NewGuid(), 6, 3, 30, 100)];
        IReadOnlyList<Position> groupB = [Pos(Guid.NewGuid(), 6, 3, 30, 100)];

        Assert.Throws<InvalidOperationException>(
            () => CrossCupGroupSeeder.ResolveSeedOrder([groupA, groupB], qualifiersPerGroup: 0));
    }

    private static Position Pos(Guid teamId, int points, int wins, int diff, int pointsFor)
    {
        return new Position
        {
            TeamId = teamId,
            TeamName = $"Team-{teamId}",
            LogoUrl = "http://example.com/logo.png",
            MatchesPlayed = wins,
            Wins = wins,
            Losses = 0,
            PointsFor = pointsFor,
            PointsAgainst = pointsFor - diff,
            PointsDifference = diff,
            Points = points,
        };
    }
}
