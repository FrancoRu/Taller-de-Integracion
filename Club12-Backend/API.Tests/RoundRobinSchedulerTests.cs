using System;
using System.Collections.Generic;
using System.Linq;
using Application.Utils.Helper.RoundRobin;
using Xunit;

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
}
