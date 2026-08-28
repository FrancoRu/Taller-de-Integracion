using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Verifies the divisions LIST endpoint (GET /api/divisions) populates each
/// division's Positions, the field the admin divisions table reads for its
/// per-division team counter — HU-32/HU-33. The single-division detail
/// endpoint already filled Positions; the list endpoint did not, so the
/// counter always rendered 0 (and any "no hay equipos" gate driven by it
/// stayed off) even for a fully-populated division.
/// </summary>
public class DivisionListPositionsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DivisionListPositionsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetFilteredDivisions_PopulatesPositions_ForDivisionWithFinishedGroupMatches()
    {
        Guid tournamentId;
        Guid divisionId;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

            Tournament tournament = await SeedTournamentAsync(db);
            List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
            Division division = await SeedDivisionAsync(db, tournament);
            Stage groupStage = await SeedGroupStageAsync(db, division, tournament);
            await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[1], homeScore: 90, visitorScore: 80);

            tournamentId = tournament.Id;
            divisionId = division.Id;
        }

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync($"api/divisions?tournamentId={tournamentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginatedDivisionsDto? body = await response.Content.ReadFromJsonAsync<PaginatedDivisionsDto>();
        Assert.NotNull(body);

        DivisionDto seededDivision = Assert.Single(body!.Items, d => d.Id == divisionId);
        Assert.NotNull(seededDivision.Positions);
        Assert.Equal(2, seededDivision.Positions!.Count);
    }

    private sealed record PositionDto(Guid TeamId);
    private sealed record DivisionDto(Guid Id, List<PositionDto>? Positions);
    private sealed record PaginatedDivisionsDto(List<DivisionDto> Items);

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Division list positions test tournament",
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

        return tournament;
    }

    private static async Task<List<Team>> SeedTeamsAsync(ApplicationDBContext db, Tournament tournament, int count)
    {
        List<Team> teams = [];

        for (int i = 0; i < count; i++)
        {
            Team team = new()
            {
                Name = $"Team-{Guid.NewGuid()}",
                Slug = $"team-{Guid.NewGuid()}",
                ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
                LogoUrl = "http://example.com/logo.png",
                ShirtColor = "Red",
                TournamentId = tournament.Id,
                Players = [],
                CreatedBy = "test",
            };

            db.Teams.Add(team);
            teams.Add(team);
        }

        await db.SaveChangesAsync();

        return teams;
    }

    private static async Task<Division> SeedDivisionAsync(ApplicationDBContext db, Tournament tournament)
    {
        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        };

        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        return division;
    }

    private static async Task<Stage> SeedGroupStageAsync(ApplicationDBContext db, Division division, Tournament tournament)
    {
        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Group-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            IsElimination = false,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(7),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
    }

    private static async Task SeedFinishedMatchAsync(
        ApplicationDBContext db, Stage stage, Team home, Team visitor, int homeScore, int visitorScore)
    {
        Match match = new()
        {
            MatchDate = stage.StartDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            HomeScore = homeScore,
            VisitorScore = visitorScore,
            IsFinished = true,
            WinningTeam = homeScore > visitorScore ? home : visitor,
            WinningTeamId = homeScore > visitorScore ? home.Id : visitor.Id,
            Stage = stage,
            StageId = stage.Id,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();
    }
}
