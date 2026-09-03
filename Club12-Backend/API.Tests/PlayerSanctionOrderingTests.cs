using Application.DTOs.Abstract.Request;
using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerSanction.Request;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Verifies the paginated sanctions list served by
/// <c>GET /api/player-sanctions/find</c> defaults to newest-first ordering by
/// <see cref="PlayerSanction.IssuedDate"/>. The admin (<c>/panel/sanciones</c>)
/// and public (<c>/sanciones</c>) pages send no sort parameter and do not wire
/// server-side column sort, so the backend default is the effective order —
/// it must surface the most recently issued sanctions on the first page, not
/// the oldest.
/// </summary>
public class PlayerSanctionOrderingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayerSanctionOrderingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void FilterRequest_DefaultOrdering_IsIssuedDateDescending()
    {
        GetPlayerSanctionsFilteredRequest request = new();

        Assert.Equal(nameof(PlayerSanction.IssuedDate), request.OrderBy);
        Assert.Equal(SortOrder.Descending, request.Order);
    }

    [Fact]
    public async Task GetPlayerSanctionsAsync_NoExplicitSort_ReturnsNewestIssuedFirst()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        string token = Guid.NewGuid().ToString("N")[..8];

        DateTime oldest = new(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime newest = new(2023, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        DateTime middle = new(2022, 9, 20, 0, 0, 0, DateTimeKind.Utc);

        // Inserted in a deliberately non-chronological order so an
        // insertion-order (DateCreated) default would produce a different
        // sequence than the issue-date one under test.
        await SeedSanctionAsync(db, token, oldest);
        await SeedSanctionAsync(db, token, newest);
        await SeedSanctionAsync(db, token, middle);

        PaginatedResponse<PlayerSanction> result = await sanctionService.GetPlayerSanctionsAsync(
            new GetPlayerSanctionsFilteredRequest { Description = token, PageSize = 50 });

        List<DateTime> issuedDates = [.. result.Items.Select(s => s.IssuedDate)];

        Assert.Equal([newest, middle, oldest], issuedDates);
        for (int i = 1; i < issuedDates.Count; i++)
        {
            Assert.True(issuedDates[i] <= issuedDates[i - 1]);
        }
    }

    [Fact]
    public async Task GetPlayerSanctionsAsync_ExplicitAscendingSort_OverridesDefault()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        string token = Guid.NewGuid().ToString("N")[..8];

        DateTime oldest = new(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime newest = new(2023, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        DateTime middle = new(2022, 9, 20, 0, 0, 0, DateTimeKind.Utc);

        await SeedSanctionAsync(db, token, newest);
        await SeedSanctionAsync(db, token, oldest);
        await SeedSanctionAsync(db, token, middle);

        PaginatedResponse<PlayerSanction> result = await sanctionService.GetPlayerSanctionsAsync(
            new GetPlayerSanctionsFilteredRequest
            {
                Description = token,
                PageSize = 50,
                OrderBy = nameof(PlayerSanction.IssuedDate),
                Order = SortOrder.Ascending,
            });

        List<DateTime> issuedDates = [.. result.Items.Select(s => s.IssuedDate)];

        Assert.Equal([oldest, middle, newest], issuedDates);
    }

    [Fact]
    public async Task GetPlayerSanctionsAsync_DescriptionSearch_StillMatches_Regression()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        string token = Guid.NewGuid().ToString("N")[..8];

        PlayerSanction sanction = await SeedSanctionAsync(
            db, token, new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        PaginatedResponse<PlayerSanction> result = await sanctionService.GetPlayerSanctionsAsync(
            new GetPlayerSanctionsFilteredRequest { Description = token });

        Assert.Contains(result.Items, s => s.Id == sanction.Id);
    }

    /// <summary>
    /// Seeds the full object graph a <see cref="PlayerSanction"/> needs under
    /// SQLite's enforced FKs (Tournament→Division→Stage→Match, Team→Player),
    /// mirroring <c>PlayerSanctionSearchTests.SeedSanctionAsync</c> but with a
    /// caller-supplied issue date and a shared token embedded in the reason so
    /// a test can isolate its own rows.
    /// </summary>
    private static async Task<PlayerSanction> SeedSanctionAsync(
        ApplicationDBContext db, string token, DateTime issuedDate)
    {
        Guid divisionId = Guid.NewGuid();
        Guid stageId = Guid.NewGuid();
        Guid teamId = Guid.NewGuid();

        Tournament tournament = new()
        {
            Description = "Sanction ordering test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = issuedDate.AddDays(-1),
            StartDate = issuedDate,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Id = divisionId,
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            Stages = [],
            CreatedBy = "test",
        };

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Id = stageId,
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = issuedDate,
            EndDate = issuedDate.AddDays(14),
            DivisionId = divisionId,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        Team team = new()
        {
            Id = teamId,
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = "SEA",
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            Players = [],
            CreatedBy = "test",
        };

        Player player = new()
        {
            Slug = $"player-{Guid.NewGuid()}",
            FirstName = "Lionel",
            LastName = $"Messi-{Guid.NewGuid():N}",
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = true,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = teamId,
            CreatedBy = "test",
        };

        Match match = new()
        {
            MatchDate = issuedDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            IsFinished = true,
            Stage = stage,
            StageId = stageId,
            CreatedBy = "test",
        };

        PlayerSanction sanction = new()
        {
            Duration = 1,
            IssuedDate = issuedDate,
            Description = $"Conducta antideportiva {token}",
            Slug = $"sanction-{Guid.NewGuid()}",
            Player = player,
            Match = match,
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Divisions.Add(division);
        db.Stages.Add(stage);
        db.Teams.Add(team);
        db.Players.Add(player);
        db.Matches.Add(match);
        db.PlayerSanctions.Add(sanction);
        await db.SaveChangesAsync();

        return sanction;
    }
}
