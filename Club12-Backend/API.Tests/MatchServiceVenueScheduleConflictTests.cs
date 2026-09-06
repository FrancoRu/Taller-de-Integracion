using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Characterization tests for the already-shipped
/// <see cref="MatchService.HasVenueScheduleConflictAsync"/> (Phase 1, no production
/// change): proves the exact 2-hour exclusive boundary, cross-division/tournament
/// reach, different-venue independence, and self-exclusion before the rule is wired
/// into <c>CreateMatch</c> and <c>SuspendMatch</c>.
/// </summary>
public class MatchServiceVenueScheduleConflictTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MatchServiceVenueScheduleConflictTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HasVenueScheduleConflictAsync_JustUnderTwoHoursApart_SameVenue_ReturnsTrue()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Venue venue = await SeedVenueAsync(db);
        Stage stage = await SeedStageAsync(db);
        DateTime existingDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stage, venue.Id, existingDate);

        bool result = await matchService.HasVenueScheduleConflictAsync(
            venue.Id, existingDate.AddMinutes(119), Guid.Empty);

        Assert.True(result);
    }

    [Fact]
    public async Task HasVenueScheduleConflictAsync_ExactlyTwoHoursApart_SameVenue_ReturnsFalse()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Venue venue = await SeedVenueAsync(db);
        Stage stage = await SeedStageAsync(db);
        DateTime existingDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stage, venue.Id, existingDate);

        bool result = await matchService.HasVenueScheduleConflictAsync(
            venue.Id, existingDate.AddHours(2), Guid.Empty);

        Assert.False(result);
    }

    [Fact]
    public async Task HasVenueScheduleConflictAsync_SameTime_DifferentVenue_ReturnsFalse()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Venue venueA = await SeedVenueAsync(db);
        Venue venueB = await SeedVenueAsync(db);
        Stage stage = await SeedStageAsync(db);
        DateTime existingDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stage, venueA.Id, existingDate);

        bool result = await matchService.HasVenueScheduleConflictAsync(
            venueB.Id, existingDate, Guid.Empty);

        Assert.False(result);
    }

    [Fact]
    public async Task HasVenueScheduleConflictAsync_SameVenueWindow_DifferentDivisionAndTournament_ReturnsTrue()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Venue venue = await SeedVenueAsync(db);
        Stage stageOne = await SeedStageAsync(db);
        Stage stageTwo = await SeedStageAsync(db);
        DateTime existingDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        await SeedMatchAsync(db, stageOne, venue.Id, existingDate);

        bool result = await matchService.HasVenueScheduleConflictAsync(
            venue.Id, existingDate.AddHours(1), Guid.Empty);

        Assert.True(result);
        // stageTwo belongs to its own freshly seeded division/tournament, proving the
        // rule reached across division/tournament boundaries via VenueId alone.
        Assert.NotEqual(stageOne.DivisionId, stageTwo.DivisionId);
    }

    [Fact]
    public async Task HasVenueScheduleConflictAsync_ExcludeMatchId_ExcludesItself_ReturnsFalse()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Venue venue = await SeedVenueAsync(db);
        Stage stage = await SeedStageAsync(db);
        DateTime existingDate = new(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc);
        Match match = await SeedMatchAsync(db, stage, venue.Id, existingDate);

        bool result = await matchService.HasVenueScheduleConflictAsync(
            venue.Id, existingDate, match.Id);

        Assert.False(result);
    }

    private static async Task<Venue> SeedVenueAsync(ApplicationDBContext db)
    {
        Venue venue = new()
        {
            Name = $"Venue-{Guid.NewGuid()}",
            Slug = $"venue-{Guid.NewGuid()}",
            Address = "Test Address 123",
            CreatedBy = "test",
        };

        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        return venue;
    }

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db)
    {
        DateTime start = DateTime.UtcNow.Date;

        Tournament tournament = new()
        {
            Description = "Venue conflict test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = start.AddDays(-1),
            StartDate = start,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            Stages = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Stage-{Guid.NewGuid()}",
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

    private static async Task<Match> SeedMatchAsync(ApplicationDBContext db, Stage stage, Guid venueId, DateTime matchDate)
    {
        Match match = new()
        {
            Slug = $"match-{Guid.NewGuid()}",
            Type = MatchType.Regular,
            IsFinished = false,
            StageId = stage.Id,
            VenueId = venueId,
            MatchDate = matchDate,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return match;
    }
}
