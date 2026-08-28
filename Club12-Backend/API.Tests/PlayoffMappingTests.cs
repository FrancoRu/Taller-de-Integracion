using Application.Utils.Helper.Playoff;

using Domain.Entities.Models;

namespace API.Tests;

/// <summary>
/// Covers the HU-45 position-range mapping validation and the HU-81
/// qualification resolver that turns final standings + ranges into the
/// ordered team list of each playoff cup.
/// </summary>
public class PlayoffMappingTests
{
    private static DivisionPlayoffMapping Range(int from, int to, string destination)
    {
        return new DivisionPlayoffMapping
        {
            FromPosition = from,
            ToPosition = to,
            Destination = destination,
            CreatedBy = "test",
        };
    }

    private static Position Pos(Guid teamId)
    {
        return new Position
        {
            TeamId = teamId,
            TeamName = "T",
            LogoUrl = "x",
            MatchesPlayed = 0,
            Wins = 0,
            Losses = 0,
            PointsFor = 0,
            PointsAgainst = 0,
            PointsDifference = 0,
            Points = 0,
        };
    }

    [Fact]
    public void Validate_NonOverlappingRanges_DoesNotThrow()
    {
        PlayoffMappingValidator.Validate(
        [
            Range(1, 4, "Copa Oro"),
            Range(5, 8, "Copa Plata"),
        ]);
    }

    [Fact]
    public void Validate_OverlappingRanges_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => PlayoffMappingValidator.Validate(
        [
            Range(1, 4, "Copa Oro"),
            Range(4, 8, "Copa Plata"),
        ]));
    }

    [Fact]
    public void Validate_InvalidRange_ToBeforeFrom_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => PlayoffMappingValidator.Validate(
        [
            Range(5, 2, "Copa Oro"),
        ]));
    }

    [Fact]
    public void Validate_EmptyDestination_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => PlayoffMappingValidator.Validate(
        [
            Range(1, 4, "  "),
        ]));
    }

    [Fact]
    public void Validate_EmptySet_DoesNotThrow()
    {
        PlayoffMappingValidator.Validate([]);
    }

    [Fact]
    public void Resolve_SingleDivision_PlacesEachRangeTeamsInItsCup()
    {
        List<Guid> teams = [.. Enumerable.Range(0, 8).Select(_ => Guid.NewGuid())];
        List<Position> standings = [.. teams.Select(Pos)];

        Dictionary<string, List<Guid>> result = PlayoffQualificationResolver.Resolve(
        [
            new PlayoffQualificationResolver.DivisionStandings
            {
                Standings = standings,
                Mappings =
                [
                    Range(1, 4, "Copa Oro"),
                    Range(5, 8, "Copa Plata"),
                ],
            },
        ]);

        Assert.Equal([teams[0], teams[1], teams[2], teams[3]], result["Copa Oro"]);
        Assert.Equal([teams[4], teams[5], teams[6], teams[7]], result["Copa Plata"]);
    }

    [Fact]
    public void Resolve_UnmappedPositions_DoNotQualify()
    {
        List<Guid> teams = [.. Enumerable.Range(0, 10).Select(_ => Guid.NewGuid())];
        List<Position> standings = [.. teams.Select(Pos)];

        Dictionary<string, List<Guid>> result = PlayoffQualificationResolver.Resolve(
        [
            new PlayoffQualificationResolver.DivisionStandings
            {
                Standings = standings,
                Mappings =
                [
                    Range(1, 4, "Copa Oro"),
                    Range(5, 8, "Copa Plata"),
                ],
            },
        ]);

        // Positions 9-10 are unmapped: they appear in no cup.
        List<Guid> everyone = [.. result.Values.SelectMany(v => v)];
        Assert.DoesNotContain(teams[8], everyone);
        Assert.DoesNotContain(teams[9], everyone);
    }

    [Fact]
    public void Resolve_MultipleDivisions_MergeIntoCupSeededByPositionThenDivision()
    {
        List<Guid> divA = [Guid.NewGuid(), Guid.NewGuid()];
        List<Guid> divB = [Guid.NewGuid(), Guid.NewGuid()];

        Dictionary<string, List<Guid>> result = PlayoffQualificationResolver.Resolve(
        [
            new PlayoffQualificationResolver.DivisionStandings
            {
                Standings = [.. divA.Select(Pos)],
                Mappings = [Range(1, 2, "Copa")],
                DivisionOrder = 0,
            },
            new PlayoffQualificationResolver.DivisionStandings
            {
                Standings = [.. divB.Select(Pos)],
                Mappings = [Range(1, 2, "Copa")],
                DivisionOrder = 1,
            },
        ]);

        // Ordered by finishing position first, then division order:
        // A1, B1, A2, B2.
        Assert.Equal([divA[0], divB[0], divA[1], divB[1]], result["Copa"]);
    }
}
