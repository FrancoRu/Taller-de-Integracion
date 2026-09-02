using Application.DTOs.Champions.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Covers IChampionService podium computation: a division WITH a playoff is
/// crowned by its top cup's Final (winner 1st, loser 2nd) and its third-place
/// match (3rd); a group-only division is crowned by its standings top three;
/// an undecided Final yields no champion; and a best-of-N Final is decided by
/// its MatchSeries winner. Also covers the multi-cup "top cup" selection and
/// the finished-tournament champions history.
/// </summary>
public class ChampionServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ChampionServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDivisionPodiumAsync_PlayoffDivision_ReturnsFinalWinnerRunnerUpAndThird()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IChampionService championService = scope.ServiceProvider.GetRequiredService<IChampionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);

        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: null);
        Stage thirdPlaceStage = await SeedStageAsync(db, division, tournament, StageType.ThirdPlace, bracketName: null);

        // teams[0] beats teams[1] in the Final; teams[2] beats teams[3] for third place.
        await SeedFinishedMatchAsync(db, finalStage, teams[0], teams[1], 88, 70);
        await SeedFinishedMatchAsync(db, thirdPlaceStage, teams[2], teams[3], 60, 55);

        PodiumResponse? podium = await championService.GetDivisionPodiumAsync(division.Id);

        Assert.NotNull(podium);
        Assert.True(podium!.HasPlayoff);
        Assert.Equal(teams[0].Id, podium.First!.TeamId);
        Assert.Equal(teams[1].Id, podium.Second!.TeamId);
        Assert.Equal(teams[2].Id, podium.Third!.TeamId);
    }

    [Fact]
    public async Task GetDivisionPodiumAsync_GroupOnlyDivision_ReturnsStandingsTopThree()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IChampionService championService = scope.ServiceProvider.GetRequiredService<IChampionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);

        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null);

        // A ladder round-robin where the lower index always wins yields the exact
        // standings teams[0] > teams[1] > teams[2] > teams[3].
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                await SeedFinishedMatchAsync(db, groupStage, teams[i], teams[j], 90, 80);
            }
        }

        PodiumResponse? podium = await championService.GetDivisionPodiumAsync(division.Id);

        Assert.NotNull(podium);
        Assert.False(podium!.HasPlayoff);
        Assert.Equal(teams[0].Id, podium.First!.TeamId);
        Assert.Equal(teams[1].Id, podium.Second!.TeamId);
        Assert.Equal(teams[2].Id, podium.Third!.TeamId);
    }

    [Fact]
    public async Task GetDivisionPodiumAsync_FinalOnlyCupWithNoThirdPlaceStage_FallsBackToStandingsThird()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IChampionService championService = scope.ServiceProvider.GetRequiredService<IChampionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);

        // A round-robin group stage decides the full standings...
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null);
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                await SeedFinishedMatchAsync(db, groupStage, teams[i], teams[j], 90, 80);
            }
        }

        // ...and a bare Final (no SemiFinal, no ThirdPlace stage) crowns the
        // top two into a "Copa de Oro"-style title decider — there is no
        // bracket round to draw a real third place from, so it must fall
        // back to the group standings' position 3 (teams[2]) instead of
        // leaving the podium's third place empty.
        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: null);
        await SeedFinishedMatchAsync(db, finalStage, teams[0], teams[1], 88, 70);

        PodiumResponse? podium = await championService.GetDivisionPodiumAsync(division.Id);

        Assert.NotNull(podium);
        Assert.True(podium!.HasPlayoff);
        Assert.Equal(teams[0].Id, podium.First!.TeamId);
        Assert.Equal(teams[1].Id, podium.Second!.TeamId);
        Assert.Equal(teams[2].Id, podium.Third!.TeamId);
    }

    [Fact]
    public async Task GetDivisionPodiumAsync_DeepBracketWithNoThirdPlaceStage_KeepsThirdEmpty()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IChampionService championService = scope.ServiceProvider.GetRequiredService<IChampionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);

        // A real bracket (SemiFinal -> Final) that opted out of a third-place
        // match is a deliberate two-team podium — it must NOT be papered over
        // with a standings-based third, even though standings would have one.
        Stage groupStage = await SeedStageAsync(db, division, tournament, StageType.Group, bracketName: null);
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                await SeedFinishedMatchAsync(db, groupStage, teams[i], teams[j], 90, 80);
            }
        }

        Stage semiStage = await SeedStageAsync(db, division, tournament, StageType.SemiFinal, bracketName: null);
        await SeedFinishedMatchAsync(db, semiStage, teams[0], teams[3], 70, 60);
        await SeedFinishedMatchAsync(db, semiStage, teams[1], teams[2], 65, 62);

        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: null);
        await SeedFinishedMatchAsync(db, finalStage, teams[0], teams[1], 88, 70);

        PodiumResponse? podium = await championService.GetDivisionPodiumAsync(division.Id);

        Assert.NotNull(podium);
        Assert.True(podium!.HasPlayoff);
        Assert.Equal(teams[0].Id, podium.First!.TeamId);
        Assert.Equal(teams[1].Id, podium.Second!.TeamId);
        Assert.Null(podium.Third);
    }

    [Fact]
    public async Task GetDivisionPodiumAsync_UndecidedFinal_ReturnsNoChampion()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IChampionService championService = scope.ServiceProvider.GetRequiredService<IChampionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);

        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: null);
        await SeedUnfinishedMatchAsync(db, finalStage, teams[0], teams[1]);

        PodiumResponse? podium = await championService.GetDivisionPodiumAsync(division.Id);

        Assert.NotNull(podium);
        Assert.True(podium!.HasPlayoff);
        Assert.Null(podium.First);
        Assert.Null(podium.Second);
        Assert.Null(podium.Third);
    }

    [Fact]
    public async Task GetDivisionPodiumAsync_BestOfSeriesFinal_UsesSeriesWinner()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IChampionService championService = scope.ServiceProvider.GetRequiredService<IChampionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);

        Stage finalStage = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: null, bestOf: 3);

        // The series is won by teams[1] even though the (only recorded) individual
        // game shows teams[0] winning — the podium must trust the series winner.
        await SeedDecidedSeriesAsync(db, finalStage, teams[1], teams[0], bestOf: 3);
        await SeedFinishedMatchAsync(db, finalStage, teams[0], teams[1], 80, 70);

        PodiumResponse? podium = await championService.GetDivisionPodiumAsync(division.Id);

        Assert.NotNull(podium);
        Assert.True(podium!.HasPlayoff);
        Assert.Equal(teams[1].Id, podium.First!.TeamId);
        Assert.Equal(teams[0].Id, podium.Second!.TeamId);
    }

    [Fact]
    public async Task GetDivisionPodiumAsync_MultipleCups_CrownsTopCupFinalWinner()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IChampionService championService = scope.ServiceProvider.GetRequiredService<IChampionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);

        // Position 1 maps to "Copa Oro", so it is the top cup; "Copa Plata" is secondary.
        await SeedMappingAsync(db, division, 1, 2, "Copa Oro");
        await SeedMappingAsync(db, division, 3, 4, "Copa Plata");

        Stage goldFinal = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: "Copa Oro");
        Stage silverFinal = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: "Copa Plata");

        await SeedFinishedMatchAsync(db, goldFinal, teams[0], teams[1], 88, 70);
        await SeedFinishedMatchAsync(db, silverFinal, teams[2], teams[3], 66, 60);

        PodiumResponse? podium = await championService.GetDivisionPodiumAsync(division.Id);

        Assert.NotNull(podium);
        Assert.True(podium!.HasPlayoff);
        Assert.Equal(teams[0].Id, podium.First!.TeamId);
        Assert.Equal(teams[1].Id, podium.Second!.TeamId);
    }

    [Fact]
    public async Task GetChampionsHistoryAsync_OnlyFinishedTournaments_ReturnsDecidedChampions()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IChampionService championService = scope.ServiceProvider.GetRequiredService<IChampionService>();

        // A finished tournament with a decided playoff division.
        Tournament finished = await SeedTournamentAsync(db, TournamentStatus.Finished);
        Division finishedDivision = await SeedDivisionAsync(db, finished);
        List<Team> finishedTeams = await SeedTeamsAsync(db, finished, 2);
        Stage finishedFinal = await SeedStageAsync(db, finishedDivision, finished, StageType.Final, bracketName: null);
        await SeedFinishedMatchAsync(db, finishedFinal, finishedTeams[0], finishedTeams[1], 88, 70);

        // An ongoing tournament must never appear in the history.
        Tournament ongoing = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Division ongoingDivision = await SeedDivisionAsync(db, ongoing);
        List<Team> ongoingTeams = await SeedTeamsAsync(db, ongoing, 2);
        Stage ongoingFinal = await SeedStageAsync(db, ongoingDivision, ongoing, StageType.Final, bracketName: null);
        await SeedFinishedMatchAsync(db, ongoingFinal, ongoingTeams[0], ongoingTeams[1], 90, 80);

        List<ChampionHistoryResponse> history = await championService.GetChampionsHistoryAsync(seasonId: null);

        Assert.Contains(history, entry =>
            entry.TournamentId == finished.Id
            && entry.DivisionName == finishedDivision.Name
            && entry.ChampionTeam.TeamId == finishedTeams[0].Id);
        Assert.DoesNotContain(history, entry => entry.TournamentId == ongoing.Id);

        // A single-bracket division crowns one champion with no sub-cup label.
        ChampionHistoryResponse single = history.Single(entry =>
            entry.TournamentId == finished.Id && entry.DivisionName == finishedDivision.Name);
        Assert.Null(single.CupName);
    }

    [Fact]
    public async Task GetChampionsHistoryAsync_MultipleCups_ReturnsOneChampionPerCup()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IChampionService championService = scope.ServiceProvider.GetRequiredService<IChampionService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Finished);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 4);

        // Two sub-cups: Copa Oro (top tier, seeded from position 1) and Copa Plata.
        await SeedMappingAsync(db, division, 1, 2, "Copa Oro");
        await SeedMappingAsync(db, division, 3, 4, "Copa Plata");

        Stage goldFinal = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: "Copa Oro");
        Stage silverFinal = await SeedStageAsync(db, division, tournament, StageType.Final, bracketName: "Copa Plata");

        await SeedFinishedMatchAsync(db, goldFinal, teams[0], teams[1], 88, 70);
        await SeedFinishedMatchAsync(db, silverFinal, teams[2], teams[3], 66, 60);

        List<ChampionHistoryResponse> history = await championService.GetChampionsHistoryAsync(seasonId: null);

        List<ChampionHistoryResponse> divisionChampions = [.. history
            .Where(entry => entry.TournamentId == tournament.Id && entry.DivisionName == division.Name)];

        // Both cups crown a champion: Copa Oro → teams[0], Copa Plata → teams[2].
        Assert.Equal(2, divisionChampions.Count);
        Assert.Contains(divisionChampions, e => e.CupName == "Copa Oro" && e.ChampionTeam.TeamId == teams[0].Id);
        Assert.Contains(divisionChampions, e => e.CupName == "Copa Plata" && e.ChampionTeam.TeamId == teams[2].Id);

        // The top cup (Copa Oro) is listed before the lower tier (Copa Plata).
        Assert.Equal("Copa Oro", divisionChampions[0].CupName);
    }

    private static async Task<Tournament> SeedTournamentAsync(
        ApplicationDBContext db, TournamentStatus status = TournamentStatus.OpenForRegistration)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);
        Tournament tournament = new()
        {
            Description = "Champion test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
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
            IsCrossDivisionCup = false,
            Stages = [],
            CreatedBy = "test",
        };
        db.Divisions.Add(division);
        await db.SaveChangesAsync();
        return division;
    }

    private static async Task SeedMappingAsync(ApplicationDBContext db, Division division, int from, int to, string destination)
    {
        db.DivisionPlayoffMappings.Add(new DivisionPlayoffMapping
        {
            DivisionId = division.Id,
            FromPosition = from,
            ToPosition = to,
            Destination = destination,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();
    }

    private static async Task<List<Team>> SeedTeamsAsync(ApplicationDBContext db, Tournament tournament, int count)
    {
        List<Team> teams = [];
        for (int i = 0; i < count; i++)
        {
            Team team = new()
            {
                Name = $"Team-{Guid.NewGuid()}",
                Slug = $"team-{Guid.NewGuid()}",
                ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
                LogoUrl = "http://example.com/logo.png",
                ShirtColor = "Red",
                TournamentId = tournament.Id,
                Players = [],
                CreatedBy = "test",
            };
            db.Teams.Add(team);
            teams.Add(team);
        }
        await db.SaveChangesAsync();
        return teams;
    }

    private static async Task<Stage> SeedStageAsync(
        ApplicationDBContext db, Division division, Tournament tournament, StageType stageType, string? bracketName, int bestOf = 1)
    {
        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"{stageType}-{Guid.NewGuid()}",
            StageType = stageType,
            IsActive = true,
            IsElimination = stageType != StageType.Group,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(7),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            BracketName = bracketName,
            BestOf = bestOf,
            CreatedBy = "test",
        };
        db.Stages.Add(stage);
        await db.SaveChangesAsync();
        return stage;
    }

    private static async Task SeedFinishedMatchAsync(
        ApplicationDBContext db, Stage stage, Team home, Team visitor, int homeScore, int visitorScore)
    {
        db.Matches.Add(new Match
        {
            StageId = stage.Id,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            MatchDate = stage.StartDate,
            HomeTeamId = home.Id,
            VisitorTeamId = visitor.Id,
            HomeScore = homeScore,
            VisitorScore = visitorScore,
            IsFinished = true,
            WinningTeamId = homeScore > visitorScore ? home.Id : visitor.Id,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedUnfinishedMatchAsync(ApplicationDBContext db, Stage stage, Team home, Team visitor)
    {
        db.Matches.Add(new Match
        {
            StageId = stage.Id,
            Type = MatchType.Playoff,
            Slug = $"match-{Guid.NewGuid()}",
            MatchDate = stage.StartDate,
            HomeTeamId = home.Id,
            VisitorTeamId = visitor.Id,
            IsFinished = false,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedDecidedSeriesAsync(
        ApplicationDBContext db, Stage stage, Team winner, Team loser, int bestOf)
    {
        db.MatchSeries.Add(new MatchSeries
        {
            StageId = stage.Id,
            HomeTeamId = winner.Id,
            VisitorTeamId = loser.Id,
            BestOf = bestOf,
            WinningTeamId = winner.Id,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();
    }
}
