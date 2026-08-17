using Application.Utils.Helper.Standings;

using Domain.Entities.Models;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

public class PositionCalculatorTests
{
    private static Team MakeTeam(string name)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "system",
            Name = name,
            Slug = $"{name}-{Guid.NewGuid()}",
            ThreeLetterCode = name[..Math.Min(3, name.Length)].ToUpperInvariant(),
            LogoUrl = "https://example.com/logo.png",
            ShirtColor = "red",
            Players = [],
        };
    }

    private static Match MakeFinishedMatch(Team home, Team visitor, int homeScore, int visitorScore)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "system",
            MatchDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Type = MatchType.Regular,
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            HomeScore = homeScore,
            VisitorScore = visitorScore,
            IsFinished = true,
            WinningTeam = homeScore > visitorScore ? home : visitor,
            WinningTeamId = homeScore > visitorScore ? home.Id : visitor.Id,
        };
    }

    [Fact]
    public void CalculatePositions_TwoTeamsOneMatch_AwardsTwoPointsForWinOneForLoss()
    {
        Team home = MakeTeam("Home");
        Team visitor = MakeTeam("Visitor");
        List<Match> matches = [MakeFinishedMatch(home, visitor, 80, 70)];

        List<Position> positions = PositionCalculator.CalculatePositions(matches);

        Position homePosition = Assert.Single(positions, p => p.TeamId == home.Id);
        Assert.Equal(home.LogoUrl, homePosition.LogoUrl);
        Assert.Equal(1, homePosition.MatchesPlayed);
        Assert.Equal(1, homePosition.Wins);
        Assert.Equal(0, homePosition.Losses);
        Assert.Equal(80, homePosition.PointsFor);
        Assert.Equal(70, homePosition.PointsAgainst);
        Assert.Equal(10, homePosition.PointsDifference);
        Assert.Equal(2, homePosition.Points);

        Position visitorPosition = Assert.Single(positions, p => p.TeamId == visitor.Id);
        Assert.Equal(1, visitorPosition.MatchesPlayed);
        Assert.Equal(0, visitorPosition.Wins);
        Assert.Equal(1, visitorPosition.Losses);
        Assert.Equal(70, visitorPosition.PointsFor);
        Assert.Equal(80, visitorPosition.PointsAgainst);
        Assert.Equal(-10, visitorPosition.PointsDifference);
        Assert.Equal(1, visitorPosition.Points);
    }

    [Fact]
    public void CalculatePositions_AccumulatesAcrossMultipleMatchesForTheSameTeam()
    {
        Team a = MakeTeam("A");
        Team b = MakeTeam("B");
        Team c = MakeTeam("C");
        List<Match> matches =
        [
            MakeFinishedMatch(a, b, 90, 80),
            MakeFinishedMatch(c, a, 85, 60),
        ];

        List<Position> positions = PositionCalculator.CalculatePositions(matches);

        Position teamA = Assert.Single(positions, p => p.TeamId == a.Id);
        Assert.Equal(2, teamA.MatchesPlayed);
        Assert.Equal(1, teamA.Wins);
        Assert.Equal(1, teamA.Losses);
        Assert.Equal(150, teamA.PointsFor);
        Assert.Equal(165, teamA.PointsAgainst);
        Assert.Equal(3, teamA.Points);
    }

    [Fact]
    public void CalculatePositions_UnfinishedOrUnseededMatches_AreIgnored()
    {
        Team a = MakeTeam("A");
        Team b = MakeTeam("B");
        Match unfinished = MakeFinishedMatch(a, b, 90, 80);
        unfinished.IsFinished = false;
        Match unseeded = new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "system",
            MatchDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Type = MatchType.Regular,
            HomeTeam = null,
            VisitorTeam = null,
            IsFinished = true,
        };

        List<Position> positions = PositionCalculator.CalculatePositions([unfinished, unseeded]);

        Assert.Empty(positions);
    }

    [Fact]
    public void CalculatePositions_NoMatches_ReturnsEmptyList()
    {
        List<Position> positions = PositionCalculator.CalculatePositions([]);

        Assert.Empty(positions);
    }

    [Fact]
    public void CalculatePositions_RanksByPointsThenPointsDifferenceThenPointsFor()
    {
        Team a = MakeTeam("A");
        Team b = MakeTeam("B");
        Team c = MakeTeam("C");

        List<Match> matches =
        [
            MakeFinishedMatch(a, b, 90, 70),
            MakeFinishedMatch(a, c, 85, 80),
            MakeFinishedMatch(b, c, 100, 60),
        ];

        List<Position> positions = PositionCalculator.CalculatePositions(matches);

        Assert.Equal([a.Id, b.Id, c.Id], positions.Select(p => p.TeamId));
    }
}
