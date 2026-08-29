using Application.Utils.Helper.Standings;

using Domain.Entities.Models;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Covers the disciplinary point-deduction adjustment (deducción de puntos):
/// a team's accumulated deductions are subtracted from its standings total,
/// the table re-ranks accordingly, and the applied deduction is surfaced on
/// the position row. The raw subtraction is allowed to drop a team's points
/// below zero — a penalty can legitimately sink a team, mirroring real
/// disciplinary sanctions; the value is shown as-is and never clamped.
/// </summary>
public class PositionCalculatorDeductionTests
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

    private static TeamPointDeduction MakeDeduction(Guid teamId, int points, string reason)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "system",
            DivisionId = Guid.NewGuid(),
            TeamId = teamId,
            Points = points,
            Reason = reason,
        };
    }

    [Fact]
    public void CalculatePositions_WithDeduction_SubtractsFromTotalAndReportsIt()
    {
        Team winner = MakeTeam("Winner");
        Team loser = MakeTeam("Loser");
        List<Match> matches = [MakeFinishedMatch(winner, loser, 80, 70)];
        List<TeamPointDeduction> deductions = [MakeDeduction(winner.Id, 1, "Alineación indebida")];

        List<Position> positions = PositionCalculator.CalculatePositions(
            matches, deductions: deductions);

        Position winnerPosition = Assert.Single(positions, p => p.TeamId == winner.Id);
        // 2 for the win minus a 1-point deduction.
        Assert.Equal(1, winnerPosition.Points);
        Assert.NotNull(winnerPosition.PointDeduction);
        Assert.Equal(1, winnerPosition.PointDeduction!.Points);
        Assert.Equal("Alineación indebida", winnerPosition.PointDeduction.Reason);

        Position loserPosition = Assert.Single(positions, p => p.TeamId == loser.Id);
        Assert.Null(loserPosition.PointDeduction);
    }

    [Fact]
    public void CalculatePositions_MultipleDeductionsForOneTeam_AggregatesPointsAndReasons()
    {
        Team team = MakeTeam("Penalised");
        Team other = MakeTeam("Other");
        List<Match> matches = [MakeFinishedMatch(team, other, 90, 80)];
        List<TeamPointDeduction> deductions =
        [
            MakeDeduction(team.Id, 1, "Motivo A"),
            MakeDeduction(team.Id, 2, "Motivo B"),
        ];

        List<Position> positions = PositionCalculator.CalculatePositions(
            matches, deductions: deductions);

        Position row = Assert.Single(positions, p => p.TeamId == team.Id);
        // 2 for the win minus (1 + 2) deducted.
        Assert.Equal(-1, row.Points);
        Assert.NotNull(row.PointDeduction);
        Assert.Equal(3, row.PointDeduction!.Points);
        Assert.Contains("Motivo A", row.PointDeduction.Reason);
        Assert.Contains("Motivo B", row.PointDeduction.Reason);
    }

    [Fact]
    public void CalculatePositions_DeductionDropsTeam_ReordersStandings()
    {
        // Two teams that both won one game: without a deduction the head-to-head
        // winner leads. A heavy deduction on the leader must drop it below the
        // other team in the final ordering.
        Team leader = MakeTeam("Leader");
        Team chaser = MakeTeam("Chaser");
        Team filler1 = MakeTeam("Filler1");
        Team filler2 = MakeTeam("Filler2");

        List<Match> matches =
        [
            MakeFinishedMatch(leader, filler1, 100, 50),
            MakeFinishedMatch(chaser, filler2, 60, 58),
        ];

        // No deduction: leader (DIF +50) ranks above chaser (DIF +2).
        List<Position> raw = PositionCalculator.CalculatePositions(matches);
        Assert.Equal(leader.Id, raw[0].TeamId);

        // Deduct the leader's 2 points: it falls to 0, below chaser's 2.
        List<TeamPointDeduction> deductions = [MakeDeduction(leader.Id, 2, "Sanción")];
        List<Position> adjusted = PositionCalculator.CalculatePositions(
            matches, deductions: deductions);

        Position chaserRow = adjusted.First(p => p.TeamId == chaser.Id);
        Position leaderRow = adjusted.First(p => p.TeamId == leader.Id);
        Assert.True(
            adjusted.IndexOf(chaserRow) < adjusted.IndexOf(leaderRow),
            "The deducted leader must rank below the chaser after the penalty.");
        Assert.Equal(0, leaderRow.Points);
    }

    [Fact]
    public void CalculatePositions_NullOrEmptyDeductions_LeavesTotalsUntouched()
    {
        Team a = MakeTeam("A");
        Team b = MakeTeam("B");
        List<Match> matches = [MakeFinishedMatch(a, b, 80, 70)];

        List<Position> withNull = PositionCalculator.CalculatePositions(matches, deductions: null);
        List<Position> withEmpty = PositionCalculator.CalculatePositions(matches, deductions: []);

        Assert.Equal(2, withNull.Single(p => p.TeamId == a.Id).Points);
        Assert.Null(withNull.Single(p => p.TeamId == a.Id).PointDeduction);
        Assert.Equal(2, withEmpty.Single(p => p.TeamId == a.Id).Points);
    }
}
