using Application.DTOs.Match.Response;
using Application.DTOs.Player.Response;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// A match's roster (<see cref="TeamDetailedMatchResponse.Players"/>) must
/// reflect each player's REAL season-scoped medical-record status. Found live:
/// <c>CreateMap&lt;Player, PublicPlayerResponse&gt;()</c> has no member mapping
/// for MedicalRecordStatus/IsHabilitado/JerseyNumber — those live on
/// <see cref="PlayerTeamRegistration"/>, not <see cref="Player"/> — so every
/// roster entry silently defaulted to not-habilitado, even for players who
/// genuinely are (HU-57/HU-62). Fixed via an AfterMap step on
/// <c>CreateMap&lt;Match, DetailedMatchResponse&gt;()</c> that resolves each
/// roster player's matching registration for the match's tournament.
/// </summary>
public class MatchRosterEligibilityMappingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MatchRosterEligibilityMappingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void MappingMatchToDetailedResponse_ResolvesRosterHabilitadoFromRegistration()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IMapper mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        Guid tournamentId = Guid.NewGuid();
        Guid teamId = Guid.NewGuid();

        Team home = new()
        {
            Id = teamId,
            Name = "Home",
            Slug = $"home-{Guid.NewGuid()}",
            ThreeLetterCode = "HOM",
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            Players = [],
            CreatedBy = "test",
        };

        Player habilitado = NewPlayer(home, "Habilitado");
        habilitado.PlayerTeamRegistrations =
        [
            new PlayerTeamRegistration
            {
                PlayerId = habilitado.Id,
                TeamId = teamId,
                TournamentId = tournamentId,
                MedicalRecordStatus = MedicalRecordStatus.Approved,
                MedicalRecordFileUrl = $"{teamId}/{habilitado.Id}/ficha.pdf",
                JerseyNumber = 7,
                CreatedBy = "test",
            },
        ];

        Player pending = NewPlayer(home, "Pending");
        pending.PlayerTeamRegistrations =
        [
            new PlayerTeamRegistration
            {
                PlayerId = pending.Id,
                TeamId = teamId,
                TournamentId = tournamentId,
                MedicalRecordStatus = MedicalRecordStatus.Pending,
                CreatedBy = "test",
            },
        ];

        home.Players = [habilitado, pending];

        Tournament tournament = new()
        {
            Id = tournamentId,
            Description = "Test",
            Name = "Test tournament",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = DateTime.UtcNow.Date,
            StartDate = DateTime.UtcNow.Date,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Name = "Division",
            Slug = $"division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournamentId,
            Stages = [],
            CreatedBy = "test",
        };

        Stage stage = new()
        {
            Name = "Group",
            Slug = $"stage-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            Division = division,
            DivisionId = division.Id,
            Matches = [],
            CreatedBy = "test",
        };

        Match match = new()
        {
            Slug = $"match-{Guid.NewGuid()}",
            Type = MatchType.Regular,
            IsFinished = false,
            HomeTeam = home,
            HomeTeamId = home.Id,
            Stage = stage,
            StageId = stage.Id,
            MatchDate = DateTime.UtcNow.Date,
            CreatedBy = "test",
        };

        DetailedMatchResponse response = mapper.Map<DetailedMatchResponse>(match);

        PublicPlayerResponse mappedHabilitado = Assert.Single(
            response.HomeTeam!.Players, p => p.Id == habilitado.Id);
        Assert.True(mappedHabilitado.IsHabilitado);
        Assert.Equal(MedicalRecordStatus.Approved, mappedHabilitado.MedicalRecordStatus);
        Assert.Equal(7, mappedHabilitado.JerseyNumber);

        PublicPlayerResponse mappedPending = Assert.Single(
            response.HomeTeam!.Players, p => p.Id == pending.Id);
        Assert.False(mappedPending.IsHabilitado);
        Assert.Equal(MedicalRecordStatus.Pending, mappedPending.MedicalRecordStatus);
    }

    private static Player NewPlayer(Team team, string lastName) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Test",
        LastName = lastName,
        Slug = $"player-{Guid.NewGuid()}",
        DocumentNumber = Guid.NewGuid().ToString("N")[..10],
        IsSanctioned = false,
        BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        SocialSecurity = "OSDE",
        Team = team,
        TeamId = team.Id,
        CreatedBy = "test",
    };
}
