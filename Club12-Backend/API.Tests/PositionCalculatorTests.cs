using Application.Utils.Helper.Standings;

using Domain.Entities.Models;
using Domain.Enums;

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
            Slug = $"match-{Guid.NewGuid()}",
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
            Slug = $"match-{Guid.NewGuid()}",
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

    // ---- HU-79: configurable points ---------------------------------------

    [Fact]
    public void CalculatePositions_ConfiguredPoints_UsesGivenWinAndLossValues()
    {
        Team home = MakeTeam("Home");
        Team visitor = MakeTeam("Visitor");
        List<Match> matches = [MakeFinishedMatch(home, visitor, 80, 70)];

        List<Position> positions = PositionCalculator.CalculatePositions(matches, pointsForWin: 3, pointsForLoss: 0);

        Assert.Equal(3, positions.Single(p => p.TeamId == home.Id).Points);
        Assert.Equal(0, positions.Single(p => p.TeamId == visitor.Id).Points);
    }

    // ---- HU-80: tiebreaker chain ------------------------------------------

    [Fact]
    public void CalculatePositions_TieOnPoints_BrokenByGamesWon()
    {
        // A goes 2-1 and B goes 1-3: both reach 5 points (2*W + 1*L), but A
        // has more wins, so PG breaks the tie before DG or H2H are consulted.
        Team a = MakeTeam("A");
        Team b = MakeTeam("B");
        Team[] fill = [.. Enumerable.Range(0, 7).Select(i => MakeTeam($"F{i}"))];

        List<Match> matches =
        [
            MakeFinishedMatch(a, fill[0], 90, 80),
            MakeFinishedMatch(a, fill[1], 90, 80),
            MakeFinishedMatch(fill[2], a, 90, 80),
            MakeFinishedMatch(b, fill[3], 90, 80),
            MakeFinishedMatch(fill[4], b, 90, 80),
            MakeFinishedMatch(fill[5], b, 90, 80),
            MakeFinishedMatch(fill[6], b, 90, 80),
        ];

        List<Position> positions = PositionCalculator.CalculatePositions(matches);

        Position pa = positions.Single(p => p.TeamId == a.Id);
        Position pb = positions.Single(p => p.TeamId == b.Id);
        Assert.Equal(pa.Points, pb.Points);
        Assert.True(positions.IndexOf(pa) < positions.IndexOf(pb));
        Assert.Equal(TiebreakerCriterion.GamesWon, pb.ResolvedBy);
    }

    [Fact]
    public void CalculatePositions_TieOnPointsAndWins_BrokenByPointsDifference()
    {
        // A and B are both 1-1 (equal PTS and PG) but A has a better overall
        // points difference, so DG breaks the tie.
        Team a = MakeTeam("A");
        Team b = MakeTeam("B");
        Team c = MakeTeam("C");
        Team d = MakeTeam("D");
        Team e = MakeTeam("E");
        Team f = MakeTeam("F");

        List<Match> matches =
        [
            MakeFinishedMatch(a, c, 90, 70), // A +20
            MakeFinishedMatch(d, a, 85, 80), // A -5  -> DG +15
            MakeFinishedMatch(b, e, 85, 80), // B +5
            MakeFinishedMatch(f, b, 90, 70), // B -20 -> DG -15
        ];

        List<Position> positions = PositionCalculator.CalculatePositions(matches);

        Position pa = positions.Single(p => p.TeamId == a.Id);
        Position pb = positions.Single(p => p.TeamId == b.Id);
        Assert.Equal(pa.Points, pb.Points);
        Assert.Equal(pa.Wins, pb.Wins);
        Assert.True(positions.IndexOf(pa) < positions.IndexOf(pb));
        Assert.Equal(TiebreakerCriterion.PointsDifference, pb.ResolvedBy);
    }

    [Fact]
    public void CalculatePositions_TieOnAllZoneCriteria_BrokenByHeadToHeadBetweenTwo()
    {
        // A and B end equal on PTS, PG and whole-zone DG; A beat B head to
        // head (their only meeting), so H2H breaks the tie.
        Team a = MakeTeam("A");
        Team b = MakeTeam("B");
        Team c = MakeTeam("C");
        Team d = MakeTeam("D");

        List<Match> matches =
        [
            MakeFinishedMatch(a, b, 90, 80), // A beats B, A +10 / B -10
            MakeFinishedMatch(c, a, 90, 80), // A loses, A -10 -> DG 0
            MakeFinishedMatch(b, d, 90, 80), // B beats D, B +10 -> DG 0
        ];

        List<Position> positions = PositionCalculator.CalculatePositions(matches);

        Position pa = positions.Single(p => p.TeamId == a.Id);
        Position pb = positions.Single(p => p.TeamId == b.Id);
        Assert.Equal(pa.Points, pb.Points);
        Assert.Equal(pa.Wins, pb.Wins);
        Assert.Equal(pa.PointsDifference, pb.PointsDifference);
        Assert.True(positions.IndexOf(pa) < positions.IndexOf(pb));
        Assert.Equal(TiebreakerCriterion.HeadToHead, pb.ResolvedBy);
    }

    [Fact]
    public void CalculatePositions_ThreeWayTie_BrokenByHeadToHeadMiniTable()
    {
        // A, B, C all finish 2-2 with DG 0. Their round-robin mini-table is
        // linear (A beat B and C, B beat C), so H2H fully resolves A>B>C.
        Team a = MakeTeam("A");
        Team b = MakeTeam("B");
        Team c = MakeTeam("C");
        Team[] ext = [.. Enumerable.Range(0, 6).Select(i => MakeTeam($"X{i}"))];

        List<Match> matches =
        [
            // Mini-table among the tied set.
            MakeFinishedMatch(a, b, 90, 80),
            MakeFinishedMatch(a, c, 90, 80),
            MakeFinishedMatch(b, c, 90, 80),
            // External games that equalize wins and DG across A, B, C.
            MakeFinishedMatch(ext[0], a, 90, 80), // A loss
            MakeFinishedMatch(ext[1], a, 90, 80), // A loss
            MakeFinishedMatch(b, ext[2], 90, 80), // B win
            MakeFinishedMatch(ext[3], b, 90, 80), // B loss
            MakeFinishedMatch(c, ext[4], 90, 80), // C win
            MakeFinishedMatch(c, ext[5], 90, 80), // C win
        ];

        List<Position> positions = PositionCalculator.CalculatePositions(matches);

        Position pa = positions.Single(p => p.TeamId == a.Id);
        Position pb = positions.Single(p => p.TeamId == b.Id);
        Position pc = positions.Single(p => p.TeamId == c.Id);

        Assert.Equal(pa.Points, pb.Points);
        Assert.Equal(pb.Points, pc.Points);
        Assert.Equal(pa.PointsDifference, pc.PointsDifference);

        // A, B, C occupy the top three slots, in that order.
        Assert.Equal([a.Id, b.Id, c.Id], positions.Take(3).Select(p => p.TeamId));
        Assert.Equal(TiebreakerCriterion.HeadToHead, pb.ResolvedBy);
        Assert.Equal(TiebreakerCriterion.HeadToHead, pc.ResolvedBy);
    }

    [Fact]
    public void CalculatePositions_TwoTeamsPlayedTwice_BrokenByHeadToHeadPointsDifference()
    {
        // A and B split their two head-to-head games (equal H2H result), so
        // the tie falls through to the points difference within those games.
        // A won its game by 20 and lost by 10, so its H2H difference is
        // better. External games keep whole-zone PTS/PG/DG equal.
        Team a = MakeTeam("A");
        Team b = MakeTeam("B");
        Team p = MakeTeam("P");
        Team q = MakeTeam("Q");
        Team r = MakeTeam("R");
        Team s = MakeTeam("S");

        List<Match> matches =
        [
            MakeFinishedMatch(a, b, 90, 70), // A wins by 20
            MakeFinishedMatch(b, a, 90, 80), // B wins by 10
            MakeFinishedMatch(a, p, 85, 80), // A win +5
            MakeFinishedMatch(q, a, 90, 75), // A loss -15 -> A DG 0
            MakeFinishedMatch(b, r, 90, 75), // B win +15
            MakeFinishedMatch(s, b, 85, 80), // B loss -5 -> B DG 0
        ];

        List<Position> positions = PositionCalculator.CalculatePositions(matches);

        Position pa = positions.Single(p => p.TeamId == a.Id);
        Position pb = positions.Single(p => p.TeamId == b.Id);
        Assert.Equal(pa.Points, pb.Points);
        Assert.Equal(pa.Wins, pb.Wins);
        Assert.Equal(pa.PointsDifference, pb.PointsDifference);
        Assert.True(positions.IndexOf(pa) < positions.IndexOf(pb));
        Assert.Equal(TiebreakerCriterion.HeadToHead, pb.ResolvedBy);
    }
}
