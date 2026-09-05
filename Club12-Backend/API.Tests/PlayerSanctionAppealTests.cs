using Application.DTOs.PlayerSanction.Request;
using Application.DTOs.PlayerSanction.Response;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Characterization tests for the sanction appeal state machine implemented in
/// PlayerSanctionController.AppealPlayerSanction and
/// PlayerSanctionController.ResolvePlayerSanctionAppeal — an
/// accepted architecture smell (business logic living in the controller rather than a
/// dedicated service). Locks in the current guard clauses and persisted status transitions via
/// a real HTTP round trip through CustomWebApplicationFactory; introduces no
/// behavior change.
/// </summary>
public class PlayerSanctionAppealTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    // Mirrors the API's JSON settings (enums serialized as strings) so the
    // typed response can be read back.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public PlayerSanctionAppealTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AppealPlayerSanction_AlreadyPending_Returns400AndStatusUnchanged()
    {
        Guid sanctionId = await SeedSanctionIdAsync(SanctionAppealStatus.Pending);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        AppealPlayerSanctionRequest request = new() { Reason = "Second appeal while pending" };

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"api/player-sanctions/{sanctionId}/appeal", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(SanctionAppealStatus.Pending, await ReadAppealStatusAsync(sanctionId));
    }

    [Fact]
    public async Task AppealPlayerSanction_FromNone_Succeeds_AndPersistsPending()
    {
        Guid sanctionId = await SeedSanctionIdAsync(SanctionAppealStatus.None);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        AppealPlayerSanctionRequest request = new() { Reason = "I was not at fault" };

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"api/player-sanctions/{sanctionId}/appeal", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SanctionAppealStatus.Pending, await ReadAppealStatusAsync(sanctionId));
    }

    [Fact]
    public async Task AppealPlayerSanction_MissingSanction_Returns404()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        AppealPlayerSanctionRequest request = new() { Reason = "Does not matter" };

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"api/player-sanctions/{Guid.NewGuid()}/appeal", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public static readonly TheoryData<SanctionAppealStatus> NotPendingStatuses =
    [
        SanctionAppealStatus.None,
        SanctionAppealStatus.Accepted,
        SanctionAppealStatus.Rejected,
    ];

    [Theory]
    [MemberData(nameof(NotPendingStatuses))]
    public async Task ResolvePlayerSanctionAppeal_NotPending_Returns400AndStatusUnchanged(
        SanctionAppealStatus initialStatus)
    {
        Guid sanctionId = await SeedSanctionIdAsync(initialStatus);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        ResolveAppealRequest request = new() { Accepted = true, Resolution = "Reviewed" };

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"api/player-sanctions/{sanctionId}/appeal/resolve", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(initialStatus, await ReadAppealStatusAsync(sanctionId));
    }

    [Fact]
    public async Task ResolvePlayerSanctionAppeal_Accept_PersistsAccepted()
    {
        Guid sanctionId = await SeedSanctionIdAsync(SanctionAppealStatus.Pending);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        ResolveAppealRequest request = new() { Accepted = true, Resolution = "Appeal accepted" };

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"api/player-sanctions/{sanctionId}/appeal/resolve", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SanctionAppealStatus.Accepted, await ReadAppealStatusAsync(sanctionId));
    }

    [Fact]
    public async Task ResolvePlayerSanctionAppeal_Accept_LiftsTheSanctionImmediately()
    {
        // Regression: accepting an appeal used to only record AppealStatus —
        // FechasRemaining/IsActive were derived purely from Duration and rounds
        // served, with no reference to AppealStatus at all, so an accepted
        // appeal had zero practical effect: the subject stayed sanctioned for
        // the full original duration regardless of the decision.
        Guid sanctionId = await SeedSanctionIdAsync(SanctionAppealStatus.Pending);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        ResolveAppealRequest request = new() { Accepted = true, Resolution = "Appeal accepted" };

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"api/player-sanctions/{sanctionId}/appeal/resolve", request);

        PlayerSanctionResponse? body = await response.Content.ReadFromJsonAsync<PlayerSanctionResponse>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(0, body.FechasRemaining);
        Assert.False(body.IsActive);
    }

    [Fact]
    public async Task ResolvePlayerSanctionAppeal_Reject_PersistsRejected()
    {
        Guid sanctionId = await SeedSanctionIdAsync(SanctionAppealStatus.Pending);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        ResolveAppealRequest request = new() { Accepted = false, Resolution = "Appeal rejected" };

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"api/player-sanctions/{sanctionId}/appeal/resolve", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SanctionAppealStatus.Rejected, await ReadAppealStatusAsync(sanctionId));
    }

    [Fact]
    public async Task ResolvePlayerSanctionAppeal_MissingSanction_Returns404()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        ResolveAppealRequest request = new() { Accepted = true, Resolution = "Does not matter" };

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"api/player-sanctions/{Guid.NewGuid()}/appeal/resolve", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> SeedSanctionIdAsync(SanctionAppealStatus status)
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext db = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        PlayerSanction sanction = await SeedSanctionAsync(db, status);
        return sanction.Id;
    }

    /// <summary>
    /// Uses a fresh scope/DbContext because the request pipeline persists through
    /// its own scoped DbContext; re-querying with a new one avoids reading a
    /// stale change-tracker snapshot instead of the actual persisted row.
    /// </summary>
    private async Task<SanctionAppealStatus> ReadAppealStatusAsync(Guid sanctionId)
    {
        using IServiceScope verifyScope = _factory.Services.CreateScope();
        ApplicationDBContext db = verifyScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        PlayerSanction sanction = await db.PlayerSanctions.AsNoTracking()
            .SingleAsync(s => s.Id == sanctionId);

        return sanction.AppealStatus;
    }

    /// <summary>
    /// Seeds the full object graph a PlayerSanction requires under SQLite's
    /// enforced FKs: Team→Player and Tournament→Division→Stage→Match, mirroring the object-
    /// graph depth of AutomatedMatchGenerationTests.SeedStageAsync.
    /// </summary>
    private static async Task<PlayerSanction> SeedSanctionAsync(
        ApplicationDBContext db, SanctionAppealStatus status)
    {
        DateTime startDate = DateTime.UtcNow.Date;
        DateTime endDate = startDate.AddDays(14);

        Guid divisionId = Guid.NewGuid();
        Guid stageId = Guid.NewGuid();
        Guid teamId = Guid.NewGuid();

        Tournament tournament = new()
        {
            Description = "Sanction appeal characterization tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Id = divisionId,
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            Stages = [],
            CreatedBy = "test",
        };

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Id = stageId,
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = startDate,
            EndDate = endDate,
            DivisionId = divisionId,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        Team team = new()
        {
            Id = teamId,
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = "SAT",
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Red",
            Players = [],
            CreatedBy = "test",
        };

        Player player = new()
        {
            Slug = $"player-{Guid.NewGuid()}",
            FirstName = "Sanctioned",
            LastName = "Player",
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = true,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = teamId,
            CreatedBy = "test",
        };

        Match match = new()
        {
            MatchDate = startDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            IsFinished = true,
            Stage = stage,
            StageId = stageId,
            CreatedBy = "test",
        };

        PlayerSanction sanction = new()
        {
            Duration = 1,
            IssuedDate = startDate,
            Description = "Sanction appeal characterization test",
            Slug = $"sanction-{Guid.NewGuid()}",
            Player = player,
            Match = match,
            AppealStatus = status,
            CreatedBy = "test",
        };

        if (status != SanctionAppealStatus.None)
        {
            sanction.AppealReason = "Prior appeal reason";
            sanction.AppealDate = startDate;
        }

        if (status is SanctionAppealStatus.Accepted or SanctionAppealStatus.Rejected)
        {
            sanction.AppealResolution = "Prior resolution notes";
            sanction.AppealResolvedDate = startDate;
        }

        db.Tournaments.Add(tournament);
        db.Divisions.Add(division);
        db.Stages.Add(stage);
        db.Teams.Add(team);
        db.Players.Add(player);
        db.Matches.Add(match);
        db.PlayerSanctions.Add(sanction);
        await db.SaveChangesAsync();

        return sanction;
    }
}
