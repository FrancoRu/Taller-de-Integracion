using Application.Interfaces.Services;
using Application.Utils.Helper.Standings;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Verifies IDivisionService.GetGroupStandingsByDivisionIdAsync returns one
/// standings table PER Group stage — the shape a multi-group cross-division
/// cup (HU-110) needs so the public tournament view can render one POSICIONES
/// table per internal group ("Grupo 1".."Grupo N") instead of a single
/// aggregate. A regular zone (one Group stage) still yields exactly one table.
/// </summary>
public class DivisionGroupStandingsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DivisionGroupStandingsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetGroupStandingsByDivisionIdAsync_MultipleGroupStages_ReturnsOneTablePerGroup()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);

        List<Team> group1Teams = await SeedTeamsAsync(db, tournament, 2);
        List<Team> group2Teams = await SeedTeamsAsync(db, tournament, 2);

        Stage group1 = await SeedGroupStageAsync(db, division, tournament, "Grupo 1", order: 0);
        Stage group2 = await SeedGroupStageAsync(db, division, tournament, "Grupo 2", order: 1);

        await SeedFinishedMatchAsync(db, group1, group1Teams[0], group1Teams[1], 90, 80);
        await SeedFinishedMatchAsync(db, group2, group2Teams[0], group2Teams[1], 70, 65);

        List<GroupStandings> groups = await divisionService.GetGroupStandingsByDivisionIdAsync(division.Id);

        Assert.Equal(2, groups.Count);

        GroupStandings first = groups[0];
        Assert.Equal(group1.Id, first.StageId);
        Assert.Equal("Grupo 1", first.StageName);
        Assert.Equal(2, first.Positions.Count);
        Assert.All(first.Positions, p =>
            Assert.Contains(p.TeamId, group1Teams.Select(t => t.Id)));

        GroupStandings second = groups[1];
        Assert.Equal(group2.Id, second.StageId);
        Assert.Equal("Grupo 2", second.StageName);
        Assert.Equal(2, second.Positions.Count);
        Assert.All(second.Positions, p =>
            Assert.Contains(p.TeamId, group2Teams.Select(t => t.Id)));

        // Distinct team count across ALL group stages is 4, not one group's 2.
        int distinctTeams = groups.SelectMany(g => g.Positions).Select(p => p.TeamId).Distinct().Count();
        Assert.Equal(4, distinctTeams);
    }

    [Fact]
    public async Task GetGroupStandingsByDivisionIdAsync_SingleGroupStage_ReturnsOneTable()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
        Stage groupStage = await SeedGroupStageAsync(db, division, tournament, "Fase de Grupos", order: 0);

        await SeedFinishedMatchAsync(db, groupStage, teams[0], teams[1], 90, 80);

        List<GroupStandings> groups = await divisionService.GetGroupStandingsByDivisionIdAsync(division.Id);

        GroupStandings single = Assert.Single(groups);
        Assert.Equal(groupStage.Id, single.StageId);
        Assert.Equal(2, single.Positions.Count);
    }

    [Fact]
    public async Task GetGroupStandingsByDivisionIdAsync_NoGroupStage_ReturnsEmptyList()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);

        List<GroupStandings> groups = await divisionService.GetGroupStandingsByDivisionIdAsync(division.Id);

        Assert.Empty(groups);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Group standings test tournament",
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

    private static async Task<Stage> SeedGroupStageAsync(
        ApplicationDBContext db, Division division, Tournament tournament, string name, int order)
    {
        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = name,
            StageType = StageType.Group,
            IsActive = true,
            IsElimination = false,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(7),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            Order = order,
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
