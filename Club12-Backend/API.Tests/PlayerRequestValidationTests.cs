using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// A player's DocumentNumber must be digits only, and BirthDate must put them
/// at least 15 years old — both enforced as DataAnnotations on
/// CreatePlayerRequest/UpdatePlayerRequest, so [ApiController] rejects a bad
/// payload with 400 before any service code runs.
/// </summary>
public class PlayerRequestValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayerRequestValidationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Guid> SeedTeamAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);
        Tournament tournament = new()
        {
            Description = "Player validation test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Status = TournamentStatus.OpenForRegistration,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Green",
            TournamentId = tournament.Id,
            Players = [],
            CreatedBy = "test",
        };
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        return team.Id;
    }

    private static object ValidPayload(Guid teamId, object overrides) =>
        new
        {
            firstName = "Juan",
            lastName = "Pérez",
            documentNumber = $"30{Random.Shared.Next(100000, 999999)}",
            birthDate = DateTime.UtcNow.Date.AddYears(-20),
            phoneNumber = "3435551234",
            socialSecurity = "OSDE",
            teamId,
        }.Merge(overrides);

    [Fact]
    public async Task CreatePlayer_NonNumericDocumentNumber_ReturnsBadRequest()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Guid teamId = await SeedTeamAsync(db);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/players",
            ValidPayload(teamId, new { documentNumber = "d23" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlayer_YoungerThan15_ReturnsBadRequest()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Guid teamId = await SeedTeamAsync(db);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/players",
            ValidPayload(teamId, new { birthDate = DateTime.UtcNow.Date.AddYears(-10) }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlayer_ExactlyMinimumAge_IsAccepted()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Guid teamId = await SeedTeamAsync(db);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/players",
            ValidPayload(teamId, new { birthDate = DateTime.UtcNow.Date.AddYears(-15) }));

        string body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected 201, got {response.StatusCode}: {body}");
    }

    [Fact]
    public async Task CreatePlayer_ValidPayload_IsAccepted()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Guid teamId = await SeedTeamAsync(db);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/players",
            ValidPayload(teamId, new { }));

        string body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected 201, got {response.StatusCode}: {body}");
    }

    /// <summary>
    /// Found live while E2E-testing the tournament wizard: submitting a
    /// DocumentNumber that already belongs to another player hit the DB's
    /// unique index (IX_Players_DocumentNumber) with no pre-check in
    /// PlayerService.CreatePlayerAsync, so it bubbled up as an unhandled
    /// DbUpdateException — a raw 500 instead of a friendly conflict, and the
    /// frontend had no error-shaped response to show a toast for. Must be a
    /// clean 409 with a Spanish message, exactly like every other duplicate
    /// guard in this codebase (team code, division slug, etc).
    /// </summary>
    [Fact]
    public async Task CreatePlayer_DuplicateDocumentNumber_ReturnsConflictNotServerError()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Guid teamId = await SeedTeamAsync(db);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        string documentNumber = $"30{Random.Shared.Next(100000, 999999)}";

        HttpResponseMessage first = await client.PostAsJsonAsync(
            "api/players",
            ValidPayload(teamId, new { documentNumber }));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        HttpResponseMessage second = await client.PostAsJsonAsync(
            "api/players",
            ValidPayload(teamId, new { documentNumber }));

        string body = await second.Content.ReadAsStringAsync();
        Assert.True(
            second.StatusCode == HttpStatusCode.Conflict,
            $"Expected 409, got {second.StatusCode}: {body}");
    }
}

file static class ObjectMergeExtensions
{
    /// <summary>
    /// Merges an anonymous-object payload with overrides by re-projecting to
    /// a plain dictionary, keeping the test payload builders one-liners.
    /// </summary>
    public static object Merge(this object baseObject, object overrides)
    {
        Dictionary<string, object?> merged = baseObject.GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(baseObject));

        foreach (var prop in overrides.GetType().GetProperties())
        {
            merged[prop.Name] = prop.GetValue(overrides);
        }

        return merged;
    }
}
