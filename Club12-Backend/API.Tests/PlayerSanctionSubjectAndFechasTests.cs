using Application.DTOs.PlayerSanction.Request;
using Application.DTOs.PlayerSanction.Response;
using Application.Interfaces.Services;

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
/// Tests for the sanction refinements (HU-75 fechas-based duration, HU-76
/// auto-expiry / eligibility, HU-77 team &amp; staff subjects). Fechas-remaining
/// and eligibility are exercised at the service layer for determinism; subject
/// creation and the public response shape go through a real HTTP round trip.
/// </summary>
public class PlayerSanctionSubjectAndFechasTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly DateTime Anchor = new(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Mirrors the API's JSON settings (enums serialized as strings) so the
    // typed response can be read back.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly CustomWebApplicationFactory _factory;

    public PlayerSanctionSubjectAndFechasTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------- HU-75

    [Fact]
    public async Task GetFechasRemainingAsync_DecrementsAsTeamPlaysRounds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService service = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        // Duration 3 fechas, issued in round 1. No later rounds finished yet.
        SanctionSeed seed = await SeedPlayerSanctionAsync(db, durationFechas: 3, issuedRound: 1);

        PlayerSanction sanction = await ReloadWithPlayerAsync(seed.SanctionId);
        Assert.Equal(3, await service.GetFechasRemainingAsync(sanction));

        // Team plays and finishes round 2 -> one fecha served -> 2 remaining.
        await AddFinishedMatchAsync(db, seed.StageId, seed.TeamId, round: 2);
        Assert.Equal(2, await service.GetFechasRemainingAsync(sanction));

        // Team plays and finishes round 3 -> two served -> 1 remaining.
        await AddFinishedMatchAsync(db, seed.StageId, seed.TeamId, round: 3);
        Assert.Equal(1, await service.GetFechasRemainingAsync(sanction));
    }

    [Fact]
    public async Task GetFechasRemainingAsync_ClampsToZero_WhenAllFechasServed()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService service = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        SanctionSeed seed = await SeedPlayerSanctionAsync(db, durationFechas: 2, issuedRound: 1);

        await AddFinishedMatchAsync(db, seed.StageId, seed.TeamId, round: 2);
        await AddFinishedMatchAsync(db, seed.StageId, seed.TeamId, round: 3);
        await AddFinishedMatchAsync(db, seed.StageId, seed.TeamId, round: 4);

        PlayerSanction sanction = await ReloadWithPlayerAsync(seed.SanctionId);

        // 3 rounds played but duration is 2 -> clamped to 0, never negative.
        Assert.Equal(0, await service.GetFechasRemainingAsync(sanction));
    }

    [Fact]
    public async Task GetFechasRemainingAsync_IgnoresUnfinishedAndLowerRounds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService service = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        SanctionSeed seed = await SeedPlayerSanctionAsync(db, durationFechas: 3, issuedRound: 2);

        // Round 1 (before the sanction) is finished but must NOT count.
        await AddFinishedMatchAsync(db, seed.StageId, seed.TeamId, round: 1);
        // Round 3 scheduled but NOT finished -> must NOT count.
        await AddMatchAsync(db, seed.StageId, seed.TeamId, round: 3, isFinished: false);

        PlayerSanction sanction = await ReloadWithPlayerAsync(seed.SanctionId);
        Assert.Equal(3, await service.GetFechasRemainingAsync(sanction));
    }

    // ---------------------------------------------------------------- HU-76

    [Fact]
    public async Task HasActiveSanctionAsync_TrueWhileFechasRemain_FalseOnceServed()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService service = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        SanctionSeed seed = await SeedPlayerSanctionAsync(db, durationFechas: 1, issuedRound: 1);

        // A player with fechas remaining is ineligible (active sanction).
        Assert.True(await service.HasActiveSanctionAsync(seed.PlayerId));

        // Team finishes round 2 -> the single fecha is served -> sanction
        // auto-expires and the player becomes eligible again.
        await AddFinishedMatchAsync(db, seed.StageId, seed.TeamId, round: 2);
        Assert.False(await service.HasActiveSanctionAsync(seed.PlayerId));
    }

    // ---------------------------------------------------------------- HU-77

    [Fact]
    public async Task CreateTeamSanction_IsPersisted_AndReturnedWithTeamSubject()
    {
        SanctionSeed seed;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            seed = await SeedPlayerSanctionAsync(db, durationFechas: 2, issuedRound: 1);
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        CreatePlayerSanctionRequest request = new()
        {
            Duration = 2,
            IssuedDate = Anchor,
            Description = "Institutional sanction against the team.",
            SubjectType = SanctionSubjectType.Team,
            TeamId = seed.TeamId,
            MatchId = seed.MatchId,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("api/player-sanctions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        PlayerSanctionResponse? created = await response.Content.ReadFromJsonAsync<PlayerSanctionResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(SanctionSubjectType.Team, created!.SubjectType);
        Assert.Equal(seed.TeamId, created.TeamId);
        Assert.Null(created.PlayerId);
    }

    [Fact]
    public async Task CreateStaffSanction_IsPersisted_AndReturnedWithStaffSubject()
    {
        SanctionSeed seed;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            seed = await SeedPlayerSanctionAsync(db, durationFechas: 1, issuedRound: 1);
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        CreatePlayerSanctionRequest request = new()
        {
            Duration = 1,
            IssuedDate = Anchor,
            Description = "Sanction against a staff member.",
            SubjectType = SanctionSubjectType.Staff,
            StaffName = "Coach Pep Guardiola",
            MatchId = seed.MatchId,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("api/player-sanctions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        PlayerSanctionResponse? created = await response.Content.ReadFromJsonAsync<PlayerSanctionResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(SanctionSubjectType.Staff, created!.SubjectType);
        Assert.Equal("Coach Pep Guardiola", created.StaffName);
        Assert.Null(created.PlayerId);
        Assert.Null(created.TeamId);
    }

    [Fact]
    public async Task CreateTeamSanction_WithoutTeamId_IsRejected()
    {
        SanctionSeed seed;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            seed = await SeedPlayerSanctionAsync(db, durationFechas: 1, issuedRound: 1);
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        CreatePlayerSanctionRequest request = new()
        {
            Duration = 1,
            IssuedDate = Anchor,
            Description = "Team sanction missing its team.",
            SubjectType = SanctionSubjectType.Team,
            MatchId = seed.MatchId,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("api/player-sanctions", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlayerSanction_StillWorks_AndReturnsPlayerSubject()
    {
        SanctionSeed seed;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            seed = await SeedPlayerSanctionAsync(db, durationFechas: 2, issuedRound: 1);
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        // Backward-compatible payload: no SubjectType, only a PlayerId.
        CreatePlayerSanctionRequest request = new()
        {
            Duration = 2,
            IssuedDate = Anchor,
            Description = "Ordinary player sanction.",
            PlayerId = seed.PlayerId,
            MatchId = seed.MatchId,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("api/player-sanctions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        PlayerSanctionResponse? created = await response.Content.ReadFromJsonAsync<PlayerSanctionResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(SanctionSubjectType.Player, created!.SubjectType);
        Assert.Equal(seed.PlayerId, created.PlayerId);
        Assert.NotNull(created.PlayerFullName);
        Assert.Null(created.TeamId);
        // Duration 2 with no later finished rounds -> 2 fechas remaining, active.
        Assert.Equal(2, created.FechasRemaining);
        Assert.True(created.IsActive);
    }

    // --------------------------------------------------------------- helpers

    private sealed record SanctionSeed(
        Guid SanctionId, Guid PlayerId, Guid TeamId, Guid StageId, Guid MatchId);

    private async Task<PlayerSanction> ReloadWithPlayerAsync(Guid sanctionId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        return await db.PlayerSanctions
            .Include(s => s.Player)
            .AsNoTracking()
            .SingleAsync(s => s.Id == sanctionId);
    }

    /// <summary>
    /// Seeds Tournament -> Division -> Stage -> Match plus Team -> Player and a
    /// player sanction issued in <paramref name="issuedRound"/>. Uses a base
    /// date far from other tests' data; fechas are scoped by the stage's own
    /// rounds so different tests never interfere.
    /// </summary>
    private static async Task<SanctionSeed> SeedPlayerSanctionAsync(
        ApplicationDBContext db, int durationFechas, int issuedRound)
    {
        DateTime issuedDate = Anchor.AddDays(Random.Shared.Next(0, 100_000));

        Guid divisionId = Guid.NewGuid();
        Guid stageId = Guid.NewGuid();
        Guid teamId = Guid.NewGuid();

        Tournament tournament = new()
        {
            Description = "Fechas/subject test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = issuedDate.AddDays(-1),
            StartDate = issuedDate,
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
            StartDate = issuedDate,
            EndDate = issuedDate.AddDays(60),
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
            ThreeLetterCode = "FCH",
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            Players = [],
            CreatedBy = "test",
        };

        Player player = new()
        {
            Slug = $"player-{Guid.NewGuid()}",
            FirstName = "Fechas",
            LastName = "Tester",
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = true,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = teamId,
            CreatedBy = "test",
        };

        Match sanctionMatch = new()
        {
            MatchDate = issuedDate,
            Type = MatchType.Regular,
            Round = issuedRound,
            Slug = $"match-{Guid.NewGuid()}",
            IsFinished = true,
            HomeTeamId = teamId,
            Stage = stage,
            StageId = stageId,
            CreatedBy = "test",
        };

        PlayerSanction sanction = new()
        {
            Duration = durationFechas,
            IssuedDate = issuedDate,
            Description = "Fechas-based sanction",
            Slug = $"sanction-{Guid.NewGuid()}",
            SubjectType = SanctionSubjectType.Player,
            Player = player,
            PlayerId = player.Id,
            Match = sanctionMatch,
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Divisions.Add(division);
        db.Stages.Add(stage);
        db.Teams.Add(team);
        db.Players.Add(player);
        db.Matches.Add(sanctionMatch);
        db.PlayerSanctions.Add(sanction);
        await db.SaveChangesAsync();

        return new SanctionSeed(sanction.Id, player.Id, teamId, stageId, sanctionMatch.Id);
    }

    private static Task AddFinishedMatchAsync(
        ApplicationDBContext db, Guid stageId, Guid teamId, int round)
        => AddMatchAsync(db, stageId, teamId, round, isFinished: true);

    private static async Task AddMatchAsync(
        ApplicationDBContext db, Guid stageId, Guid teamId, int round, bool isFinished)
    {
        Match match = new()
        {
            MatchDate = Anchor.AddDays(round),
            Type = MatchType.Regular,
            Round = round,
            Slug = $"match-{Guid.NewGuid()}",
            IsFinished = isFinished,
            HomeTeamId = teamId,
            StageId = stageId,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();
    }
}
