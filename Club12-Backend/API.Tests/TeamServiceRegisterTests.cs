using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Domain.Entities.Models;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

/// <summary>
/// Characterization tests for TeamService.RegisterTeamsToTournamentAsync
/// (TeamService.cs:115-129), which reconciles a tournament's team roster
/// against a submitted list of team IDs (add/unassign/keep/reassign). These
/// tests pin the currently-correct behavior of the existing, unmodified
/// service — pure test additions, no production code changes.
///
/// Mutations are verified by re-querying `ApplicationDBContext` in a fresh DI
/// scope (not the seeding scope's change tracker), matching the convention
/// used by PlayerSanctionAppealTests.ReadAppealStatusAsync.
/// </summary>
public class TeamServiceRegisterTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TeamServiceRegisterTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegisterTeamsToTournamentAsync_UnassignedTeam_IsRegistered()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = await SeedTournamentAsync(seedDb);
        Team team = await SeedTeamAsync(seedDb, tournamentId: null);

        await RegisterAsync(tournament, [team.Id]);

        Team? persisted = await ReadTeamAsync(team.Id);
        Assert.NotNull(persisted);
        Assert.Equal(tournament.Id, persisted!.TournamentId);
    }

    [Fact]
    public async Task RegisterTeamsToTournamentAsync_DroppedTeam_IsUnassigned()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = await SeedTournamentAsync(seedDb);
        Team team = await SeedTeamAsync(seedDb, tournamentId: tournament.Id);

        // team.Id deliberately absent from teamIds
        await RegisterAsync(tournament, []);

        Team? persisted = await ReadTeamAsync(team.Id);
        Assert.NotNull(persisted);
        Assert.Null(persisted!.TournamentId);
    }

    [Fact]
    public async Task RegisterTeamsToTournamentAsync_EmptyList_UnassignsAllCurrentMembers()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = await SeedTournamentAsync(seedDb);
        Team teamOne = await SeedTeamAsync(seedDb, tournamentId: tournament.Id);
        Team teamTwo = await SeedTeamAsync(seedDb, tournamentId: tournament.Id);

        await RegisterAsync(tournament, []);

        Team? persistedOne = await ReadTeamAsync(teamOne.Id);
        Team? persistedTwo = await ReadTeamAsync(teamTwo.Id);
        Assert.Null(persistedOne!.TournamentId);
        Assert.Null(persistedTwo!.TournamentId);
    }

    [Fact]
    public async Task RegisterTeamsToTournamentAsync_ExistingMember_StaysRegistered()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = await SeedTournamentAsync(seedDb);
        Team team = await SeedTeamAsync(seedDb, tournamentId: tournament.Id);

        await RegisterAsync(tournament, [team.Id]);

        Team? persisted = await ReadTeamAsync(team.Id);
        Assert.NotNull(persisted);
        Assert.Equal(tournament.Id, persisted!.TournamentId);
    }

    [Fact]
    public async Task RegisterTeamsToTournamentAsync_CrossTournamentTeam_IsReassigned()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament targetTournament = await SeedTournamentAsync(seedDb);
        Tournament otherTournament = await SeedTournamentAsync(seedDb);
        Team team = await SeedTeamAsync(seedDb, tournamentId: otherTournament.Id);

        await RegisterAsync(targetTournament, [team.Id]);

        Team? persisted = await ReadTeamAsync(team.Id);
        Assert.NotNull(persisted);
        Assert.Equal(targetTournament.Id, persisted!.TournamentId);
    }

    [Fact]
    public async Task RegisterTeamsToTournamentAsync_UninvolvedTeam_IsUntouched()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament targetTournament = await SeedTournamentAsync(seedDb);
        Tournament otherTournament = await SeedTournamentAsync(seedDb);
        Team uninvolvedTeam = await SeedTeamAsync(seedDb, tournamentId: otherTournament.Id);
        Team unrelatedTeam = await SeedTeamAsync(seedDb, tournamentId: null);

        // uninvolvedTeam.Id deliberately absent from teamIds, and its current
        // TournamentId is neither null nor targetTournament.Id.
        await RegisterAsync(targetTournament, [unrelatedTeam.Id]);

        Team? persisted = await ReadTeamAsync(uninvolvedTeam.Id);
        Assert.NotNull(persisted);
        Assert.Equal(otherTournament.Id, persisted!.TournamentId);
    }

    private async Task RegisterAsync(Tournament tournament, List<Guid> teamIds)
    {
        using IServiceScope actScope = _factory.Services.CreateScope();
        ITeamService teamService = actScope.ServiceProvider.GetRequiredService<ITeamService>();
        await teamService.RegisterTeamsToTournamentAsync(tournament, teamIds);
    }

    private async Task<Team?> ReadTeamAsync(Guid teamId)
    {
        using IServiceScope verifyScope = _factory.Services.CreateScope();
        ApplicationDBContext db = verifyScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        return await db.Teams.AsNoTracking().SingleOrDefaultAsync(t => t.Id == teamId);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Team registration characterization tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            MaxTeams = 16,
            MinTeams = 2,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, Guid? tournamentId)
    {
        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
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
}
