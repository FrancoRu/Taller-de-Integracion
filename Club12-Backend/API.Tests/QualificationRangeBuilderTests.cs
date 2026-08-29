using Application.DTOs.Divisions.Response;
using Application.Utils.Helper.Playoff;

using Domain.Entities.Models;

namespace API.Tests;

/// <summary>
/// Covers the HU-45 qualification-range derivation that turns a division's
/// playoff mappings into the ordered, cup-ranked ranges the public standings
/// table paints.
/// </summary>
public class QualificationRangeBuilderTests
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

    [Fact]
    public void Build_OrdersRangesTopDown_AndAssignsCupRank()
    {
        // Deliberately out of order to prove the builder sorts top-down.
        List<QualificationRangeResponse> ranges = QualificationRangeBuilder.Build(
        [
            Range(5, 8, "Copa Plata"),
            Range(1, 4, "Copa Oro"),
            Range(9, 12, "Copa Bronce"),
        ]);

        Assert.Collection(
            ranges,
            first =>
            {
                Assert.Equal(1, first.FromPosition);
                Assert.Equal(4, first.ToPosition);
                Assert.Equal("Copa Oro", first.CupName);
                Assert.Equal(0, first.Order);
            },
            second =>
            {
                Assert.Equal(5, second.FromPosition);
                Assert.Equal(8, second.ToPosition);
                Assert.Equal("Copa Plata", second.CupName);
                Assert.Equal(1, second.Order);
            },
            third =>
            {
                Assert.Equal(9, third.FromPosition);
                Assert.Equal(12, third.ToPosition);
                Assert.Equal("Copa Bronce", third.CupName);
                Assert.Equal(2, third.Order);
            });
    }

    [Fact]
    public void Build_NoMappings_ReturnsEmpty()
    {
        Assert.Empty(QualificationRangeBuilder.Build([]));
    }

    [Fact]
    public void Build_Null_ReturnsEmpty()
    {
        Assert.Empty(QualificationRangeBuilder.Build(null));
    }
}
