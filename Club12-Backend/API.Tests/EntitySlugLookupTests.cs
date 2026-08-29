using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Verifies slug-based lookup (QA wave 1, Bug 5) for the entities not already
/// covered by a dedicated slug test — Team, Tournament and Match. Each service
/// path must treat a value that does NOT parse as a GUID as a slug and find the
/// row, while still resolving the same entity by its GUID id. Exercised at the
/// service layer (TeamController depends on the live SupabaseHelper, so the team
/// case cannot round-trip through the HTTP host).
/// </summary>
public class EntitySlugLookupTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EntitySlugLookupTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Team_IsResolvedBySlug_AndById()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService service = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Team team = await SeedTeamAsync(db, tournament.Id);

        Team? bySlug = await service.GetTeamByIdOrSlugAsync(team.Slug);
        Team? byId = await service.GetTeamByIdOrSlugAsync(team.Id.ToString());

        Assert.NotNull(bySlug);
        Assert.Equal(team.Id, bySlug!.Id);
        Assert.NotNull(byId);
        Assert.Equal(team.Id, byId!.Id);
    }

    [Fact]
    public async Task Tournament_IsResolvedBySlug_AndById()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITournamentService service = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament tournament = await SeedTournamentAsync(db);

        Tournament? bySlug = await service.GetTournamentByIdOrSlugAsync(tournament.Slug);
        Tournament? byId = await service.GetTournamentByIdOrSlugAsync(tournament.Id.ToString());

        Assert.NotNull(bySlug);
        Assert.Equal(tournament.Id, bySlug!.Id);
        Assert.NotNull(byId);
        Assert.Equal(tournament.Id, byId!.Id);
    }

    [Fact]
    public async Task Match_IsResolvedBySlug_AndById()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService service = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Team home = await SeedTeamAsync(db, tournament.Id);
        Match match = await SeedMatchAsync(db, tournament, home);

        Match? bySlug = await service.GetMatchByIdOrSlugAsync(match.Slug);
        Match? byId = await service.GetMatchByIdOrSlugAsync(match.Id.ToString());

        Assert.NotNull(bySlug);
        Assert.Equal(match.Id, bySlug!.Id);
        Assert.NotNull(byId);
        Assert.Equal(match.Id, byId!.Id);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(10);
        Tournament tournament = new()
        {
            Description = "Slug lookup test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        return tournament;
    }

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, Guid tournamentId)
    {
        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            TournamentId = tournamentId,
            Players = [],
            CreatedBy = "test",
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team;
    }

    private static async Task<Match> SeedMatchAsync(ApplicationDBContext db, Tournament tournament, Team home)
    {
        Guid divisionId = Guid.NewGuid();
        Guid stageId = Guid.NewGuid();

        Division division = new()
        {
            Id = divisionId,
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        };

        Stage stage = new()
        {
            Id = stageId,
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(30),
            DivisionId = divisionId,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        Match match = new()
        {
            MatchDate = tournament.StartDate,
            Type = MatchType.Regular,
            Round = 1,
            Slug = $"match-{Guid.NewGuid()}",
            IsFinished = false,
            HomeTeam = home,
            HomeTeamId = home.Id,
            Stage = stage,
            StageId = stageId,
            CreatedBy = "test",
        };

        db.Divisions.Add(division);
        db.Stages.Add(stage);
        db.Matches.Add(match);
        await db.SaveChangesAsync();
        return match;
    }
}
