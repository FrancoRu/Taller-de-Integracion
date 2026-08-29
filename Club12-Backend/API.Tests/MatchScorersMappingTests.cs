using API.AutoMapperProfiles;

using Application.DTOs.Match.Response;

using AutoMapper;

using Domain.Entities.Models;

using Microsoft.Extensions.Logging.Abstractions;

using System;

namespace API.Tests;

/// <summary>
/// Verifies the public match detail attributes each scorer to its own team (via
/// the player's TeamId, since Scorer has no TeamId) and aggregates points per
/// player — so the match scoreboard can list "goleadores del partido" per side.
/// </summary>
public class MatchScorersMappingTests
{
    private static IMapper CreateMapper()
    {
        MapperConfiguration configuration = new(
            cfg =>
            {
                cfg.AddProfile<MatchProfile>();
                cfg.AddProfile<TeamProfile>();
            },
            NullLoggerFactory.Instance);
        return configuration.CreateMapper();
    }

    [Fact]
    public void DetailedMatch_AttributesScorersToTheirTeam_AndAggregatesPerPlayer()
    {
        Team home = CreateTeam("Home");
        Team visitor = CreateTeam("Visitor");
        Player homePlayer = CreatePlayer(home, "Perez", "Juan");
        Player visitorPlayer = CreatePlayer(visitor, "Gomez", "Luis");

        Match match = new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "system",
            MatchDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Type = Domain.Enums.MatchType.Regular,
            Slug = "home-vs-visitor",
            IsFinished = true,
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            HomeScore = 20,
            VisitorScore = 10,
            Scorers =
            [
                new Scorer { CreatedBy = "system", PlayerId = homePlayer.Id, Player = homePlayer, Points = 8, MatchId = Guid.Empty },
                new Scorer { CreatedBy = "system", PlayerId = homePlayer.Id, Player = homePlayer, Points = 12, MatchId = Guid.Empty },
                new Scorer { CreatedBy = "system", PlayerId = visitorPlayer.Id, Player = visitorPlayer, Points = 10, MatchId = Guid.Empty },
            ],
        };

        DetailedMatchResponse response = CreateMapper().Map<DetailedMatchResponse>(match);

        Assert.Single(response.HomeTeam!.Scorers);
        Assert.Equal(homePlayer.Id, response.HomeTeam!.Scorers[0].PlayerId);
        Assert.Equal(20, response.HomeTeam!.Scorers[0].Points); // 8 + 12 aggregated per player

        Assert.Single(response.VisitorTeam!.Scorers);
        Assert.Equal(visitorPlayer.Id, response.VisitorTeam!.Scorers[0].PlayerId);
        Assert.Equal(10, response.VisitorTeam!.Scorers[0].Points);
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

    private static Player CreatePlayer(Team team, string lastName, string firstName)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "system",
            FirstName = firstName,
            LastName = lastName,
            Slug = $"{lastName}-{firstName}-{Guid.NewGuid()}",
            DocumentNumber = "12345678",
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "SS-0001",
            TeamId = team.Id,
            Team = team,
        };
    }
}
