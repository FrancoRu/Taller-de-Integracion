using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Integrity guard for reverting an Ongoing tournament back to draft
/// (Ongoing -> RegistrationClosed via
/// <see cref="ITournamentService.ChangeStatusAsync"/>): the revert is allowed
/// only while nothing has been played. Any finished match (Status Played or
/// WalkOver, i.e. <see cref="Match.IsFinished"/> == true) carries results that a
/// fixture teardown would lose, so the revert is rejected (mapped to 409). With
/// no played match the revert succeeds and the (unplayed) fixture is torn down.
/// </summary>
public class TournamentRevertGuardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentRevertGuardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(MatchStatus.Played)]
    [InlineData(MatchStatus.WalkOver)]
    public async Task ChangeStatusAsync_RevertOngoingToDraft_WithPlayedMatch_ThrowsAndKeepsOngoing(
        MatchStatus playedStatus)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedGroupStageAsync(db, division);
        await SeedMatchAsync(db, stage, isFinished: true, status: playedStatus);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.RegistrationClosed));

        // Status unchanged and the played match is untouched.
        Tournament reloaded = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.Ongoing, reloaded.Status);
        Assert.Equal(1, await db.Matches.AsNoTracking().CountAsync(m => m.StageId == stage.Id));
    }

    [Fact]
    public async Task ChangeStatusAsync_RevertOngoingToDraft_WithNoPlayedMatch_SucceedsAndTearsDownFixture()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedGroupStageAsync(db, division);
        // An unplayed (scheduled) fixture: reverting is safe and tears it down.
        await SeedMatchAsync(db, stage, isFinished: false, status: MatchStatus.Scheduled);

        await tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.RegistrationClosed);

        Tournament reloaded = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.RegistrationClosed, reloaded.Status);
        Assert.Equal(0, await db.Matches.AsNoTracking().CountAsync(m => m.StageId == stage.Id));
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, TournamentStatus status)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Revert guard test tournament",
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

    private static async Task SeedMatchAsync(
        ApplicationDBContext db, Stage stage, bool isFinished, MatchStatus status)
    {
        Match match = new()
        {
            Slug = $"match-{Guid.NewGuid()}",
            Type = Domain.Enums.MatchType.Regular,
            StageId = stage.Id,
            MatchDate = DateTime.UtcNow.Date.AddDays(31),
            IsFinished = isFinished,
            Status = status,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();
    }
}
