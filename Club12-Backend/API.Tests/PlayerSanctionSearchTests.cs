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
/// Verifies PlayerSanctionService.GetPlayerSanctionsAsync's free-text
/// search (the Description filter) matches by the sanctioned PLAYER's name
/// (First/Second/LastName), not only by the sanction reason — HU-23 — using a
/// case-insensitive partial (Contains) match rather than an exact/prefix one —
/// HU-24. Each test embeds a unique token in exactly one field so the shared
/// IClassFixture database cannot leak matches between tests.
/// </summary>
public class PlayerSanctionSearchTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayerSanctionSearchTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_ByPlayerLastName_CaseInsensitivePartial_Matches()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        string token = Guid.NewGuid().ToString("N")[..8];

        // Token lives ONLY in the player's last name, stored upper-cased so a
        // lower-cased search term proves case-insensitivity; the reason has no
        // token, proving the match came from the name, not the description.
        PlayerSanction sanction = await SeedSanctionAsync(
            db,
            firstName: "Lionel",
            lastName: $"Messi{token.ToUpperInvariant()}",
            description: "Conducta antideportiva");

        PaginatedResponse<PlayerSanction> result = await sanctionService.GetPlayerSanctionsAsync(
            new GetPlayerSanctionsFilteredRequest { Description = token.ToLowerInvariant() });

        Assert.Contains(result.Items, s => s.Id == sanction.Id);
    }

    [Fact]
    public async Task Search_ByPlayerFirstName_MidStringSubstring_Matches()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        string token = Guid.NewGuid().ToString("N")[..8];

        // Token sits in the MIDDLE of the first name — a startsWith/exact match
        // would miss it; only a Contains match succeeds.
        PlayerSanction sanction = await SeedSanctionAsync(
            db,
            firstName: $"Lio{token}nel",
            lastName: "Maradona",
            description: "Doble falta tecnica");

        PaginatedResponse<PlayerSanction> result = await sanctionService.GetPlayerSanctionsAsync(
            new GetPlayerSanctionsFilteredRequest { Description = token });

        Assert.Contains(result.Items, s => s.Id == sanction.Id);
    }

    [Fact]
    public async Task Search_ByDescription_StillMatches_Regression()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        string token = Guid.NewGuid().ToString("N")[..8];

        // Token lives ONLY in the reason — the original description search must
        // keep working after adding the player-name clauses.
        PlayerSanction sanction = await SeedSanctionAsync(
            db,
            firstName: "Diego",
            lastName: "Simeone",
            description: $"Expulsion-{token.ToUpperInvariant()}");

        PaginatedResponse<PlayerSanction> result = await sanctionService.GetPlayerSanctionsAsync(
            new GetPlayerSanctionsFilteredRequest { Description = token.ToLowerInvariant() });

        Assert.Contains(result.Items, s => s.Id == sanction.Id);
    }

    [Fact]
    public async Task Search_NonMatchingTerm_DoesNotMatch()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        string token = Guid.NewGuid().ToString("N")[..8];

        PlayerSanction sanction = await SeedSanctionAsync(
            db,
            firstName: "Angel",
            lastName: $"DiMaria{token}",
            description: "Protesta");

        // A different, unrelated token must not surface the sanction — guards
        // against the name clause degenerating into "match everything".
        string otherToken = Guid.NewGuid().ToString("N")[..8];
        PaginatedResponse<PlayerSanction> result = await sanctionService.GetPlayerSanctionsAsync(
            new GetPlayerSanctionsFilteredRequest { Description = otherToken });

        Assert.DoesNotContain(result.Items, s => s.Id == sanction.Id);
    }

    /// <summary>
    /// Seeds the full object graph a PlayerSanction requires under SQLite's
    /// enforced FKs: Tournament→Division→Stage→Match and Team→Player, mirroring
    /// PlayerSanctionServiceTests.SeedSanctionAsync, but with caller-supplied
    /// player names and reason so a test can target one searchable field.
    /// </summary>
    private static async Task<PlayerSanction> SeedSanctionAsync(
        ApplicationDBContext db, string firstName, string lastName, string description)
    {
        DateTime issuedDate = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Guid divisionId = Guid.NewGuid();
        Guid stageId = Guid.NewGuid();
        Guid teamId = Guid.NewGuid();

        Tournament tournament = new()
        {
            Description = "Sanction search test tournament",
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
            FirstName = firstName,
            LastName = lastName,
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
            Description = description,
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
