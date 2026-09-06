using Application.DTOs.Stage.Request;

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
/// Proves StageController's preview-draw/draw HTTP routes: authorization
/// gating (mirroring SeasonControllerAuthorizationTests) and a full
/// preview-token-to-commit round trip through the real pipeline. The
/// underlying seeding rules are already covered service-side by
/// PlayoffDrawTests.
/// </summary>
public class StageControllerDrawTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StageControllerDrawTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PreviewDraw_Anonymous_ReturnsUnauthorized()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Stage stage = await SeedSemiFinalStageAsync(db);

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/stages/{stage.Id}/preview-draw", new DrawRequest { Mode = DrawMode.Random });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PreviewDraw_GuestRole_ReturnsForbidden()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Stage stage = await SeedSemiFinalStageAsync(db);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Guest);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/stages/{stage.Id}/preview-draw", new DrawRequest { Mode = DrawMode.Random });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PreviewDraw_UnknownStage_ReturnsNotFound()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/stages/{Guid.NewGuid()}/preview-draw", new DrawRequest { Mode = DrawMode.Random });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PreviewThenCommitDraw_StaffRole_SeedsMatchesFromPreviewToken()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Stage stage = await SeedSemiFinalStageAsync(db);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage previewResponse = await client.PostAsJsonAsync(
            $"api/stages/{stage.Id}/preview-draw", new DrawRequest { Mode = DrawMode.Random });
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        DrawPreviewBody? preview = await previewResponse.Content.ReadFromJsonAsync<DrawPreviewBody>();
        Assert.NotNull(preview);
        Assert.False(string.IsNullOrWhiteSpace(preview!.DrawToken));

        HttpResponseMessage commitResponse = await client.PostAsJsonAsync(
            $"api/stages/{stage.Id}/draw",
            new DrawRequest { Mode = DrawMode.Random, DrawToken = preview.DrawToken });

        Assert.Equal(HttpStatusCode.OK, commitResponse.StatusCode);
        List<MatchBody>? matches = await commitResponse.Content.ReadFromJsonAsync<List<MatchBody>>();
        Assert.NotNull(matches);
        Assert.Equal(2, matches!.Count);

        List<Match> persistedMatches = await db.Matches.AsNoTracking()
            .Where(m => m.StageId == stage.Id).ToListAsync();
        Assert.All(persistedMatches, m => Assert.True(m.HomeTeamId.HasValue));

        Stage? reloaded = await db.Stages.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stage.Id);
        Assert.NotNull(reloaded!.DrawnAt);
    }

    [Fact]
    public async Task PreviewThenCommitDraw_BestOfGreaterThanOne_SeedsMatchesThroughFullHttpPipeline()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Stage stage = await SeedSemiFinalStageAsync(db, bestOf: 3);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage previewResponse = await client.PostAsJsonAsync(
            $"api/stages/{stage.Id}/preview-draw", new DrawRequest { Mode = DrawMode.Random });
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        DrawPreviewBody? preview = await previewResponse.Content.ReadFromJsonAsync<DrawPreviewBody>();
        Assert.NotNull(preview);

        HttpResponseMessage commitResponse = await client.PostAsJsonAsync(
            $"api/stages/{stage.Id}/draw",
            new DrawRequest { Mode = DrawMode.Random, DrawToken = preview!.DrawToken });

        string body = await commitResponse.Content.ReadAsStringAsync();
        Assert.True(
            commitResponse.StatusCode == HttpStatusCode.OK,
            $"Expected 200 but got {commitResponse.StatusCode}: {body}");
    }

    [Fact]
    public async Task PreviewThenCommitDraw_StageHasNoPreGeneratedMatches_SucceedsThroughFullHttpPipeline()
    {
        // Matches placeholders normally come from GenerateFixtureAsync when the tournament
        // starts (TournamentService.ChangeStatusAsync), but a playoffs-only division's bracket
        // has no group phase to trigger that, and the draw UI is reachable before then — so a
        // real first-ever draw hits a stage with zero matches. This is the exact shape a live
        // QA pass hit as a 500 on /api/stages/{id}/draw.
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Stage stage = await SeedSemiFinalStageAsync(db, seedMatches: false);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage previewResponse = await client.PostAsJsonAsync(
            $"api/stages/{stage.Id}/preview-draw", new DrawRequest { Mode = DrawMode.Random });
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        DrawPreviewBody? preview = await previewResponse.Content.ReadFromJsonAsync<DrawPreviewBody>();
        Assert.NotNull(preview);

        HttpResponseMessage commitResponse = await client.PostAsJsonAsync(
            $"api/stages/{stage.Id}/draw",
            new DrawRequest { Mode = DrawMode.Random, DrawToken = preview!.DrawToken });

        string body = await commitResponse.Content.ReadAsStringAsync();
        Assert.True(
            commitResponse.StatusCode == HttpStatusCode.OK,
            $"Expected 200 but got {commitResponse.StatusCode}: {body}");

        List<MatchBody>? matches = await commitResponse.Content.ReadFromJsonAsync<List<MatchBody>>();
        Assert.NotNull(matches);
        Assert.Equal(2, matches!.Count);
    }

    [Fact]
    public async Task CommitDraw_StaleTokenAfterAnotherDraw_ReturnsConflict()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Stage stage = await SeedSemiFinalStageAsync(db);

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/stages/{stage.Id}/draw",
            new DrawRequest { Mode = DrawMode.Random, DrawToken = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<Stage> SeedSemiFinalStageAsync(ApplicationDBContext db, int bestOf = 1, bool seedMatches = true)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Stage controller draw test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            IsCrossDivisionCup = false,
            Stages = [],
            CreatedBy = "test",
        };
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        for (int i = 0; i < 4; i++)
        {
            Team team = new()
            {
                Name = $"Team-{Guid.NewGuid()}",
                Slug = $"team-{Guid.NewGuid()}",
                ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
                LogoUrl = "https://example.test/logo.png",
                ShirtColor = "Blue",
                TournamentId = tournament.Id,
                Players = [],
                CreatedBy = "test",
            };
            db.Teams.Add(team);
            await db.SaveChangesAsync();

            db.DivisionTeamRegistrations.Add(new DivisionTeamRegistration
            {
                TeamId = team.Id,
                DivisionId = division.Id,
                CreatedBy = "test",
            });
        }

        await db.SaveChangesAsync();

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.SemiFinal,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(7),
            DivisionId = division.Id,
            Division = division,
            BracketName = "Copa Única",
            BestOf = bestOf,
            Matches = [],
            CreatedBy = "test",
        };
        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        if (seedMatches)
        {
            for (int i = 0; i < 2; i++)
            {
                db.Matches.Add(new Match
                {
                    StageId = stage.Id,
                    Type = MatchType.Playoff,
                    Slug = $"match-{Guid.NewGuid()}",
                    MatchDate = stage.StartDate.AddMinutes(i),
                    IsFinished = false,
                    CreatedBy = "test",
                });
            }

            await db.SaveChangesAsync();
        }

        return stage;
    }

    private sealed record DrawPreviewBody(string DrawToken);

    private sealed record MatchBody(Guid Id);
}
