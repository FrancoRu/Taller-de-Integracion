using Application.Utils.Helper.Slug;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Persistance;

/// <summary>
/// Builds one fully-populated sample Tournament (divisions, teams, players,
/// a group stage, round-robin matches with scores/scorers/statistics, and
/// sanctions) from a declarative definition. Shared by the startup
/// DataSeeder (one call, fixed definition) and DataMaintenanceService (two
/// calls, two distinct definitions) so the construction logic exists once.
/// </summary>
public static class SampleTournamentBuilder
{
    private const string CreatedBy = AuditConstants.SystemUser;

    private static readonly string[] FirstNames =
    [
        "Juan", "Carlos", "Martín", "Diego", "Facundo", "Lucas", "Nicolás", "Matías",
        "Franco", "Ezequiel", "Agustín", "Bruno", "Iván", "Santiago", "Tomás", "Gonzalo",
    ];

    private static readonly string[] LastNames =
    [
        "González", "Rodríguez", "Fernández", "López", "Díaz", "Pérez", "Sánchez", "Romero",
        "Álvarez", "Torres", "Ruiz", "Ramírez", "Flores", "Acosta", "Benítez", "Medina",
    ];

    public sealed record DivisionDefinition(
        string DivisionName,
        string[] TeamNames,
        string[] TeamCodes,
        string[] TeamColors);

    public sealed record TournamentDefinition(
        string Name,
        string Description,
        DateTime TeamRegistrationDeadline,
        DateTime StartDate,
        DateTime StageStartDate,
        DateTime StageEndDate,
        DateTime FinishedMatchesStart,
        DateTime UpcomingMatchesStart,
        int MinTeams,
        int MaxTeams,
        DivisionDefinition[] Divisions);

    public sealed record BuildResult(Tournament Tournament, List<PlayerSanction> Sanctions);

    /// <summary>
    /// Builds one Tournament with every division in <paramref name="definition"/>.
    /// <paramref name="playerCounter"/> is threaded through (and must keep
    /// incrementing) across multiple calls so player names/document numbers
    /// never collide between tournaments built in the same seeding run.
    /// </summary>
    public static BuildResult Build(TournamentDefinition definition, List<Venue> venues, ref int playerCounter)
    {
        Tournament tournament = new()
        {
            CreatedBy = CreatedBy,
            Name = definition.Name,
            Slug = SlugGenerator.GenerateSlug(definition.Name),
            Description = definition.Description,
            TeamRegistrationDeadline = definition.TeamRegistrationDeadline,
            StartDate = definition.StartDate,
            MinTeams = definition.MinTeams,
            MaxTeams = definition.MaxTeams,
            Status = TournamentStatus.Ongoing,
            Divisions = [],
            Teams = [],
        };

        List<PlayerSanction> sanctions = [];

        foreach (DivisionDefinition divisionDef in definition.Divisions)
        {
            (Division division, List<Team> teams) = BuildDivisionWithTeams(
                tournament,
                divisionDef.DivisionName,
                divisionDef.TeamNames,
                divisionDef.TeamCodes,
                divisionDef.TeamColors,
                ref playerCounter);

            tournament.Divisions.Add(division);
            foreach (Team team in teams)
            {
                tournament.Teams.Add(team);
            }

            Stage stage = new()
            {
                CreatedBy = CreatedBy,
                Name = "Fase de Grupos",
                StageType = StageType.Group,
                IsActive = true,
                StartDate = definition.StageStartDate,
                EndDate = definition.StageEndDate,
                DivisionId = Guid.Empty,
                Division = division,
                Matches = [],
                Order = 0,
            };
            division.Stages.Add(stage);

            foreach (Team team in teams)
            {
                stage.StageTeamMatches.Add(new StageTeamMatch
                {
                    CreatedBy = CreatedBy,
                    StageId = Guid.Empty,
                    Stage = stage,
                    TeamId = Guid.Empty,
                    Team = team,
                });
            }

            sanctions.AddRange(SeedRoundRobinMatches(
                stage, teams, venues, definition.FinishedMatchesStart, definition.UpcomingMatchesStart));
        }

        return new BuildResult(tournament, sanctions);
    }

    private static (Division Division, List<Team> Teams) BuildDivisionWithTeams(
        Tournament tournament,
        string divisionName,
        string[] teamNames,
        string[] teamCodes,
        string[] teamColors,
        ref int playerCounter)
    {
        Division division = new()
        {
            CreatedBy = CreatedBy,
            Name = divisionName,
            Tournament = tournament,
            Stages = [],
        };

        List<Team> teams = [];

        for (int i = 0; i < teamNames.Length; i++)
        {
            Team team = new()
            {
                CreatedBy = CreatedBy,
                Name = teamNames[i],
                Slug = SlugGenerator.GenerateSlug(teamNames[i]),
                ThreeLetterCode = teamCodes[i],
                LogoUrl = $"https://placehold.co/128x128?text={teamCodes[i]}",
                ShirtColor = teamColors[i],
                Tournament = tournament,
                Players = [],
            };

            for (int p = 0; p < 8; p++)
            {
                playerCounter++;

                string firstName = FirstNames[playerCounter % FirstNames.Length];
                string lastName = LastNames[(playerCounter * 3) % LastNames.Length];
                string documentNumber = (30000000 + playerCounter).ToString();

                Player player = new()
                {
                    CreatedBy = CreatedBy,
                    FirstName = firstName,
                    LastName = lastName,
                    DocumentNumber = documentNumber,
                    IsSanctioned = false,
                    BirthDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc)
                        .AddYears(-(18 + (playerCounter % 20)))
                        .AddDays(playerCounter % 27),
                    SocialSecurity = $"20-{documentNumber}-3",
                    Team = team,
                };

                team.Players.Add(player);

                team.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
                {
                    CreatedBy = CreatedBy,
                    PlayerId = Guid.Empty,
                    Player = player,
                    TeamId = Guid.Empty,
                    Team = team,
                    TournamentId = Guid.Empty,
                    Tournament = tournament,
                });
            }

            teams.Add(team);
        }

        return (division, teams);
    }

    private static List<PlayerSanction> SeedRoundRobinMatches(
        Stage stage, List<Team> teams, List<Venue> venues, DateTime finishedMatchesStart, DateTime upcomingMatchesStart)
    {
        List<PlayerSanction> sanctions = [];
        List<(Team Home, Team Visitor)> pairings = [];

        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                pairings.Add((teams[i], teams[j]));
            }
        }

        int finishedCount = pairings.Count / 2;

        for (int i = 0; i < pairings.Count; i++)
        {
            (Team home, Team visitor) = pairings[i];
            bool isFinished = i < finishedCount;
            Venue venue = venues[i % venues.Count];

            Match match = new()
            {
                CreatedBy = CreatedBy,
                MatchDate = isFinished
                    ? finishedMatchesStart.AddDays(i * 7)
                    : upcomingMatchesStart.AddDays((i - finishedCount) * 7),
                Type = MatchType.Regular,
                Slug = SlugGenerator.GenerateSlug($"{home.Name}-vs-{visitor.Name}-{i}-{stage.Division!.Tournament!.Slug}"),
                HomeTeam = home,
                VisitorTeam = visitor,
                IsFinished = isFinished,
                Stage = stage,
                Venue = venue,
                PlayerStatistics = [],
                Scorers = [],
            };

            if (isFinished)
            {
                int homeScore = 1 + ((i * 2) % 4);
                int visitorScore = (i % 3);
                match.HomeScore = homeScore;
                match.VisitorScore = visitorScore;
                if (homeScore != visitorScore)
                {
                    match.WinningTeam = homeScore > visitorScore ? home : visitor;
                }

                Player homeScorer = home.Players.ElementAt(i % home.Players.Count);
                match.Scorers.Add(new Scorer
                {
                    CreatedBy = CreatedBy,
                    PlayerId = Guid.Empty,
                    Player = homeScorer,
                    Points = homeScore,
                    MatchId = Guid.Empty,
                    Match = match,
                });
                match.PlayerStatistics.Add(new PlayerStatistic
                {
                    CreatedBy = CreatedBy,
                    Value = 1,
                    PlayerId = Guid.Empty,
                    Player = homeScorer,
                    MatchId = Guid.Empty,
                    Match = match,
                    Type = StatisticType.Assists,
                });

                if (visitorScore > 0)
                {
                    Player visitorScorer = visitor.Players.ElementAt(i % visitor.Players.Count);
                    match.Scorers.Add(new Scorer
                    {
                        CreatedBy = CreatedBy,
                        PlayerId = Guid.Empty,
                        Player = visitorScorer,
                        Points = visitorScore,
                        MatchId = Guid.Empty,
                        Match = match,
                    });
                }

                if (i == 0 || i == finishedCount - 1)
                {
                    Team losingTeam;
                    if (match.WinningTeam is null)
                    {
                        losingTeam = visitor;
                    }
                    else
                    {
                        losingTeam = match.WinningTeam == home ? visitor : home;
                    }

                    Player sanctionedPlayer = losingTeam.Players.ElementAt((i + 1) % losingTeam.Players.Count);
                    sanctionedPlayer.IsSanctioned = true;

                    sanctions.Add(new PlayerSanction
                    {
                        CreatedBy = CreatedBy,
                        Duration = 2,
                        IssuedDate = match.MatchDate,
                        Description = "Expulsión por doble amonestación.",
                        Player = sanctionedPlayer,
                        PlayerId = Guid.Empty,
                        Match = match,
                        MatchId = Guid.Empty,
                        Slug = SlugGenerator.GenerateSlug(
                            $"{sanctionedPlayer.FirstName}-{sanctionedPlayer.LastName}-{match.Slug}"),
                        AppealStatus = i == 0 ? SanctionAppealStatus.Pending : SanctionAppealStatus.None,
                    });
                }
            }

            stage.Matches.Add(match);
        }

        return sanctions;
    }
}
