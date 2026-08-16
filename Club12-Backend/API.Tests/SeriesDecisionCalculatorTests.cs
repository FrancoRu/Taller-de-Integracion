using System;
using System.Collections.Generic;
using Application.Utils.Helper.Series;
using Domain.Entities.Models;
using Domain.Enums;
using Xunit;
using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

public class SeriesDecisionCalculatorTests
{
    private static Match MakeGame(Guid homeTeamId, Guid visitorTeamId, int gameNumber, Guid? winningTeamId, bool isFinished = true) => new()
    {
        Id = Guid.NewGuid(),
        CreatedBy = "test",
        MatchDate = new DateTime(2026, 1, gameNumber),
        Type = MatchType.Playoff,
        HomeTeamId = homeTeamId,
        VisitorTeamId = visitorTeamId,
        IsFinished = isFinished,
        WinningTeamId = winningTeamId,
        GameNumber = gameNumber,
    };

    [Fact]
    public void DetermineWinner_BestOfThree_TwoWinsForOneTeam_DecidesTheSeriesEarly()
    {
        Guid home = Guid.NewGuid();
        Guid visitor = Guid.NewGuid();
        MatchSeries series = new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "test",
            StageId = Guid.NewGuid(),
            HomeTeamId = home,
            VisitorTeamId = visitor,
            BestOf = 3,
            Matches =
            [
                MakeGame(home, visitor, 1, home),
                MakeGame(home, visitor, 2, home),
            ],
        };

        Guid? winner = SeriesDecisionCalculator.DetermineWinner(series);

        Assert.Equal(home, winner);
    }

    [Fact]
    public void DetermineWinner_BestOfThree_SplitOneEach_NotYetDecided()
    {
        Guid home = Guid.NewGuid();
        Guid visitor = Guid.NewGuid();
        MatchSeries series = new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "test",
            StageId = Guid.NewGuid(),
            HomeTeamId = home,
            VisitorTeamId = visitor,
            BestOf = 3,
            Matches =
            [
                MakeGame(home, visitor, 1, home),
                MakeGame(home, visitor, 2, visitor),
            ],
        };

        Guid? winner = SeriesDecisionCalculator.DetermineWinner(series);

        Assert.Null(winner);
    }

    [Fact]
    public void DetermineWinner_UnfinishedGamesAreIgnored()
    {
        Guid home = Guid.NewGuid();
        Guid visitor = Guid.NewGuid();
        MatchSeries series = new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "test",
            StageId = Guid.NewGuid(),
            HomeTeamId = home,
            VisitorTeamId = visitor,
            BestOf = 3,
            Matches =
            [
                MakeGame(home, visitor, 1, home),
                MakeGame(home, visitor, 2, null, isFinished: false),
            ],
        };

        Guid? winner = SeriesDecisionCalculator.DetermineWinner(series);

        Assert.Null(winner);
    }

    [Fact]
    public void DetermineWinner_BestOfOne_SingleGameDecidesImmediately()
    {
        Guid home = Guid.NewGuid();
        Guid visitor = Guid.NewGuid();
        MatchSeries series = new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "test",
            StageId = Guid.NewGuid(),
            HomeTeamId = home,
            VisitorTeamId = visitor,
            BestOf = 1,
            Matches = [MakeGame(home, visitor, 1, visitor)],
        };

        Guid? winner = SeriesDecisionCalculator.DetermineWinner(series);

        Assert.Equal(visitor, winner);
    }

    [Fact]
    public void DetermineWinner_NoGamesYet_ReturnsNull()
    {
        MatchSeries series = new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "test",
            StageId = Guid.NewGuid(),
            HomeTeamId = Guid.NewGuid(),
            VisitorTeamId = Guid.NewGuid(),
            BestOf = 5,
            Matches = [],
        };

        Guid? winner = SeriesDecisionCalculator.DetermineWinner(series);

        Assert.Null(winner);
    }
}
