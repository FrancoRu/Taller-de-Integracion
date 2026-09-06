using Application.DTOs.Match.Request;
using Application.DTOs.Match.Response;
using Application.Utils.Constants;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Proves the venue schedule conflict guard (already shipped on
/// <see cref="Application.Services.MatchService.HasVenueScheduleConflictAsync"/>
/// and wired into <c>UpdateMatchDate</c>) is uniformly enforced on the two
/// previously-unguarded write paths: <c>POST /api/matches</c> and
/// <c>PUT /api/matches/{id}/suspend</c>.
/// </summary>
public class MatchControllerVenueConflictTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MatchControllerVenueConflictTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateMatch_CollidingVenueAndTime_ReturnsBadRequestWithConflictMessage()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        (Stage stage, Venue venue, Team teamA, Team teamB, Team teamC, Team teamD) = await SeedFixtureAsync(db);
        DateTime existingDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stage, venue.Id, existingDate, teamA.Id, teamB.Id);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        CreateMatchRequest request = new()
        {
            MatchDate = existingDate.AddHours(1),
            HomeTeamId = teamC.Id,
            VisitorTeamId = teamD.Id,
            StageId = stage.Id,
            VenueId = venue.Id,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("api/matches", request);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(ErrorMessages.Match.VenueScheduleConflict, body);
    }

    /// <summary>
    /// A bare-string BadRequest body serializes as a raw JSON string, which the
    /// frontend's error handler cannot parse into a display message (it only reads a
    /// `detail`/`title` field off a JSON object) — silently falling back to a generic
    /// error instead of showing the real conflict reason. This proves the fix: the
    /// response is a proper ProblemDetails object with the message under `detail`.
    /// </summary>
    [Fact]
    public async Task CreateMatch_CollidingVenueAndTime_ReturnsProblemDetailsShapeWithDetailField()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        (Stage stage, Venue venue, Team teamA, Team teamB, Team teamC, Team teamD) = await SeedFixtureAsync(db);
        DateTime existingDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stage, venue.Id, existingDate, teamA.Id, teamB.Id);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        CreateMatchRequest request = new()
        {
            MatchDate = existingDate.AddHours(1),
            HomeTeamId = teamC.Id,
            VisitorTeamId = teamD.Id,
            StageId = stage.Id,
            VenueId = venue.Id,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("api/matches", request);
        ProblemDetailsBody? problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();

        Assert.NotNull(problem);
        Assert.Equal(ErrorMessages.Match.VenueScheduleConflict, problem!.Detail);
    }

    private sealed record ProblemDetailsBody(string? Title, string? Detail, int? Status);

    [Fact]
    public async Task CreateMatch_NullVenue_SameCollidingDate_Succeeds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        (Stage stage, Venue venue, Team teamA, Team teamB, Team teamC, Team teamD) = await SeedFixtureAsync(db);
        DateTime existingDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stage, venue.Id, existingDate, teamA.Id, teamB.Id);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        CreateMatchRequest request = new()
        {
            MatchDate = existingDate,
            HomeTeamId = teamC.Id,
            VisitorTeamId = teamD.Id,
            StageId = stage.Id,
            VenueId = null,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("api/matches", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateMatch_ExactlyTwoHoursAfterExistingMatch_SameVenue_Succeeds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        (Stage stage, Venue venue, Team teamA, Team teamB, Team teamC, Team teamD) = await SeedFixtureAsync(db);
        DateTime existingDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stage, venue.Id, existingDate, teamA.Id, teamB.Id);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        CreateMatchRequest request = new()
        {
            MatchDate = existingDate.AddHours(2),
            HomeTeamId = teamC.Id,
            VisitorTeamId = teamD.Id,
            StageId = stage.Id,
            VenueId = venue.Id,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("api/matches", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateMatch_SameTime_DifferentVenue_Succeeds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        (Stage stage, Venue venueA, Team teamA, Team teamB, Team teamC, Team teamD) = await SeedFixtureAsync(db);
        Venue venueB = await SeedVenueAsync(db);
        DateTime existingDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stage, venueA.Id, existingDate, teamA.Id, teamB.Id);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        CreateMatchRequest request = new()
        {
            MatchDate = existingDate,
            HomeTeamId = teamC.Id,
            VisitorTeamId = teamD.Id,
            StageId = stage.Id,
            VenueId = venueB.Id,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("api/matches", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SuspendMatch_CollidingNewDateAtItsOwnVenue_ReturnsBadRequestAndLeavesDateUnchanged()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        (Stage stage, Venue venue, Team teamA, Team teamB, Team teamC, Team teamD) = await SeedFixtureAsync(db);
        DateTime matchMDate = new(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc);
        DateTime matchNDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stage, venue.Id, matchMDate, teamA.Id, teamB.Id);
        Match matchN = await SeedMatchAsync(db, stage, venue.Id, matchNDate, teamC.Id, teamD.Id);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        SuspendMatchRequest request = new() { MatchDate = matchMDate.AddHours(1) };

        HttpResponseMessage response = await client.PutAsJsonAsync($"api/matches/{matchN.Id}/suspend", request);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(ErrorMessages.Match.VenueScheduleConflict, body);

        Match? reloaded = await db.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchN.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(matchNDate, reloaded!.MatchDate);
    }

    [Fact]
    public async Task SuspendMatch_NonCollidingNewDate_Succeeds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        (Stage stage, Venue venue, Team teamA, Team teamB, Team teamC, Team teamD) = await SeedFixtureAsync(db);
        DateTime matchMDate = new(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc);
        DateTime matchNDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stage, venue.Id, matchMDate, teamA.Id, teamB.Id);
        Match matchN = await SeedMatchAsync(db, stage, venue.Id, matchNDate, teamC.Id, teamD.Id);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        DateTime newDate = matchNDate.AddDays(1);
        SuspendMatchRequest request = new() { MatchDate = newDate };

        HttpResponseMessage response = await client.PutAsJsonAsync($"api/matches/{matchN.Id}/suspend", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Match? reloaded = await db.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchN.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(newDate, reloaded!.MatchDate);
    }

    [Fact]
    public async Task SuspendMatch_NoVenue_SucceedsRegardlessOfDate()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        (Stage stage, Venue venue, Team teamA, Team teamB, Team teamC, Team teamD) = await SeedFixtureAsync(db);
        DateTime matchMDate = new(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc);
        DateTime matchNDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stage, venue.Id, matchMDate, teamA.Id, teamB.Id);
        Match matchN = await SeedMatchAsync(db, stage, venueId: null, matchNDate, teamC.Id, teamD.Id);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        SuspendMatchRequest request = new() { MatchDate = matchMDate.AddMinutes(30) };

        HttpResponseMessage response = await client.PutAsJsonAsync($"api/matches/{matchN.Id}/suspend", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<Venue> SeedVenueAsync(ApplicationDBContext db)
    {
        Venue venue = new()
        {
            Name = $"Venue-{Guid.NewGuid()}",
            Slug = $"venue-{Guid.NewGuid()}",
            Address = "Test Address 123",
            CreatedBy = "test",
        };

        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        return venue;
    }

    private static async Task<(Stage stage, Venue venue, Team teamA, Team teamB, Team teamC, Team teamD)> SeedFixtureAsync(ApplicationDBContext db)
    {
        DateTime start = DateTime.UtcNow.Date;

        Tournament tournament = new()
        {
            Description = "Venue conflict controller test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = start.AddDays(-1),
            StartDate = start,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            Stages = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = start,
            EndDate = start.AddDays(60),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);

        List<Team> teams = [.. Enumerable.Range(0, 4).Select(i => new Team
        {
            Name = $"Team-{i}-{Guid.NewGuid()}",
            Slug = $"team-{i}-{Guid.NewGuid()}",
            ThreeLetterCode = $"T{i:D2}",
            LogoUrl = "https://example.com/logo.png",
            ShirtColor = "Red",
            TournamentId = tournament.Id,
            Players = [],
            CreatedBy = "test",
        })];

        db.Teams.AddRange(teams);
        await db.SaveChangesAsync();

        Venue venue = await SeedVenueAsync(db);

        return (stage, venue, teams[0], teams[1], teams[2], teams[3]);
    }

    private static async Task<Match> SeedMatchAsync(
        ApplicationDBContext db, Stage stage, Guid? venueId, DateTime matchDate, Guid homeTeamId, Guid visitorTeamId)
    {
        Match match = new()
        {
            Slug = $"match-{Guid.NewGuid()}",
            Type = MatchType.Regular,
            IsFinished = false,
            StageId = stage.Id,
            VenueId = venueId,
            MatchDate = matchDate,
            HomeTeamId = homeTeamId,
            VisitorTeamId = visitorTeamId,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return match;
    }
}
