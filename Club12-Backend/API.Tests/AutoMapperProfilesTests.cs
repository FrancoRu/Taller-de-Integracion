using System;
using API.AutoMapperProfiles;
using Application.DTOs.Match.Response;
using AutoMapper;
using Domain.Entities.Models;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace API.Tests;

/// <summary>
/// Verifies the Match to MinimalMatchResponse mapping resolves home and visitor team names
/// correctly when teams are assigned, and returns null instead of throwing when they are not.
/// </summary>
public class AutoMapperProfilesTests
{
    private static IMapper CreateMapper()
    {
        MapperConfiguration configuration = new(cfg => cfg.AddProfile<MatchProfile>(), NullLoggerFactory.Instance);
        return configuration.CreateMapper();
    }

    private static Team CreateTeam(string name) => new()
    {
        Id = Guid.NewGuid(),
        CreatedBy = "system",
        Name = name,
        ThreeLetterCode = name[..Math.Min(3, name.Length)].ToUpperInvariant(),
        LogoUrl = "https://example.com/logo.png",
        ShirtColor = "red",
        Players = [],
    };

    private static Match CreateMatch(Team? homeTeam, Team? visitorTeam) => new()
    {
        Id = Guid.NewGuid(),
        CreatedBy = "system",
        MatchDate = new DateTime(2026, 1, 1),
        Type = Domain.Enums.MatchType.Regular,
        HomeTeam = homeTeam,
        VisitorTeam = visitorTeam,
        IsFinished = false,
    };

    [Fact]
    public void Map_ToMinimalMatchResponse_WithAssignedTeams_ResolvesRealTeamNames()
    {
        Team homeTeam = CreateTeam("River Plate");
        Team visitorTeam = CreateTeam("Boca Juniors");
        Match match = CreateMatch(homeTeam, visitorTeam);
        IMapper mapper = CreateMapper();

        MinimalMatchResponse response = mapper.Map<MinimalMatchResponse>(match);

        Assert.Equal("River Plate", response.HomeTeamName);
        Assert.Equal("Boca Juniors", response.VisitorTeamName);
    }

    /// <summary>
    /// Confirms that a match with no assigned home or visitor team maps to null team names
    /// instead of throwing.
    /// </summary>
    [Fact]
    public void Map_ToMinimalMatchResponse_WithUnassignedTeams_DegradesToNullInsteadOfThrowing()
    {
        Match match = CreateMatch(homeTeam: null, visitorTeam: null);
        IMapper mapper = CreateMapper();

        MinimalMatchResponse response = mapper.Map<MinimalMatchResponse>(match);

        Assert.Null(response.HomeTeamName);
        Assert.Null(response.VisitorTeamName);
    }
}
