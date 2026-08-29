using Application.DTOs.Team.Response;
using Application.Utils.Helper.TeamProfile;

using Domain.Entities.Models;
using Domain.Enums;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Pure-mapping tests for <see cref="TeamMatchMapper"/>: the win/loss result,
/// home/away score orientation and opponent resolution are all relative to the
/// team, so they are exercised here without a database.
/// </summary>
public class TeamMatchMapperTests
{
    private static Team MakeTeam(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = name.ToLowerInvariant(),
        ThreeLetterCode = name[..3].ToUpperInvariant(),
        LogoUrl = $"https://example.test/{name}.png",
        ShirtColor = "Red",
        Players = [],
        CreatedBy = "test",
    };

    [Fact]
    public void Map_FinishedHomeWin_OrientsScoresAndResultToTeam()
    {
        Team home = MakeTeam("HomeTeam");
        Team visitor = MakeTeam("VisitorTeam");
        Match match = new()
        {
            Type = MatchType.Regular,
            Slug = "m1",
            MatchDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsFinished = true,
            Status = MatchStatus.Played,
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            HomeScore = 90,
            VisitorScore = 80,
            WinningTeamId = home.Id,
            Venue = new Venue { Name = "Main Arena", Slug = "main-arena", Address = "123 St", CreatedBy = "test" },
            CreatedBy = "test",
        };

        TeamMatchResponse result = TeamMatchMapper.Map(match, home.Id);

        Assert.True(result.IsHome);
        Assert.Equal(home.Id == match.HomeTeamId, result.IsHome);
        Assert.Equal(visitor.Id, result.OpponentTeamId);
        Assert.Equal("VisitorTeam", result.OpponentName);
        Assert.Equal(90, result.TeamScore);
        Assert.Equal(80, result.OpponentScore);
        Assert.Equal("W", result.Result);
        Assert.Equal("Played", result.Status);
        Assert.Equal("Main Arena", result.VenueName);
        Assert.True(result.IsFinished);
    }

    [Fact]
    public void Map_FinishedAwayLoss_FlipsScoresAndReturnsLoss()
    {
        Team home = MakeTeam("HomeTeam");
        Team visitor = MakeTeam("VisitorTeam");
        Match match = new()
        {
            Type = MatchType.Regular,
            Slug = "m2",
            MatchDate = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            IsFinished = true,
            Status = MatchStatus.Played,
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            HomeScore = 70,
            VisitorScore = 60,
            WinningTeamId = home.Id,
            CreatedBy = "test",
        };

        TeamMatchResponse result = TeamMatchMapper.Map(match, visitor.Id);

        Assert.False(result.IsHome);
        Assert.Equal(home.Id, result.OpponentTeamId);
        Assert.Equal(60, result.TeamScore);
        Assert.Equal(70, result.OpponentScore);
        Assert.Equal("L", result.Result);
        Assert.Null(result.VenueName);
    }

    [Fact]
    public void Map_NotFinished_HasNullResultAndScores()
    {
        Team home = MakeTeam("HomeTeam");
        Team visitor = MakeTeam("VisitorTeam");
        Match match = new()
        {
            Type = MatchType.Regular,
            Slug = "m3",
            MatchDate = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            IsFinished = false,
            Status = MatchStatus.Scheduled,
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            CreatedBy = "test",
        };

        TeamMatchResponse result = TeamMatchMapper.Map(match, home.Id);

        Assert.Null(result.Result);
        Assert.Null(result.TeamScore);
        Assert.Null(result.OpponentScore);
        Assert.Equal("Scheduled", result.Status);
        Assert.False(result.IsFinished);
    }
}
