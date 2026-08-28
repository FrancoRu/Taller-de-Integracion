using API.AutoMapperProfiles;

using Application.DTOs.Divisions.Request;
using Application.DTOs.Divisions.Response;
using Application.DTOs.Match.Request;
using Application.DTOs.Match.Response;
using Application.DTOs.Team.Response;

using AutoMapper;

using Domain.Entities.Models;

using Microsoft.Extensions.Logging.Abstractions;

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

    private static Team CreateTeam(string name)
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

    private static Match CreateMatch(Team? homeTeam, Team? visitorTeam)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "system",
            MatchDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Type = Domain.Enums.MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            HomeTeam = homeTeam,
            VisitorTeam = visitorTeam,
            IsFinished = false,
        };
    }

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

    /// <summary>
    /// HU-67: editing a match's calendar date/time (the UpdateMatchRequest path)
    /// must never move it to another round. The request carries no Round, so
    /// mapping it onto an existing match leaves the round untouched while still
    /// applying the new date.
    /// </summary>
    [Fact]
    public void Map_UpdateMatchRequestOntoMatch_ChangesDate_ButLeavesRoundUnchanged()
    {
        Match match = CreateMatch(homeTeam: null, visitorTeam: null);
        match.Round = 3;
        DateTime originalDate = match.MatchDate;
        DateTime newDate = originalDate.AddDays(5);

        UpdateMatchRequest request = new() { MatchDate = newDate };
        IMapper mapper = CreateMapper();

        mapper.Map(request, match);

        Assert.Equal(3, match.Round);
        Assert.Equal(newDate, match.MatchDate);
    }

    /// <summary>
    /// The round flows through to the response DTOs so the frontend can group
    /// the fixture by matchday (HU-63).
    /// </summary>
    [Fact]
    public void Map_ToDetailedMatchResponse_ExposesRound()
    {
        Match match = CreateMatch(homeTeam: null, visitorTeam: null);
        match.Round = 7;
        IMapper mapper = CreateMapper();

        DetailedMatchResponse response = mapper.Map<DetailedMatchResponse>(match);

        Assert.Equal(7, response.Round);
    }

    /// <summary>
    /// The team's ClubId flows through to the response DTO so the frontend can
    /// link a team back to its club.
    /// </summary>
    [Fact]
    public void Map_ToTeamResponse_ExposesClubId()
    {
        MapperConfiguration configuration = new(cfg => cfg.AddProfile<TeamProfile>(), NullLoggerFactory.Instance);
        IMapper mapper = configuration.CreateMapper();

        Guid clubId = Guid.NewGuid();
        Team team = CreateTeam("River Plate");
        team.ClubId = clubId;

        TeamResponse response = mapper.Map<TeamResponse>(team);

        Assert.Equal(clubId, response.ClubId);
    }

    /// <summary>
    /// HU-110: QualifiersPerGroup round-trips through the division mappings —
    /// CreateDivisionRequest -> Division -> DivisionResponse — by AutoMapper's
    /// name convention, so the wizard's value reaches the entity and is echoed
    /// back in the response.
    /// </summary>
    [Fact]
    public void Map_DivisionQualifiersPerGroup_RoundTripsThroughRequestAndResponse()
    {
        MapperConfiguration configuration = new(cfg => cfg.AddProfile<DivisionProfile>(), NullLoggerFactory.Instance);
        IMapper mapper = configuration.CreateMapper();

        CreateDivisionRequest request = new()
        {
            Name = "Copa Club12",
            TournamentId = Guid.NewGuid(),
            IsCrossDivisionCup = true,
            QualifiersPerGroup = 2,
        };

        Division division = mapper.Map<Division>(request);
        Assert.Equal(2, division.QualifiersPerGroup);

        DivisionResponse response = mapper.Map<DivisionResponse>(division);
        Assert.Equal(2, response.QualifiersPerGroup);
    }

    /// <summary>
    /// An omitted QualifiersPerGroup defaults to 1 on the request DTO and
    /// survives the mapping unchanged, leaving every existing division's
    /// seeding behavior intact.
    /// </summary>
    [Fact]
    public void Map_DivisionQualifiersPerGroup_DefaultsToOne()
    {
        MapperConfiguration configuration = new(cfg => cfg.AddProfile<DivisionProfile>(), NullLoggerFactory.Instance);
        IMapper mapper = configuration.CreateMapper();

        CreateDivisionRequest request = new()
        {
            Name = "Primera",
            TournamentId = Guid.NewGuid(),
        };

        Division division = mapper.Map<Division>(request);

        Assert.Equal(1, division.QualifiersPerGroup);
    }
}
