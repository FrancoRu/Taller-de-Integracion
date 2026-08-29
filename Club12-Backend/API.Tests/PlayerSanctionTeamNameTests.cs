using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerSanction.Request;
using Application.DTOs.PlayerSanction.Response;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Covers the sanction subject name for TEAM sanctions (QA wave 1, Bug 4): when
/// a sanction targets a team (SubjectType = Team), the team NAME must appear in
/// both the created response and the sanctions list, so the list shows WHO was
/// sanctioned — not a blank subject. Regression: the list query does not load
/// the Team navigation, so the mapper alone left TeamName null; the subject is
/// now resolved explicitly regardless of which navigations are loaded.
/// </summary>
public class PlayerSanctionTeamNameTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly DateTime Anchor = new(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly CustomWebApplicationFactory _factory;

    public PlayerSanctionTeamNameTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TeamSanction_ExposesTeamName_InCreatedResponseAndList()
    {
        Guid teamId;
        Guid matchId;
        string teamName;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            (teamId, matchId, teamName) = await SeedTeamAndMatchAsync(db);
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        CreatePlayerSanctionRequest request = new()
        {
            Duration = 2,
            IssuedDate = Anchor,
            Description = "Institutional sanction against the team.",
            SubjectType = SanctionSubjectType.Team,
            TeamId = teamId,
            MatchId = matchId,
        };

        HttpResponseMessage createResponse = await client.PostAsJsonAsync("api/player-sanctions", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        PlayerSanctionResponse? created =
            await createResponse.Content.ReadFromJsonAsync<PlayerSanctionResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(SanctionSubjectType.Team, created!.SubjectType);
        Assert.Equal(teamId, created.TeamId);
        Assert.Equal(teamName, created.TeamName);
        Assert.Null(created.PlayerFullName);

        // The list (find) does not load the Team navigation; TeamName must still
        // be present for the subject to render.
        HttpResponseMessage listResponse = await client.GetAsync(
            $"api/player-sanctions/find?TeamId={teamId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        PaginatedResponse<PlayerSanctionResponse>? list =
            await listResponse.Content.ReadFromJsonAsync<PaginatedResponse<PlayerSanctionResponse>>(JsonOptions);
        Assert.NotNull(list);
        PlayerSanctionResponse listed = Assert.Single(list!.Items);
        Assert.Equal(SanctionSubjectType.Team, listed.SubjectType);
        Assert.Equal(teamName, listed.TeamName);
    }

    private static async Task<(Guid TeamId, Guid MatchId, string TeamName)> SeedTeamAndMatchAsync(
        ApplicationDBContext db)
    {
        Guid divisionId = Guid.NewGuid();
        Guid stageId = Guid.NewGuid();
        Guid teamId = Guid.NewGuid();
        string teamName = $"Sanctioned Team {Guid.NewGuid():N}";

        Tournament tournament = new()
        {
            Description = "Team-name sanction test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = Anchor.AddDays(-1),
            StartDate = Anchor,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Id = divisionId,
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            Stages = [],
            CreatedBy = "test",
        };

        Stage stage = new()
        {
            Id = stageId,
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = Anchor,
            EndDate = Anchor.AddDays(60),
            DivisionId = divisionId,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        Team team = new()
        {
            Id = teamId,
            Name = teamName,
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = "SAN",
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Red",
            Players = [],
            CreatedBy = "test",
        };

        Match match = new()
        {
            MatchDate = Anchor,
            Type = MatchType.Regular,
            Round = 1,
            Slug = $"match-{Guid.NewGuid()}",
            IsFinished = true,
            HomeTeamId = teamId,
            Stage = stage,
            StageId = stageId,
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Divisions.Add(division);
        db.Stages.Add(stage);
        db.Teams.Add(team);
        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return (teamId, match.Id, teamName);
    }
}
