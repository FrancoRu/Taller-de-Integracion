using System;
using System.Collections.Generic;
using System.Linq;
using Application.Utils.Helper.Playoff;
using Xunit;

namespace API.Tests;

/// <summary>
/// Verifies PlayoffSeeder produces the classic bracket seed order (not
/// naive adjacent pairing) — the property that keeps seed 1 and seed 2
/// apart until the final — and that non-power-of-two counts are padded
/// with byes awarded to the best seeds.
/// </summary>
public class PlayoffSeederTests
{
    [Fact]
    public void SeedPairs_EightTeams_ProducesClassicBracketOrder()
    {
        List<Guid> seeds = [.. new[] { 1, 2, 3, 4, 5, 6, 7, 8 }.Select(_ => Guid.NewGuid())];

        List<(Guid HomeTeamId, Guid? VisitorTeamId)> pairs = PlayoffSeeder.SeedPairs(seeds);

        Assert.Equal((seeds[0], (Guid?)seeds[7]), pairs[0]);
        Assert.Equal((seeds[3], (Guid?)seeds[4]), pairs[1]);
        Assert.Equal((seeds[1], (Guid?)seeds[6]), pairs[2]);
        Assert.Equal((seeds[2], (Guid?)seeds[5]), pairs[3]);
    }

    [Fact]
    public void SeedPairs_FourTeams_ProducesOneAndFourThenTwoAndThree()
    {
        List<Guid> seeds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        List<(Guid HomeTeamId, Guid? VisitorTeamId)> pairs = PlayoffSeeder.SeedPairs(seeds);

        Assert.Equal((seeds[0], (Guid?)seeds[3]), pairs[0]);
        Assert.Equal((seeds[1], (Guid?)seeds[2]), pairs[1]);
    }

    [Fact]
    public void SeedPairs_TwoTeams_ProducesSinglePair()
    {
        List<Guid> seeds = [Guid.NewGuid(), Guid.NewGuid()];

        List<(Guid HomeTeamId, Guid? VisitorTeamId)> pairs = PlayoffSeeder.SeedPairs(seeds);

        Assert.Equal([(seeds[0], (Guid?)seeds[1])], pairs);
    }

    [Fact]
    public void SeedPairs_FewerThanTwoTeams_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => PlayoffSeeder.SeedPairs([]));
        Assert.Throws<InvalidOperationException>(() => PlayoffSeeder.SeedPairs([Guid.NewGuid()]));
    }

    [Fact]
    public void SeedPairs_ThreeTeams_BestSeedGetsAByeAndOthersPlay()
    {
        List<Guid> seeds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        List<(Guid HomeTeamId, Guid? VisitorTeamId)> pairs = PlayoffSeeder.SeedPairs(seeds);

        Assert.Equal(2, pairs.Count);
        Assert.Equal((seeds[0], (Guid?)null), pairs[0]);
        Assert.Equal((seeds[1], (Guid?)seeds[2]), pairs[1]);
    }

    [Fact]
    public void SeedPairs_FiveTeams_TopThreeSeedsGetByes()
    {
        List<Guid> seeds = [.. Enumerable.Range(0, 5).Select(_ => Guid.NewGuid())];

        List<(Guid HomeTeamId, Guid? VisitorTeamId)> pairs = PlayoffSeeder.SeedPairs(seeds);

        Assert.Equal(4, pairs.Count);
        int byeCount = pairs.Count(pair => pair.VisitorTeamId is null);
        Assert.Equal(3, byeCount);

        List<Guid> teamsWithByes = [.. pairs.Where(pair => pair.VisitorTeamId is null).Select(pair => pair.HomeTeamId)];
        Assert.Contains(seeds[0], teamsWithByes);
        Assert.Contains(seeds[1], teamsWithByes);
        Assert.Contains(seeds[2], teamsWithByes);

        (Guid HomeTeamId, Guid? VisitorTeamId) realMatch = pairs.Single(pair => pair.VisitorTeamId is not null);
        Assert.Equal(seeds[3], realMatch.HomeTeamId);
        Assert.Equal(seeds[4], realMatch.VisitorTeamId);
    }

    [Fact]
    public void SeedPairs_SixTeams_TopTwoSeedsGetByes()
    {
        List<Guid> seeds = [.. Enumerable.Range(0, 6).Select(_ => Guid.NewGuid())];

        List<(Guid HomeTeamId, Guid? VisitorTeamId)> pairs = PlayoffSeeder.SeedPairs(seeds);

        Assert.Equal(4, pairs.Count);
        int byeCount = pairs.Count(pair => pair.VisitorTeamId is null);
        Assert.Equal(2, byeCount);

        List<Guid> teamsWithByes = [.. pairs.Where(pair => pair.VisitorTeamId is null).Select(pair => pair.HomeTeamId)];
        Assert.Contains(seeds[0], teamsWithByes);
        Assert.Contains(seeds[1], teamsWithByes);
    }
}
