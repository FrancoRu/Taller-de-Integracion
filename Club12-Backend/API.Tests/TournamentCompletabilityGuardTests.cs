using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// HU-109: the completability guard makes it impossible to START a tournament
/// (transition to Ongoing) in a state that could never be completed, and the
/// read-only GET .../completability endpoint previews the same issues. Service
/// paths are exercised directly; the query endpoint through a real HTTP round
/// trip (TournamentController has no SupabaseHelper dependency).
/// </summary>
public class TournamentCompletabilityGuardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentCompletabilityGuardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChangeStatusIntoOngoing_ZoneTooFewTeams_IsBlockedAndKeepsStatus()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);
        Division zone = await SeedDivisionAsync(db, tournament, crossCup: false);
        Stage group = await SeedGroupStageAsync(db, zone);
        Team team = await SeedTeamAsync(db, tournament.Id);
        await AssignAndEnrollAsync(db, tournament, group, team);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.Ongoing));

        Tournament reloaded = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.RegistrationClosed, reloaded.Status);
        Assert.Equal(0, await db.Matches.CountAsync(m => m.StageId == group.Id));
    }

    [Fact]
    public async Task ChangeStatusIntoOngoing_Completable_StartsAndGeneratesFixture()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);
        Division zone = await SeedDivisionAsync(db, tournament, crossCup: false);
        Stage group = await SeedGroupStageAsync(db, zone);
        for (int i = 0; i < 4; i++)
        {
            Team team = await SeedTeamAsync(db, tournament.Id);
            await AssignAndEnrollAsync(db, tournament, group, team);
            await SeedPlayersAsync(db, team, tournament, count: 5);
        }

        await tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.Ongoing);

        Tournament reloaded = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.Ongoing, reloaded.Status);
        // 4 teams, single round-robin => 4*3/2 = 6 matches.
        Assert.Equal(6, await db.Matches.CountAsync(m => m.StageId == group.Id));
    }

    [Fact]
    public async Task GetCompletability_InvalidTournament_ReturnsIssuesAndCannotStart()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);
            Division zone = await SeedDivisionAsync(db, tournament, crossCup: false);
            Stage group = await SeedGroupStageAsync(db, zone);
            Team team = await SeedTeamAsync(db, tournament.Id);
            await AssignAndEnrollAsync(db, tournament, group, team);
            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.GetAsync($"api/tournaments/{tournamentId}/completability");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        CompletabilityResponseDto? body = await response.Content.ReadFromJsonAsync<CompletabilityResponseDto>();
        Assert.NotNull(body);
        Assert.False(body!.CanStart);
        Assert.Contains(body.Issues, i => i.Code == "ZoneTooFewTeams");
    }

    [Fact]
    public async Task GetCompletability_ValidTournament_ReturnsCanStartWithNoIssues()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);
            Division zone = await SeedDivisionAsync(db, tournament, crossCup: false);
            Stage group = await SeedGroupStageAsync(db, zone);
            for (int i = 0; i < 2; i++)
            {
                Team team = await SeedTeamAsync(db, tournament.Id);
                await AssignAndEnrollAsync(db, tournament, group, team);
                await SeedPlayersAsync(db, team, tournament, count: 5);
            }
            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.GetAsync($"api/tournaments/{tournamentId}/completability");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        CompletabilityResponseDto? body = await response.Content.ReadFromJsonAsync<CompletabilityResponseDto>();
        Assert.NotNull(body);
        Assert.True(body!.CanStart);
        Assert.Empty(body.Issues);
    }

    [Fact]
    public async Task ChangeStatusIntoOngoing_TeamTooFewPlayers_IsBlockedAndKeepsStatus()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);
        Division zone = await SeedDivisionAsync(db, tournament, crossCup: false);
        Stage group = await SeedGroupStageAsync(db, zone);

        Team fullTeam = await SeedTeamAsync(db, tournament.Id);
        await AssignAndEnrollAsync(db, tournament, group, fullTeam);
        await SeedPlayersAsync(db, fullTeam, tournament, count: 5);

        Team shortTeam = await SeedTeamAsync(db, tournament.Id);
        await AssignAndEnrollAsync(db, tournament, group, shortTeam);
        await SeedPlayersAsync(db, shortTeam, tournament, count: 4);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.Ongoing));

        Tournament reloaded = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.RegistrationClosed, reloaded.Status);
        Assert.Equal(0, await db.Matches.CountAsync(m => m.StageId == group.Id));
    }

    [Fact]
    public async Task GetCompletability_TeamTooFewPlayers_ReturnsIssueWithPlayerCount()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);
            Division zone = await SeedDivisionAsync(db, tournament, crossCup: false);
            Stage group = await SeedGroupStageAsync(db, zone);

            Team fullTeam = await SeedTeamAsync(db, tournament.Id);
            await AssignAndEnrollAsync(db, tournament, group, fullTeam);
            await SeedPlayersAsync(db, fullTeam, tournament, count: 5);

            Team shortTeam = await SeedTeamAsync(db, tournament.Id);
            await AssignAndEnrollAsync(db, tournament, group, shortTeam);
            await SeedPlayersAsync(db, shortTeam, tournament, count: 3);

            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.GetAsync($"api/tournaments/{tournamentId}/completability");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        CompletabilityResponseDto? body = await response.Content.ReadFromJsonAsync<CompletabilityResponseDto>();
        Assert.NotNull(body);
        Assert.False(body!.CanStart);
        IssueDto issue = Assert.Single(body.Issues, i => i.Code == "TeamTooFewPlayers");
        Assert.Equal(3, issue.PlayerCount);
    }

    [Fact]
    public async Task ChangeStatusIntoOngoing_FailurePartwayThroughFixtureGeneration_RollsBackEveryDivision()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);

        // Division A: a perfectly valid, completable zone — generating its
        // fixture alone would succeed.
        Division zoneA = await SeedDivisionAsync(db, tournament, crossCup: false);
        Stage groupA = await SeedGroupStageAsync(db, zoneA);
        for (int i = 0; i < 4; i++)
        {
            Team team = await SeedTeamAsync(db, tournament.Id);
            await AssignAndEnrollAsync(db, tournament, groupA, team);
            await SeedPlayersAsync(db, team, tournament, count: 5);
        }

        // Division B: a completable group stage (so the HU-109 guard passes)
        // PLUS a RoundOf16 stage — a knockout type CreateAutomatedMatchesAsync
        // does not support. Fixture generation reaches this stage only after
        // successfully generating division B's own group matches, so this
        // also proves a same-division partial success rolls back too.
        Division zoneB = await SeedDivisionAsync(db, tournament, crossCup: false);
        Stage groupB = await SeedGroupStageAsync(db, zoneB);
        for (int i = 0; i < 2; i++)
        {
            Team team = await SeedTeamAsync(db, tournament.Id);
            await AssignAndEnrollAsync(db, tournament, groupB, team);
            await SeedPlayersAsync(db, team, tournament, count: 5);
        }
        await SeedRoundOf16StageAsync(db, zoneB);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.Ongoing));

        Tournament reloaded = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.RegistrationClosed, reloaded.Status);
        // Neither division's matches survive — the whole attempt rolls back
        // together: division A succeeded fully, division B's OWN group stage
        // even succeeded before its RoundOf16 stage threw, but none of it sticks.
        Assert.Equal(0, await db.Matches.CountAsync(m => m.StageId == groupA.Id));
        Assert.Equal(0, await db.Matches.CountAsync(m => m.StageId == groupB.Id));
    }

    [Fact]
    public async Task GetCompletability_UnknownTournament_ReturnsNotFound()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.GetAsync($"api/tournaments/{Guid.NewGuid()}/completability");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record CompletabilityResponseDto(bool CanStart, List<IssueDto> Issues);

    private sealed record IssueDto(string Code, string? DivisionName, string? TeamName, int? FromPosition, int? AssignedTeams, int? PlayerCount);

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, TournamentStatus status)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Completability test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = start.AddDays(-1),
            StartDate = start,
            Status = status,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        return tournament;
    }

    private static async Task<Division> SeedDivisionAsync(ApplicationDBContext db, Tournament tournament, bool crossCup)
    {
        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = crossCup ? $"CrossCup-{Guid.NewGuid()}" : $"Zone-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            IsCrossDivisionCup = crossCup,
            Stages = [],
            CreatedBy = "test",
        };

        db.Divisions.Add(division);
        await db.SaveChangesAsync();
        return division;
    }

    private static async Task<Stage> SeedGroupStageAsync(ApplicationDBContext db, Division division)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Group-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = start,
            EndDate = start.AddDays(14),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();
        return stage;
    }

    /// <summary>
    /// A knockout stage type CreateAutomatedMatchesAsync does not support —
    /// used to force a deterministic failure partway through fixture
    /// generation for the rollback test.
    /// </summary>
    private static async Task<Stage> SeedRoundOf16StageAsync(ApplicationDBContext db, Division division)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"RoundOf16-{Guid.NewGuid()}",
            StageType = StageType.RoundOf16,
            IsActive = true,
            IsElimination = true,
            StartDate = start,
            EndDate = start.AddDays(14),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();
        return stage;
    }

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, Guid tournamentId)
    {
        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Green",
            TournamentId = tournamentId,
            Players = [],
            CreatedBy = "test",
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team;
    }

    private static async Task AssignAndEnrollAsync(ApplicationDBContext db, Tournament tournament, Stage stage, Team team)
    {
        db.StageTeamMatches.Add(new StageTeamMatch
        {
            StageId = stage.Id,
            TeamId = team.Id,
            CreatedBy = "test",
        });

        db.TeamTournamentRegistrations.Add(new TeamTournamentRegistration
        {
            TeamId = team.Id,
            TournamentId = tournament.Id,
            CreatedBy = "test",
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedPlayersAsync(ApplicationDBContext db, Team team, Tournament tournament, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Player player = new()
            {
                FirstName = $"Player{i}",
                LastName = $"Test{Guid.NewGuid()}",
                Slug = $"player-{Guid.NewGuid()}",
                DocumentNumber = $"3{Random.Shared.Next(1000000, 9999999)}",
                IsSanctioned = false,
                BirthDate = DateTime.UtcNow.Date.AddYears(-20),
                SocialSecurity = "OSDE",
                Team = team,
                CreatedBy = "test",
            };
            db.Players.Add(player);
            await db.SaveChangesAsync();

            db.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
            {
                PlayerId = player.Id,
                TeamId = team.Id,
                TournamentId = tournament.Id,
                CreatedBy = "test",
            });
        }

        await db.SaveChangesAsync();
    }
}
