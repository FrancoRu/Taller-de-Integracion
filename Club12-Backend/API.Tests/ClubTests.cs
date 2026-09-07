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

    /// <summary>
    /// Club history returns each team's seasons ordered by the tournament's
    /// start date, most recent first, and every season entry carries that
    /// start date so the history page can sort a flattened row list.
    /// </summary>
    [Fact]
    public async Task GetClubHistory_OrdersSeasonsByStartDateDescending()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        DateTime older = new(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime newer = new(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        Tournament seasonOlder = await SeedTournamentAsync(seedDb, "Ordering Older", older);
        Tournament seasonNewer = await SeedTournamentAsync(seedDb, "Ordering Newer", newer);

        Team team = await SeedTeamAsync(
            seedDb, name: $"Sorting FC {Guid.NewGuid()}", tournamentId: seasonOlder.Id);
        // Registered in the OLDER tournament first, then the newer one — so
        // insertion order is the opposite of the expected result.
        await SeedTournamentRegistrationAsync(seedDb, team, seasonOlder);
        await SeedTournamentRegistrationAsync(seedDb, team, seasonNewer);

        await BackfillAsync();

        Team linked = await ReadTeamAsync(team.Id);
        ClubHistoryResponse? history = await GetHistoryAsync(linked.ClubId!.Value.ToString());

        Assert.NotNull(history);
        ClubTeamSeasonResponse entry = history!.Teams.Single(t => t.TeamId == team.Id);

        Assert.Equal([newer, older], entry.Seasons.Select(season => season.StartDate));
        Assert.Equal(newer, entry.Seasons.Single(s => s.TournamentId == seasonNewer.Id).StartDate);
        Assert.Equal(older, entry.Seasons.Single(s => s.TournamentId == seasonOlder.Id).StartDate);
    }

    /// <summary>
    /// A newly created team is linked to a club immediately — the roster
    /// import feature (HU-53) must have a club history to search from day
    /// one, not only after someone remembers to run the bulk backfill.
    /// </summary>
    [Fact]
    public async Task CreateTeam_IsLinkedToAClub_Immediately()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Team created = await teamService.CreateTeamAsync(NewUnsavedTeam($"Newell's {Guid.NewGuid()}"));

        Team persisted = await ReadTeamAsync(created.Id);
        Assert.NotNull(persisted.ClubId);
    }

    /// <summary>
    /// A second team created later with the same name joins the SAME club as
    /// the first, instead of getting its own — mirroring the backfill's
    /// same-name-collapses-onto-one-club rule (HU-99).
    /// </summary>
    [Fact]
    public async Task CreateTeam_WithNameOfExistingClub_JoinsTheSameClub()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        string sharedName = $"Boca Paraná {Guid.NewGuid()}";
        Team first = await teamService.CreateTeamAsync(NewUnsavedTeam(sharedName));
        Team second = await teamService.CreateTeamAsync(NewUnsavedTeam(sharedName));

        Team linkedFirst = await ReadTeamAsync(first.Id);
        Team linkedSecond = await ReadTeamAsync(second.Id);

        Assert.NotNull(linkedFirst.ClubId);
        Assert.Equal(linkedFirst.ClubId, linkedSecond.ClubId);
    }

    /// <summary>
    /// Linking a squad club to a parent institution club shows up both ways:
    /// the squad's history carries the parent, and the parent's history lists
    /// the squad among its children.
    /// </summary>
    [Fact]
    public async Task LinkClubToParent_SetsParentAndListsChildOnBothSides()
    {
        Club parent = await SeedClubAsync($"Echagüe {Guid.NewGuid()}");
        Club child = await SeedClubAsync($"Echagüe B {Guid.NewGuid()}");

        ClubHistoryResponse childHistory = await LinkParentAsync(child.Id, parent.Id);

        Assert.NotNull(childHistory.ParentClub);
        Assert.Equal(parent.Id, childHistory.ParentClub!.Id);

        ClubHistoryResponse? parentHistory = await GetHistoryAsync(parent.Id.ToString());
        Assert.NotNull(parentHistory);
        Assert.Contains(parentHistory!.ChildClubs, c => c.Id == child.Id);
    }

    /// <summary>
    /// A club can share a parent institution with a squad of the OTHER gender
    /// category — club linking is purely an institutional grouping and has no
    /// notion of tournament category, since that lives on the Tournament, not
    /// the Club.
    /// </summary>
    [Fact]
    public async Task LinkClubToParent_AllowsSquadsOfDifferentTournamentCategories()
    {
        Club parent = await SeedClubAsync($"Echagüe {Guid.NewGuid()}");
        Club masculineSquad = await SeedClubAsync($"Echagüe Primera {Guid.NewGuid()}");
        Club feminineSquad = await SeedClubAsync($"Echagüe Femenino {Guid.NewGuid()}");

        await LinkParentAsync(masculineSquad.Id, parent.Id);
        await LinkParentAsync(feminineSquad.Id, parent.Id);

        ClubHistoryResponse? parentHistory = await GetHistoryAsync(parent.Id.ToString());
        Assert.NotNull(parentHistory);
        Assert.Contains(parentHistory!.ChildClubs, c => c.Id == masculineSquad.Id);
        Assert.Contains(parentHistory.ChildClubs, c => c.Id == feminineSquad.Id);
    }

    [Fact]
    public async Task LinkClubToParent_RejectsLinkingAClubToItself()
    {
        Club club = await SeedClubAsync($"Self {Guid.NewGuid()}");

        await Assert.ThrowsAsync<InvalidOperationException>(() => LinkParentAsync(club.Id, club.Id));
    }

    /// <summary>
    /// The tree stays flat, one level deep: a club that is already a squad of
    /// some institution can't itself become a parent to another club.
    /// </summary>
    [Fact]
    public async Task LinkClubToParent_RejectsWhenTargetParentIsItselfASquad()
    {
        Club grandparent = await SeedClubAsync($"Grandparent {Guid.NewGuid()}");
        Club parent = await SeedClubAsync($"Parent {Guid.NewGuid()}");
        Club child = await SeedClubAsync($"Child {Guid.NewGuid()}");

        await LinkParentAsync(parent.Id, grandparent.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => LinkParentAsync(child.Id, parent.Id));
    }

    /// <summary>
    /// The reverse of the previous rule: a club that already has squads of its
    /// own can't become a squad of another club.
    /// </summary>
    [Fact]
    public async Task LinkClubToParent_RejectsWhenChildAlreadyHasItsOwnSquads()
    {
        Club institution = await SeedClubAsync($"Institution {Guid.NewGuid()}");
        Club squad = await SeedClubAsync($"Squad {Guid.NewGuid()}");
        await LinkParentAsync(squad.Id, institution.Id);

        Club otherInstitution = await SeedClubAsync($"Other {Guid.NewGuid()}");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => LinkParentAsync(institution.Id, otherInstitution.Id));
    }

    [Fact]
    public async Task UnlinkClubParent_ClearsTheParentLink()
    {
        Club parent = await SeedClubAsync($"Parent {Guid.NewGuid()}");
        Club child = await SeedClubAsync($"Child {Guid.NewGuid()}");
        await LinkParentAsync(child.Id, parent.Id);

        ClubHistoryResponse unlinked = await UnlinkParentAsync(child.Id);

        Assert.Null(unlinked.ParentClub);

        ClubHistoryResponse? parentHistory = await GetHistoryAsync(parent.Id.ToString());
        Assert.DoesNotContain(parentHistory!.ChildClubs, c => c.Id == child.Id);
    }

    /// <summary>
    /// Renaming a club changes its display name but never its slug, so the
    /// club's public URL (used to canonicalize the history page) stays
    /// stable across a rename.
    /// </summary>
    [Fact]
    public async Task RenameClub_ChangesNameButNeverTheSlug()
    {
        Club club = await SeedClubAsync($"Echagüe A {Guid.NewGuid()}");
        string originalSlug = club.Slug;

        ClubHistoryResponse renamed = await RenameClubAsync(club.Id, "Echagüe");

        Assert.Equal("Echagüe", renamed.Name);
        Assert.Equal(originalSlug, renamed.Slug);
    }

    [Fact]
    public async Task RenameClub_RejectsABlankName()
    {
        Club club = await SeedClubAsync($"Club {Guid.NewGuid()}");

        await Assert.ThrowsAsync<InvalidOperationException>(() => RenameClubAsync(club.Id, "   "));
    }

    [Fact]
    public async Task GetAllClubs_ReturnsEveryClubOrderedByName()
    {
        string suffix = Guid.NewGuid().ToString();
        await SeedClubAsync($"Zeta {suffix}");
        await SeedClubAsync($"Alfa {suffix}");

        IReadOnlyList<ClubSummaryResponse> clubs = await GetAllClubsAsync();

        List<ClubSummaryResponse> matching = [.. clubs.Where(c => c.Name.EndsWith(suffix))];
        Assert.Equal(2, matching.Count);
        Assert.Equal(
            matching.Select(c => c.Name).OrderBy(name => name, StringComparer.Ordinal),
            matching.Select(c => c.Name));
    }

    private async Task<Club> SeedClubAsync(string name)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Club club = new()
        {
            Name = name,
            Slug = $"club-{Guid.NewGuid()}",
            CreatedBy = "test",
        };

        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        return club;
    }

    private async Task<ClubHistoryResponse> LinkParentAsync(Guid childClubId, Guid parentClubId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IClubService clubService = scope.ServiceProvider.GetRequiredService<IClubService>();
        return await clubService.LinkClubToParentAsync(childClubId, parentClubId);
    }

    private async Task<ClubHistoryResponse> UnlinkParentAsync(Guid childClubId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IClubService clubService = scope.ServiceProvider.GetRequiredService<IClubService>();
        return await clubService.UnlinkClubParentAsync(childClubId);
    }

    private async Task<ClubHistoryResponse> RenameClubAsync(Guid clubId, string name)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IClubService clubService = scope.ServiceProvider.GetRequiredService<IClubService>();
        return await clubService.RenameClubAsync(clubId, name);
    }

    private async Task<IReadOnlyList<ClubSummaryResponse>> GetAllClubsAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IClubService clubService = scope.ServiceProvider.GetRequiredService<IClubService>();
        return [.. await clubService.GetAllClubsAsync()];
    }

    private static Team NewUnsavedTeam(string name) => new()
    {
        Name = name,
        // CreateTeamAsync overwrites this with a generated unique slug.
        Slug = $"team-{Guid.NewGuid()}",
        ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
        LogoUrl = "https://example.test/logo.png",
        ShirtColor = "Green",
        Players = [],
        CreatedBy = "test",
    };

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

    private static async Task<Tournament> SeedTournamentAsync(
        ApplicationDBContext db, string name, DateTime? startDate = null)
    {
        DateTime start = startDate ?? DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = $"{name} description",
            Name = $"{name}-{Guid.NewGuid()}",
            Slug = $"{name.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid()}",
            TeamRegistrationDeadline = start.AddDays(-1),
            StartDate = start,
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
