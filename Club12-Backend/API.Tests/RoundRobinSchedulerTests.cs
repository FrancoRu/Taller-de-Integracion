using Application.Utils.Helper.RoundRobin;

namespace API.Tests;

public class RoundRobinSchedulerTests
{
    [Fact]
    public void GenerateFixture_FourTeams_SingleLeg_EveryPairPlaysExactlyOnce()
    {
        List<Guid> teams = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        List<(Guid HomeTeamId, Guid VisitorTeamId)> fixture = RoundRobinScheduler.GenerateFixture(teams, legs: 1, new Random(1));

        Assert.Equal(6, fixture.Count);

        HashSet<(Guid, Guid)> unorderedPairs = [.. fixture.Select(m =>
            m.HomeTeamId.CompareTo(m.VisitorTeamId) < 0 ? (m.HomeTeamId, m.VisitorTeamId) : (m.VisitorTeamId, m.HomeTeamId))];
        Assert.Equal(6, unorderedPairs.Count);

        foreach (Guid team in teams)
        {
            int appearances = fixture.Count(m => m.HomeTeamId == team || m.VisitorTeamId == team);
            Assert.Equal(3, appearances);
        }
    }

    [Fact]
    public void GenerateFixture_OddTeamCount_HandlesByeWithoutIncludingIt()
    {
        List<Guid> teams = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        List<(Guid HomeTeamId, Guid VisitorTeamId)> fixture = RoundRobinScheduler.GenerateFixture(teams, legs: 1, new Random(1));

        Assert.Equal(3, fixture.Count);
        Assert.All(fixture, m =>
        {
            Assert.Contains(m.HomeTeamId, teams);
            Assert.Contains(m.VisitorTeamId, teams);
        });
    }

    [Fact]
    public void GenerateFixture_TwoLegs_DoublesPairCount()
    {
        List<Guid> teams = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        List<(Guid HomeTeamId, Guid VisitorTeamId)> singleLeg = RoundRobinScheduler.GenerateFixture(teams, legs: 1, new Random(1));
        List<(Guid HomeTeamId, Guid VisitorTeamId)> doubleLeg = RoundRobinScheduler.GenerateFixture(teams, legs: 2, new Random(1));

        Assert.Equal(singleLeg.Count * 2, doubleLeg.Count);
    }

    [Fact]
    public void GenerateFixture_FewerThanTwoTeams_ReturnsEmpty()
    {
        List<(Guid HomeTeamId, Guid VisitorTeamId)> fixture = RoundRobinScheduler.GenerateFixture([Guid.NewGuid()], legs: 1);

        Assert.Empty(fixture);
    }

    // ---- Round (jornada) assignment: HU-63 / HU-65 ----

    /// <summary>
    /// Even team count: a single round-robin has (teams - 1) rounds, and within
    /// every round each team appears exactly once (no team plays twice, none is
    /// idle since the count is even).
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    public void GenerateRounds_EvenTeams_SingleLeg_EachTeamPlaysOncePerRoundAndRoundCountIsTeamsMinusOne(int teamCount)
    {
        List<Guid> teams = [.. Enumerable.Range(0, teamCount).Select(_ => Guid.NewGuid())];

        List<(Guid HomeTeamId, Guid VisitorTeamId, int Round)> fixture =
            RoundRobinScheduler.GenerateRounds(teams, legs: 1, new Random(1));

        int expectedRounds = teamCount - 1;
        List<int> rounds = [.. fixture.Select(m => m.Round).Distinct().OrderBy(r => r)];
        Assert.Equal(expectedRounds, rounds.Count);
        Assert.Equal(Enumerable.Range(1, expectedRounds), rounds);

        // teams/2 games per round, and every team plays exactly once in it.
        foreach (int round in rounds)
        {
            List<(Guid HomeTeamId, Guid VisitorTeamId, int Round)> games = [.. fixture.Where(m => m.Round == round)];
            Assert.Equal(teamCount / 2, games.Count);

            List<Guid> participants = [.. games.SelectMany(g => new[] { g.HomeTeamId, g.VisitorTeamId })];
            Assert.Equal(teamCount, participants.Count);
            Assert.Equal(teamCount, participants.Distinct().Count());
            Assert.All(teams, t => Assert.Contains(t, participants));
        }
    }

    /// <summary>
    /// Odd team count: the schedule has exactly `teams` rounds, and in every
    /// round exactly one team is idle ("libre") — i.e. (teams - 1) teams play,
    /// one does not, and no team plays twice.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    public void GenerateRounds_OddTeams_SingleLeg_ExactlyOneByePerRound(int teamCount)
    {
        List<Guid> teams = [.. Enumerable.Range(0, teamCount).Select(_ => Guid.NewGuid())];

        List<(Guid HomeTeamId, Guid VisitorTeamId, int Round)> fixture =
            RoundRobinScheduler.GenerateRounds(teams, legs: 1, new Random(1));

        List<int> rounds = [.. fixture.Select(m => m.Round).Distinct().OrderBy(r => r)];
        Assert.Equal(teamCount, rounds.Count);
        Assert.Equal(Enumerable.Range(1, teamCount), rounds);

        foreach (int round in rounds)
        {
            List<(Guid HomeTeamId, Guid VisitorTeamId, int Round)> games = [.. fixture.Where(m => m.Round == round)];
            Assert.Equal((teamCount - 1) / 2, games.Count);

            List<Guid> participants = [.. games.SelectMany(g => new[] { g.HomeTeamId, g.VisitorTeamId })];
            Assert.Equal(participants.Count, participants.Distinct().Count());

            // Exactly one team sits out this round.
            int idle = teams.Count(t => !participants.Contains(t));
            Assert.Equal(1, idle);
        }

        // Bye (Guid.Empty) never leaks into the returned fixture.
        Assert.DoesNotContain(fixture, m => m.HomeTeamId == Guid.Empty || m.VisitorTeamId == Guid.Empty);

        // Over the whole single round-robin every pair still meets once.
        foreach (Guid team in teams)
        {
            int appearances = fixture.Count(m => m.HomeTeamId == team || m.VisitorTeamId == team);
            Assert.Equal(teamCount - 1, appearances);
        }
    }

    /// <summary>
    /// Multi-leg numbering: rounds are numbered consecutively across legs, so a
    /// double round-robin of N teams runs from round 1 to 2*(N-1) with no gaps,
    /// and every pair meets exactly twice.
    /// </summary>
    [Fact]
    public void GenerateRounds_FourTeams_TwoLegs_NumbersRoundsConsecutivelyAcrossLegs()
    {
        List<Guid> teams = [.. Enumerable.Range(0, 4).Select(_ => Guid.NewGuid())];

        List<(Guid HomeTeamId, Guid VisitorTeamId, int Round)> fixture =
            RoundRobinScheduler.GenerateRounds(teams, legs: 2, new Random(1));

        List<int> rounds = [.. fixture.Select(m => m.Round).Distinct().OrderBy(r => r)];
        Assert.Equal(Enumerable.Range(1, 6), rounds); // 2 * (4 - 1) = 6 rounds

        // 12 matches total, each team plays once per round.
        Assert.Equal(12, fixture.Count);
        foreach (int round in rounds)
        {
            List<Guid> participants = [.. fixture.Where(m => m.Round == round)
                .SelectMany(g => new[] { g.HomeTeamId, g.VisitorTeamId })];
            Assert.Equal(4, participants.Distinct().Count());
        }

        // Every unordered pair meets exactly twice across the two legs.
        Dictionary<(Guid, Guid), int> pairCounts = [];
        foreach ((Guid home, Guid visitor, _) in fixture)
        {
            (Guid, Guid) key = home.CompareTo(visitor) < 0 ? (home, visitor) : (visitor, home);
            pairCounts[key] = pairCounts.GetValueOrDefault(key) + 1;
        }
        Assert.Equal(6, pairCounts.Count);
        Assert.All(pairCounts.Values, count => Assert.Equal(2, count));
    }
}
