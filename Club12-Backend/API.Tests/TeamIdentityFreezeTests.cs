using API.Controllers;

using Application.DTOs.Team.Request;
using Application.Interfaces.Services;
using Application.Utils.Constants;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// A team's identity (<see cref="Team.Name"/> and
/// <see cref="Team.ThreeLetterCode"/>) is frozen while the team participates in
/// a tournament that is Ongoing (en curso). The PUT api/teams/{id} path rejects
/// a Name/ThreeLetterCode change with an <see cref="InvalidOperationException"/>
/// (mapped to 409 by the global handler) in that window; other fields (colors,
/// jersey style, logo) stay editable. Once the team's current tournament is not
/// Ongoing, the identity is editable again.
///
/// Exercised as direct-controller tests with the REAL <see cref="ITeamService"/>
/// and <see cref="IMapper"/> from the test host, passing
/// <see cref="Infrastructure.Storage.SupabaseHelper"/> as null! — the PUT branch
/// never touches it, and the full HTTP host cannot construct that helper without
/// live Supabase network access (same pre-existing testability gap documented in
/// <see cref="SupabaseDependentControllerNotFoundTests"/>).
/// </summary>
public class TeamIdentityFreezeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TeamIdentityFreezeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateTeam_ChangeName_WhileTournamentOngoing_IsRejectedAndKeepsName()
    {
        const string originalName = "Original Name";
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();
        IMapper mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Team team = await SeedTeamAsync(db, tournament.Id, originalName, "ORI");

        TeamController controller = new(teamService, null!, mapper);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.UpdateTeam(team.Id, new UpdateTeamRequest { Name = "Changed Name", ThreeLetterCode = "ORI" }));
        Assert.Equal(ErrorMessages.Team.IdentityFrozenWhileOngoing, ex.Message);

        Team reloaded = await db.Teams.AsNoTracking().SingleAsync(t => t.Id == team.Id);
        Assert.Equal(originalName, reloaded.Name);
    }

    [Fact]
    public async Task UpdateTeam_ChangeThreeLetterCode_WhileTournamentOngoing_IsRejectedAndKeepsCode()
    {
        const string originalCode = "ORI";
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();
        IMapper mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Team team = await SeedTeamAsync(db, tournament.Id, "Some Name", originalCode);

        TeamController controller = new(teamService, null!, mapper);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.UpdateTeam(team.Id, new UpdateTeamRequest { Name = "Some Name", ThreeLetterCode = "NEW" }));
        Assert.Equal(ErrorMessages.Team.IdentityFrozenWhileOngoing, ex.Message);

        Team reloaded = await db.Teams.AsNoTracking().SingleAsync(t => t.Id == team.Id);
        Assert.Equal(originalCode, reloaded.ThreeLetterCode);
    }

    [Fact]
    public async Task UpdateTeam_ChangeColorsOnly_WhileTournamentOngoing_Succeeds()
    {
        const string originalName = "Keep Name";
        const string originalCode = "KEE";
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();
        IMapper mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Team team = await SeedTeamAsync(db, tournament.Id, originalName, originalCode);

        TeamController controller = new(teamService, null!, mapper);

        // Same identity (name + code unchanged), only the shirt color changes:
        // the identity freeze must not block this.
        ActionResult result = await controller.UpdateTeam(
            team.Id,
            new UpdateTeamRequest { Name = originalName, ThreeLetterCode = originalCode, ShirtColor = "#123456" });

        Assert.IsType<NoContentResult>(result);

        Team reloaded = await db.Teams.AsNoTracking().SingleAsync(t => t.Id == team.Id);
        Assert.Equal("#123456", reloaded.ShirtColor);
        Assert.Equal(originalName, reloaded.Name);
        Assert.Equal(originalCode, reloaded.ThreeLetterCode);
    }

    [Fact]
    public async Task UpdateTeam_ChangeName_WhileTournamentNotOngoing_Succeeds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();
        IMapper mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        // RegistrationClosed (not started): identity is still editable.
        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);
        Team team = await SeedTeamAsync(db, tournament.Id, "Before", "BEF");

        TeamController controller = new(teamService, null!, mapper);

        ActionResult result = await controller.UpdateTeam(
            team.Id,
            new UpdateTeamRequest { Name = "After", ThreeLetterCode = "AFT", ShirtColor = "Red" });

        Assert.IsType<NoContentResult>(result);

        Team reloaded = await db.Teams.AsNoTracking().SingleAsync(t => t.Id == team.Id);
        Assert.Equal("After", reloaded.Name);
        Assert.Equal("AFT", reloaded.ThreeLetterCode);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, TournamentStatus status)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Team identity freeze test tournament",
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

    private static async Task<Team> SeedTeamAsync(
        ApplicationDBContext db, Guid tournamentId, string name, string threeLetterCode)
    {
        Team team = new()
        {
            Name = name,
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = threeLetterCode,
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Red",
            TournamentId = tournamentId,
            Players = [],
            CreatedBy = "test",
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        return team;
    }
}
