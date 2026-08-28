using Application.DTOs.Club.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies the stable cross-season club identity (HU-99): the idempotent
/// backfill collapses same-named season teams onto a single
/// <see cref="Club"/>, and club history resolves every per-season team through
/// the existing <see cref="TeamTournamentRegistration"/> join. Exercises the
/// real <see cref="IClubService"/> through DI scopes against the
/// CustomWebApplicationFactory's SQLite-backed ApplicationDBContext, matching
/// this project's established integration-test style.
/// </summary>
public class ClubTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ClubTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Two same-named teams from different seasons (the "Colón SF 2026" /
    /// "Colón SF 2027" case) are linked to ONE club, while a distinctly-named
    /// team gets its own — so a club's identity is stable across seasons.
    /// </summary>
    [Fact]
    public async Task Backfill_LinksSameNamedTeamsToOneClub_AndSeparatesDistinctNames()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        string sharedName = $"Colón SF {Guid.NewGuid()}";
        Team season2026 = await SeedTeamAsync(seedDb, name: sharedName);
        Team season2027 = await SeedTeamAsync(seedDb, name: sharedName);
        Team otherClub = await SeedTeamAsync(seedDb, name: $"Unión {Guid.NewGuid()}");

        await BackfillAsync();

        Team linked2026 = await ReadTeamAsync(season2026.Id);
        Team linked2027 = await ReadTeamAsync(season2027.Id);
        Team linkedOther = await ReadTeamAsync(otherClub.Id);

        Assert.NotNull(linked2026.ClubId);
        Assert.NotNull(linked2027.ClubId);
        Assert.NotNull(linkedOther.ClubId);

        // Same name → same stable club.
        Assert.Equal(linked2026.ClubId, linked2027.ClubId);
        // Different name → different club.
        Assert.NotEqual(linked2026.ClubId, linkedOther.ClubId);
    }

    /// <summary>
    /// The backfill is idempotent: a second run creates no new clubs and links
    /// no additional teams (every team already carries a ClubId).
    /// </summary>
    [Fact]
    public async Task Backfill_IsIdempotent_SecondRunIsNoOp()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        string sharedName = $"Central {Guid.NewGuid()}";
        await SeedTeamAsync(seedDb, name: sharedName);
        await SeedTeamAsync(seedDb, name: sharedName);

        // First run links the two teams and creates exactly one club for them.
        ClubBackfillResult first = await BackfillAsync();
        Assert.True(first.TeamsLinked >= 2);

        int clubsAfterFirst = await CountClubsAsync();

        // Second run must change nothing.
        ClubBackfillResult second = await BackfillAsync();

        Assert.Equal(0, second.ClubsCreated);
        Assert.Equal(0, second.TeamsLinked);
        Assert.Equal(clubsAfterFirst, await CountClubsAsync());
    }

    /// <summary>
    /// Club history returns the per-season teams that belong to the club, each
    /// with the tournaments (seasons) it was registered in.
    /// </summary>
    [Fact]
    public async Task GetClubHistory_ReturnsPerSeasonTeamsWithTheirSeasons()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament season2026 = await SeedTournamentAsync(seedDb, "Season 2026");
        Tournament season2027 = await SeedTournamentAsync(seedDb, "Season 2027");

        string sharedName = $"Racing {Guid.NewGuid()}";
        Team team2026 = await SeedTeamAsync(seedDb, name: sharedName, tournamentId: season2026.Id);
        await SeedTournamentRegistrationAsync(seedDb, team2026, season2026);
        Team team2027 = await SeedTeamAsync(seedDb, name: sharedName, tournamentId: season2027.Id);
        await SeedTournamentRegistrationAsync(seedDb, team2027, season2027);

        await BackfillAsync();

        Team linked = await ReadTeamAsync(team2026.Id);
        ClubHistoryResponse? history = await GetHistoryAsync(linked.ClubId!.Value.ToString());

        Assert.NotNull(history);
        // Both season teams belong to the same club.
        Assert.Equal(2, history!.Teams.Count);
        Assert.Contains(history.Teams, t => t.TeamId == team2026.Id);
        Assert.Contains(history.Teams, t => t.TeamId == team2027.Id);

        // Each per-season team surfaces the season it was registered in.
        ClubTeamSeasonResponse entry2026 = history.Teams.Single(t => t.TeamId == team2026.Id);
        Assert.Contains(entry2026.Seasons, s => s.TournamentId == season2026.Id);

        ClubTeamSeasonResponse entry2027 = history.Teams.Single(t => t.TeamId == team2027.Id);
        Assert.Contains(entry2027.Seasons, s => s.TournamentId == season2027.Id);
    }

    private async Task<ClubBackfillResult> BackfillAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IClubService clubService = scope.ServiceProvider.GetRequiredService<IClubService>();
        return await clubService.BackfillClubsAsync();
    }

    private async Task<ClubHistoryResponse?> GetHistoryAsync(string idOrSlug)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IClubService clubService = scope.ServiceProvider.GetRequiredService<IClubService>();
        return await clubService.GetClubHistoryAsync(idOrSlug);
    }

    private async Task<Team> ReadTeamAsync(Guid teamId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        return await db.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId);
    }

    private async Task<int> CountClubsAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        return await db.Clubs.AsNoTracking().CountAsync();
    }

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, string name, Guid? tournamentId = null)
    {
        Team team = new()
        {
            Name = name,
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

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, string name)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = $"{name} description",
            Name = $"{name}-{Guid.NewGuid()}",
            Slug = $"{name.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Status = TournamentStatus.OpenForRegistration,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }

    private static async Task SeedTournamentRegistrationAsync(
        ApplicationDBContext db, Team team, Tournament tournament)
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
