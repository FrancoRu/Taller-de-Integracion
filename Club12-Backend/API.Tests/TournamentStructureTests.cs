using System.Reflection;

using Application.DTOs.Tournament.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// HU-cloning: GetTournamentStructureAsync assembles the full cloneable tree
/// (Tournament -> Divisions -> Stages/PlayoffMappings) via the two-step load
/// EvaluateCompletabilityAsync already uses — IGenericRepository's includes
/// only support a single level of .Include(), so Tournament.Divisions.Stages
/// cannot be expressed directly.
/// </summary>
public class TournamentStructureTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentStructureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static Tournament NewTournament(TournamentCategory category = TournamentCategory.Masculine) => new()
    {
        Description = "Structure test tournament",
        Name = $"Tournament-{Guid.NewGuid()}",
        Slug = $"tournament-{Guid.NewGuid()}",
        TeamRegistrationDeadline = DateTime.UtcNow.Date.AddDays(29),
        StartDate = DateTime.UtcNow.Date.AddDays(30),
        Category = category,
        Status = TournamentStatus.OpenForRegistration,
        Divisions = [],
        Teams = [],
        CreatedBy = "test",
    };

    private static Division NewDivision(Tournament tournament, string name, bool isCrossDivisionCup = false, int qualifiersPerGroup = 1) => new()
    {
        Name = name,
        Slug = $"{name}-{Guid.NewGuid()}",
        Tournament = tournament,
        TournamentId = tournament.Id,
        Category = tournament.Category,
        IsCrossDivisionCup = isCrossDivisionCup,
        QualifiersPerGroup = qualifiersPerGroup,
        Stages = [],
        CreatedBy = "test",
    };

    private static Stage NewStage(
        Division division, string name, StageType type, int order,
        string? bracketName = null, int bestOf = 1, int roundRobinLegs = 1) => new()
    {
        Name = name,
        Slug = $"{name}-{Guid.NewGuid()}",
        StageType = type,
        IsActive = true,
        IsElimination = type != StageType.Group,
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(7),
        DivisionId = division.Id,
        Division = division,
        BracketName = bracketName,
        BestOf = bestOf,
        RoundRobinLegs = roundRobinLegs,
        Matches = [],
        Order = order,
        CreatedBy = "test",
    };

    [Fact]
    public async Task GetTournamentStructureAsync_SingleZoneTournament_AssemblesGroupStageAndCupMapping()
    {
        Tournament tournament = NewTournament();
        Division zone = NewDivision(tournament, "Zona A");
        zone.Stages.Add(NewStage(zone, "Fase de Grupos", StageType.Group, 0));
        zone.Stages.Add(NewStage(zone, "Semifinal Copa Oro", StageType.SemiFinal, 1, "Copa Oro"));
        zone.Stages.Add(NewStage(zone, "Final Copa Oro", StageType.Final, 2, "Copa Oro"));
        zone.PlayoffMappings.Add(new DivisionPlayoffMapping
        {
            DivisionId = zone.Id,
            Division = zone,
            FromPosition = 1,
            ToPosition = 4,
            Destination = "Copa Oro",
            CreatedBy = "test",
        });
        tournament.Divisions.Add(zone);

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();
        Tournament? structure = await tournamentService.GetTournamentStructureAsync(tournament.Id);

        Assert.NotNull(structure);
        Division loadedZone = Assert.Single(structure!.Divisions);
        Assert.Equal(3, loadedZone.Stages.Count);
        Assert.Contains(loadedZone.Stages, s => s.StageType == StageType.Group && s.BracketName == null);
        DivisionPlayoffMapping mapping = Assert.Single(loadedZone.PlayoffMappings);
        Assert.Equal("Copa Oro", mapping.Destination);
        Assert.Equal(4, mapping.ToPosition - mapping.FromPosition + 1);
    }

    [Fact]
    public async Task GetTournamentStructureAsync_DivisionWithSubGroups_IncludesEveryGroupStage()
    {
        Tournament tournament = NewTournament();
        Division zone = NewDivision(tournament, "Zona B");
        zone.Stages.Add(NewStage(zone, "Grupo A", StageType.Group, 0, roundRobinLegs: 2));
        zone.Stages.Add(NewStage(zone, "Grupo B", StageType.Group, 1, roundRobinLegs: 2));
        zone.Stages.Add(NewStage(zone, "Grupo C", StageType.Group, 2, roundRobinLegs: 2));
        tournament.Divisions.Add(zone);

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();
        Tournament? structure = await tournamentService.GetTournamentStructureAsync(tournament.Id);

        List<Stage> groupStages = [.. Assert.Single(structure!.Divisions).Stages
            .Where(s => s.StageType == StageType.Group)];
        Assert.Equal(3, groupStages.Count);
        Assert.All(groupStages, s => Assert.Equal(2, s.RoundRobinLegs));
    }

    [Fact]
    public async Task GetTournamentStructureAsync_CrossDivisionCup_IncludesGroupsAndQualifiersPerGroup()
    {
        Tournament tournament = NewTournament();
        Division crossCup = NewDivision(tournament, "Copa Cruzada", isCrossDivisionCup: true, qualifiersPerGroup: 2);
        crossCup.Stages.Add(NewStage(crossCup, "Grupo 1", StageType.Group, 0));
        crossCup.Stages.Add(NewStage(crossCup, "Grupo 2", StageType.Group, 1));
        crossCup.Stages.Add(NewStage(crossCup, "Semifinal Playoffs", StageType.SemiFinal, 2, "Playoffs"));
        crossCup.Stages.Add(NewStage(crossCup, "Final Playoffs", StageType.Final, 3, "Playoffs"));
        tournament.Divisions.Add(crossCup);

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();
        Tournament? structure = await tournamentService.GetTournamentStructureAsync(tournament.Id);

        Division loadedCrossCup = Assert.Single(structure!.Divisions);
        Assert.True(loadedCrossCup.IsCrossDivisionCup);
        Assert.Equal(2, loadedCrossCup.QualifiersPerGroup);
        Assert.Equal(2, loadedCrossCup.Stages.Count(s => s.StageType == StageType.Group));
        Assert.Empty(loadedCrossCup.PlayoffMappings);
    }

    [Fact]
    public async Task GetTournamentStructureAsync_PlayoffsOnlyDivision_HasNoGroupStageButKeepsBracket()
    {
        Tournament tournament = NewTournament();
        Division playoffsOnly = NewDivision(tournament, "Reducido");
        playoffsOnly.Stages.Add(NewStage(playoffsOnly, "Semifinal Copa Only", StageType.SemiFinal, 0, "Copa Only"));
        playoffsOnly.Stages.Add(NewStage(playoffsOnly, "Final Copa Only", StageType.Final, 1, "Copa Only"));
        playoffsOnly.PlayoffMappings.Add(new DivisionPlayoffMapping
        {
            DivisionId = playoffsOnly.Id,
            Division = playoffsOnly,
            FromPosition = 1,
            ToPosition = 8,
            Destination = "Copa Only",
            CreatedBy = "test",
        });
        tournament.Divisions.Add(playoffsOnly);

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();
        Tournament? structure = await tournamentService.GetTournamentStructureAsync(tournament.Id);

        Division loadedDivision = Assert.Single(structure!.Divisions);
        Assert.DoesNotContain(loadedDivision.Stages, s => s.StageType == StageType.Group);
        Assert.Equal(2, loadedDivision.Stages.Count);
        Assert.Equal(8, Assert.Single(loadedDivision.PlayoffMappings).ToPosition);
    }

    [Fact]
    public async Task GetTournamentStructureAsync_TournamentNotFound_ReturnsNull()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

        Tournament? structure = await tournamentService.GetTournamentStructureAsync(Guid.NewGuid());

        Assert.Null(structure);
    }

    /// <summary>
    /// Structural guard: the response DTO tree carries STRUCTURE ONLY. This
    /// reflects over the response types instead of a live tournament, so it
    /// fails the moment any of the three types is ever widened to expose
    /// rosters, matches, standings, sanctions, audit, or DrawnAt.
    /// </summary>
    [Fact]
    public void TournamentStructureResponseTree_NeverExposesInstanceDataFields()
    {
        string[] forbiddenNames =
        [
            "Teams", "Positions", "Matches", "MatchSeries", "DivisionTeamRegistrations",
            "TeamTournamentRegistrations", "StageTeamMatches", "Sanctions", "DrawnAt",
        ];

        foreach (Type type in new[]
        {
            typeof(TournamentStructureResponse), typeof(DivisionStructureResponse), typeof(StageStructureResponse),
        })
        {
            IEnumerable<string> propertyNames = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name);
            Assert.Empty(propertyNames.Intersect(forbiddenNames));
        }
    }
}
