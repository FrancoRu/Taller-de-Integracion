using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies the cascade <see cref="ITournamentService.ChangeStatusAsync"/> runs
/// when a tournament moves to <see cref="TournamentStatus.Canceled"/> or is
/// force-closed to <see cref="TournamentStatus.Finished"/> while matches are
/// still pending: every not-yet-finished match becomes
/// <see cref="MatchStatus.Canceled"/>, so no "still to be played" fixture is
/// left dangling under a dead tournament. Already-finished matches (real
/// recorded results) are never touched.
/// </summary>
public class TournamentCancelCascadeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentCancelCascadeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChangeStatusAsync_ToCanceled_CancelsPendingMatchesButKeepsFinishedOnesUntouched()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedGroupStageAsync(db, division);
        Match finishedMatch = await SeedMatchAsync(db, stage, isFinished: true, status: MatchStatus.Played);
        Match pendingMatch = await SeedMatchAsync(db, stage, isFinished: false, status: MatchStatus.Scheduled);
        Match suspendedMatch = await SeedMatchAsync(db, stage, isFinished: false, status: MatchStatus.Suspended);

        await tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.Canceled);

        Tournament reloadedTournament = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.Canceled, reloadedTournament.Status);

        Match reloadedFinished = await db.Matches.AsNoTracking().SingleAsync(m => m.Id == finishedMatch.Id);
        Assert.Equal(MatchStatus.Played, reloadedFinished.Status);
        Assert.True(reloadedFinished.IsFinished);

        Match reloadedPending = await db.Matches.AsNoTracking().SingleAsync(m => m.Id == pendingMatch.Id);
        Assert.Equal(MatchStatus.Canceled, reloadedPending.Status);
        Assert.False(reloadedPending.IsFinished);

        Match reloadedSuspended = await db.Matches.AsNoTracking().SingleAsync(m => m.Id == suspendedMatch.Id);
        Assert.Equal(MatchStatus.Canceled, reloadedSuspended.Status);
        Assert.False(reloadedSuspended.IsFinished);
    }

    [Fact]
    public async Task ChangeStatusAsync_ToFinishedWithPendingMatches_CancelsThePendingOnes()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedGroupStageAsync(db, division);
        Match pendingMatch = await SeedMatchAsync(db, stage, isFinished: false, status: MatchStatus.Scheduled);

        await tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.Finished);

        Tournament reloadedTournament = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.Finished, reloadedTournament.Status);

        Match reloadedPending = await db.Matches.AsNoTracking().SingleAsync(m => m.Id == pendingMatch.Id);
        Assert.Equal(MatchStatus.Canceled, reloadedPending.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_ToCanceled_WithNoPendingMatches_IsANoOp()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);

        await tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.Canceled);

        Tournament reloadedTournament = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.Canceled, reloadedTournament.Status);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, TournamentStatus status)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Cancel cascade test tournament",
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

    private static async Task<Match> SeedMatchAsync(
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

        return match;
    }
}
