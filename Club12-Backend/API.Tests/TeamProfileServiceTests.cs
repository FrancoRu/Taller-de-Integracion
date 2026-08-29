using Application.DTOs.Team.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Exercises the three read-only team-profile aggregations added to
/// <see cref="ITeamService"/> — summary standing row, matches (home or
/// visitor), and tournament participations — against a real SQLite-backed host,
/// seeding focused fixtures per test (same style as DivisionStandingsTests).
/// The controller's 404 guard is covered by the unknown-team resolution test.
/// </summary>
public class TeamProfileServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TeamProfileServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTeamSummaryAsync_TeamInGroupStandings_ReturnsRankedRow()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 3);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage groupStage = await SeedGroupStageAsync(db, division, tournament);

        // A beats B and C; B beats C. Standings: A (2W, 4pts) > B (1W1L, 3pts) > C (0W2L, 2pts).
        await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[1], 90, 80);
        await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[2], 100, 70);
        await SeedFinishedMatchAsync(db, groupStage, teams[1], teams[2], 85, 84);

        TeamSummaryResponse? summary = await teamService.GetTeamSummaryAsync(teams[0].Id, tournament.Id);

        Assert.NotNull(summary);
        Assert.Equal(division.Id, summary!.DivisionId);
        Assert.Equal(division.Name, summary.DivisionName);
        Assert.Equal(1, summary.Position);
        Assert.Equal(3, summary.TotalTeams);
        Assert.Equal(2, summary.Played);
        Assert.Equal(2, summary.Wins);
        Assert.Equal(0, summary.Losses);
        Assert.Equal(190, summary.PointsFor);
        Assert.Equal(150, summary.PointsAgainst);
        Assert.Equal(40, summary.PointsDifference);
        Assert.Equal(4, summary.Points);

        // Sanity: the middle team ranks second in the same table.
        TeamSummaryResponse? middle = await teamService.GetTeamSummaryAsync(teams[1].Id, tournament.Id);
        Assert.NotNull(middle);
        Assert.Equal(2, middle!.Position);
    }

    [Fact]
    public async Task GetTeamSummaryAsync_TeamNotInAnyGroupStanding_ReturnsNull()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 3);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage groupStage = await SeedGroupStageAsync(db, division, tournament);

        // Only A and B play; C never appears in a finished group match.
        await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[1], 90, 80);

        TeamSummaryResponse? summary = await teamService.GetTeamSummaryAsync(teams[2].Id, tournament.Id);

        Assert.Null(summary);
    }

    [Fact]
    public async Task GetTeamMatchesAsync_OrdersByDate_AndDerivesResultAndOrientation()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 3);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage groupStage = await SeedGroupStageAsync(db, division, tournament);
        Venue venue = await SeedVenueAsync(db);

        DateTime day1 = tournament.StartDate.AddDays(1);
        DateTime day2 = tournament.StartDate.AddDays(2);
        DateTime day3 = tournament.StartDate.AddDays(3);

        // Seeded out of order to prove ordering: home win (day1), scheduled home (day2), away loss (day3).
        // day3: C hosts A and wins 70-65, so A (visitor) loses.
        await SeedFinishedMatchAsync(db, groupStage, teams[2], teams[0], 70, 65, day3, venue);
        await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[1], 90, 80, day1, venue);
        await SeedScheduledMatchAsync(db, groupStage, teams[0], teams[2], day2);

        List<TeamMatchResponse> matches = await teamService.GetTeamMatchesAsync(teams[0].Id, tournament.Id);

        Assert.Equal(3, matches.Count);
        Assert.Equal(day1, matches[0].MatchDate);
        Assert.Equal(day2, matches[1].MatchDate);
        Assert.Equal(day3, matches[2].MatchDate);

        // day1: A home win vs B.
        Assert.True(matches[0].IsHome);
        Assert.Equal(teams[1].Id, matches[0].OpponentTeamId);
        Assert.Equal(90, matches[0].TeamScore);
        Assert.Equal(80, matches[0].OpponentScore);
        Assert.Equal("W", matches[0].Result);
        Assert.Equal(venue.Name, matches[0].VenueName);

        // day2: scheduled, no result.
        Assert.False(matches[1].IsFinished);
        Assert.Null(matches[1].Result);
        Assert.Equal("Scheduled", matches[1].Status);

        // day3: A visitor loss vs C (home C 70, visitor A 65).
        Assert.False(matches[2].IsHome);
        Assert.Equal(teams[2].Id, matches[2].OpponentTeamId);
        Assert.Equal(65, matches[2].TeamScore);
        Assert.Equal(70, matches[2].OpponentScore);
        Assert.Equal("L", matches[2].Result);
    }

    [Fact]
    public async Task GetTeamMatchesAsync_NoMatches_ReturnsEmptyList()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 1);

        List<TeamMatchResponse> matches = await teamService.GetTeamMatchesAsync(teams[0].Id, tournament.Id);

        Assert.Empty(matches);
    }

    [Fact]
    public async Task GetTeamParticipationsAsync_ListsNewestFirst_WithSeasonInfoAndIsCurrent()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Season season2024 = await SeedSeasonAsync(db, "Temporada 2024", 2024);
        Season season2025 = await SeedSeasonAsync(db, "Temporada 2025", 2025);

        Tournament tournament2024 = await SeedTournamentAsync(db, season2024, TournamentCategory.Masculine);
        Tournament tournament2025 = await SeedTournamentAsync(db, season2025, TournamentCategory.Feminine);
        Tournament tournamentNoSeason = await SeedTournamentAsync(db);

        // The team's current pointer is the 2025 tournament.
        Team team = (await SeedTeamsAsync(db, tournament2025, 1))[0];

        await SeedRegistrationAsync(db, team, tournament2024);
        await SeedRegistrationAsync(db, team, tournament2025);
        await SeedRegistrationAsync(db, team, tournamentNoSeason);

        List<TeamParticipationResponse> participations =
            await teamService.GetTeamParticipationsAsync(team.Id, team.TournamentId);

        Assert.Equal(3, participations.Count);

        // Newest first: 2025, then 2024, then the season-less one last.
        Assert.Equal(tournament2025.Id, participations[0].TournamentId);
        Assert.True(participations[0].IsCurrent);
        Assert.Equal("Temporada 2025", participations[0].SeasonName);
        Assert.Equal(2025, participations[0].Year);
        Assert.Equal(season2025.Id, participations[0].SeasonId);
        Assert.Equal(nameof(TournamentCategory.Feminine), participations[0].Category);

        Assert.Equal(tournament2024.Id, participations[1].TournamentId);
        Assert.False(participations[1].IsCurrent);
        Assert.Equal(2024, participations[1].Year);

        Assert.Equal(tournamentNoSeason.Id, participations[2].TournamentId);
        Assert.Null(participations[2].Year);
        Assert.Null(participations[2].SeasonName);
        Assert.Null(participations[2].SeasonId);
    }

    [Fact]
    public async Task GetTeamByIdOrSlugAsync_UnknownTeam_ReturnsNull_ForControllerNotFound()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Assert.Null(await teamService.GetTeamByIdOrSlugAsync(Guid.NewGuid().ToString()));
        Assert.Null(await teamService.GetTeamByIdOrSlugAsync($"unknown-slug-{Guid.NewGuid()}"));
    }

    private static async Task<Tournament> SeedTournamentAsync(
        ApplicationDBContext db, Season? season = null, TournamentCategory category = TournamentCategory.Masculine)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Team profile test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Category = category,
            SeasonId = season?.Id,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }

    private static async Task<Season> SeedSeasonAsync(ApplicationDBContext db, string name, int year)
    {
        Season season = new()
        {
            Name = name,
            Slug = $"season-{Guid.NewGuid()}",
            Year = year,
            CreatedBy = "test",
        };

        db.Seasons.Add(season);
        await db.SaveChangesAsync();

        return season;
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
            EndDate = tournament.StartDate.AddDays(30),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
    }

    private static async Task<Venue> SeedVenueAsync(ApplicationDBContext db)
    {
        Venue venue = new()
        {
            Name = $"Venue-{Guid.NewGuid()}",
            Slug = $"venue-{Guid.NewGuid()}",
            Address = "123 Test Street",
            CreatedBy = "test",
        };

        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        return venue;
    }

    private static async Task SeedFinishedMatchAsync(
        ApplicationDBContext db, Stage stage, Team home, Team visitor, int homeScore, int visitorScore,
        DateTime? matchDate = null, Venue? venue = null)
    {
        Match match = new()
        {
            MatchDate = matchDate ?? stage.StartDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            HomeScore = homeScore,
            VisitorScore = visitorScore,
            IsFinished = true,
            Status = MatchStatus.Played,
            WinningTeam = homeScore > visitorScore ? home : visitor,
            WinningTeamId = homeScore > visitorScore ? home.Id : visitor.Id,
            Stage = stage,
            StageId = stage.Id,
            VenueId = venue?.Id,
            Venue = venue,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();
    }

    private static async Task SeedScheduledMatchAsync(
        ApplicationDBContext db, Stage stage, Team home, Team visitor, DateTime matchDate)
    {
        Match match = new()
        {
            MatchDate = matchDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            IsFinished = false,
            Status = MatchStatus.Scheduled,
            Stage = stage,
            StageId = stage.Id,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();
    }

    private static async Task SeedRegistrationAsync(ApplicationDBContext db, Team team, Tournament tournament)
    {
        TeamTournamentRegistration registration = new()
        {
            TeamId = team.Id,
            TournamentId = tournament.Id,
            CreatedBy = "test",
        };

        db.TeamTournamentRegistrations.Add(registration);
        await db.SaveChangesAsync();
    }
}
