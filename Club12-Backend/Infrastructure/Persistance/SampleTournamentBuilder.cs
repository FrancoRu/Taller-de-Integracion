using Application.Utils.Constants.Stage;
using Application.Utils.Helper.Playoff;
using Application.Utils.Helper.Slug;
using Application.Utils.Helper.Standings;

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
/// DataSeeder (one call, fixed definition, no playoffs) and
/// DataMaintenanceService (two calls, two distinct definitions, full
/// group-to-final playoff bracket) so the construction logic exists once.
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
        DivisionDefinition[] Divisions);

    public sealed record BuildResult(Tournament Tournament, List<PlayerSanction> Sanctions);

    /// <summary>
    /// Builds one Tournament with every division in <paramref name="definition"/>.
    /// <paramref name="playerCounter"/> is threaded through (and must keep
    /// incrementing) across multiple calls so player names/document numbers
    /// never collide between tournaments built in the same seeding run.
    /// <paramref name="includePlayoffs"/> opts each division into a full
    /// Group -> SemiFinal -> ThirdPlace -> Final bracket built from the
    /// group stage's final standings; when false (the startup DataSeeder's
    /// default) each division gets only its Group stage, unchanged from
    /// the original behavior.
    /// </summary>
    public static BuildResult Build(
        TournamentDefinition definition,
        List<Venue> venues,
        ref int playerCounter,
        bool includePlayoffs = false)
    {
        Tournament tournament = new()
        {
            CreatedBy = CreatedBy,
            Name = definition.Name,
            Slug = SlugGenerator.GenerateSlug(definition.Name),
            Description = definition.Description,
            TeamRegistrationDeadline = definition.TeamRegistrationDeadline,
            StartDate = definition.StartDate,
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
                Slug = SlugGenerator.GenerateSlug($"Fase de Grupos {division.Name} {Guid.NewGuid()}"),
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
            AddStageTeamMatches(stage, teams);

            sanctions.AddRange(SeedRoundRobinMatches(
                stage, teams, venues, definition.FinishedMatchesStart));

            if (includePlayoffs)
            {
                SeedPlayoffStages(division, stage, teams, venues);
            }
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
            Slug = SlugGenerator.GenerateSlug($"{divisionName} {Guid.NewGuid()}"),
            Tournament = tournament,
            Stages = [],
        };

        List<Team> teams = [];

        for (int i = 0; i < teamNames.Length; i++)
        {
            Team team = new()
            {
                Id = Guid.NewGuid(),
                CreatedBy = CreatedBy,
                Name = teamNames[i],
                Slug = SlugGenerator.GenerateSlug(teamNames[i]),
                ThreeLetterCode = teamCodes[i],
                LogoUrl = $"https://placehold.co/128x128?text={teamCodes[i]}",
                ShirtColor = teamColors[i],
                Tournament = tournament,
                Players = [],
            };

            // Season-scoped participation source of truth, mirroring the
            // PlayerTeamRegistration seeding below — the denormalized
            // Team.TournamentId pointer alone is not authoritative.
            team.TeamTournamentRegistrations.Add(new TeamTournamentRegistration
            {
                CreatedBy = CreatedBy,
                TeamId = Guid.Empty,
                Team = team,
                TournamentId = Guid.Empty,
                Tournament = tournament,
            });

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
                    Slug = SlugGenerator.GenerateSlug($"{lastName} {firstName} {documentNumber}"),
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

    /// <summary>
    /// Builds a full round-robin of matches for <paramref name="teams"/>,
    /// every one of them finished with a real, non-tied score (basketball
    /// has no draws) so <see cref="PositionCalculator"/> can produce
    /// complete, real standings once every group match has been played.
    /// </summary>
    private static List<PlayerSanction> SeedRoundRobinMatches(
        Stage stage, List<Team> teams, List<Venue> venues, DateTime finishedMatchesStart)
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

        int matchCount = pairings.Count;

        for (int i = 0; i < pairings.Count; i++)
        {
            (Team home, Team visitor) = pairings[i];
            Venue venue = venues[i % venues.Count];

            // Alternating winner with a guaranteed positive margin: never a
            // tie, regardless of how many matches the round-robin has.
            bool homeWins = i % 2 == 0;
            int winnerScore = 60 + ((i * 3) % 25);
            int loserScore = winnerScore - (3 + (i % 5));
            int homeScore = homeWins ? winnerScore : loserScore;
            int visitorScore = homeWins ? loserScore : winnerScore;

            Match match = new()
            {
                CreatedBy = CreatedBy,
                MatchDate = finishedMatchesStart.AddDays(i * 7),
                Type = MatchType.Regular,
                Slug = SlugGenerator.GenerateSlug($"{home.Name}-vs-{visitor.Name}-{i}"),
                HomeTeam = home,
                HomeTeamId = home.Id,
                VisitorTeam = visitor,
                VisitorTeamId = visitor.Id,
                HomeScore = homeScore,
                VisitorScore = visitorScore,
                IsFinished = true,
                WinningTeam = homeWins ? home : visitor,
                WinningTeamId = homeWins ? home.Id : visitor.Id,
                Stage = stage,
                Venue = venue,
                PlayerStatistics = [],
                Scorers = [],
            };

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

            if (i == 0 || i == matchCount - 1)
            {
                Team losingTeam = match.WinningTeam == home ? visitor : home;

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

            stage.Matches.Add(match);
        }

        return sanctions;
    }

    /// <summary>
    /// Builds SemiFinal -> ThirdPlace -> Final stages for one division,
    /// seeded from the (now fully finished) group stage's standings via the
    /// same pure helpers the production automated-stage pipeline uses
    /// (<see cref="PositionCalculator"/>, <see cref="PlayoffSeeder"/>).
    /// Called directly instead of going through the tournament-level
    /// automated-stage endpoint because that endpoint validates the whole
    /// tournament's team count against 8/16/32/64, which doesn't fit this
    /// seeder's 2-divisions-of-4 structure. With exactly 4 teams the
    /// bracket is SemiFinal(2 matches) -> ThirdPlace(1) + Final(1); no
    /// QuarterFinal, which only appears at 16+ teams.
    /// </summary>
    private static void SeedPlayoffStages(Division division, Stage groupStage, List<Team> teams, List<Venue> venues)
    {
        List<Position> standings = PositionCalculator.CalculatePositions(groupStage.Matches);
        Dictionary<Guid, Team> teamsById = teams.ToDictionary(t => t.Id);
        List<Guid> orderedTeamIds = [.. standings.Select(p => p.TeamId)];
        List<(Guid HomeTeamId, Guid? VisitorTeamId)> semiFinalPairs = PlayoffSeeder.SeedPairs(orderedTeamIds);

        Stage semiFinalStage = new()
        {
            CreatedBy = CreatedBy,
            Name = StageTemplate.SemiFinal.Name,
            Slug = SlugGenerator.GenerateSlug($"{StageTemplate.SemiFinal.Name} {division.Name} {Guid.NewGuid()}"),
            StageType = StageType.SemiFinal,
            IsActive = true,
            IsElimination = true,
            StartDate = groupStage.EndDate.AddDays(StageTemplate.StandardGapDays),
            EndDate = groupStage.EndDate.AddDays(StageTemplate.StandardGapDays + StageTemplate.DurationDays),
            DivisionId = Guid.Empty,
            Division = division,
            Matches = [],
            Order = 1,
        };
        division.Stages.Add(semiFinalStage);
        AddStageTeamMatches(semiFinalStage, teams);

        List<Team> semiFinalWinners = [];
        List<Team> semiFinalLosers = [];

        for (int i = 0; i < semiFinalPairs.Count; i++)
        {
            (Guid homeId, Guid? visitorId) = semiFinalPairs[i];
            Team home = teamsById[homeId];
            Team visitor = teamsById[visitorId!.Value];

            int homeScore = 78 + (i * 4);
            int visitorScore = 65 + (i * 3);

            Match match = BuildFinishedPlayoffMatch(
                semiFinalStage, home, visitor, homeScore, visitorScore, venues,
                semiFinalStage.StartDate.AddDays(i * 2), i);
            semiFinalStage.Matches.Add(match);

            semiFinalWinners.Add(match.WinningTeam!);
            semiFinalLosers.Add(match.WinningTeam == home ? visitor : home);
        }

        Stage thirdPlaceStage = new()
        {
            CreatedBy = CreatedBy,
            Name = StageTemplate.ThirdPlace.Name,
            Slug = SlugGenerator.GenerateSlug($"{StageTemplate.ThirdPlace.Name} {division.Name} {Guid.NewGuid()}"),
            StageType = StageType.ThirdPlace,
            IsActive = true,
            IsElimination = true,
            StartDate = semiFinalStage.EndDate.AddDays(StageTemplate.ThirdPlaceGapDays),
            EndDate = semiFinalStage.EndDate.AddDays(StageTemplate.ThirdPlaceGapDays + StageTemplate.DurationDays),
            DivisionId = Guid.Empty,
            Division = division,
            Matches = [],
            Order = 2,
        };
        division.Stages.Add(thirdPlaceStage);
        AddStageTeamMatches(thirdPlaceStage, semiFinalLosers);

        Match thirdPlaceMatch = BuildFinishedPlayoffMatch(
            thirdPlaceStage, semiFinalLosers[0], semiFinalLosers[1], 74, 61, venues,
            thirdPlaceStage.StartDate, 0);
        thirdPlaceStage.Matches.Add(thirdPlaceMatch);

        Stage finalStage = new()
        {
            CreatedBy = CreatedBy,
            Name = StageTemplate.Final.Name,
            Slug = SlugGenerator.GenerateSlug($"{StageTemplate.Final.Name} {division.Name} {Guid.NewGuid()}"),
            StageType = StageType.Final,
            IsActive = true,
            IsElimination = true,
            StartDate = thirdPlaceStage.EndDate.AddDays(StageTemplate.StandardGapDays),
            EndDate = thirdPlaceStage.EndDate.AddDays(StageTemplate.StandardGapDays + StageTemplate.DurationDays),
            DivisionId = Guid.Empty,
            Division = division,
            Matches = [],
            Order = 3,
        };
        division.Stages.Add(finalStage);
        AddStageTeamMatches(finalStage, semiFinalWinners);

        Match finalMatch = BuildFinishedPlayoffMatch(
            finalStage, semiFinalWinners[0], semiFinalWinners[1], 82, 70, venues,
            finalStage.StartDate, 0);
        finalStage.Matches.Add(finalMatch);
    }

    private static void AddStageTeamMatches(Stage stage, List<Team> teams)
    {
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
    }

    /// <summary>
    /// Builds one finished, decisive (never-tied) knockout Match with a
    /// scorer/statistic for the winner and a scorer for the loser (when its
    /// score is positive), mirroring <see cref="SeedRoundRobinMatches"/>'s
    /// lightweight scoring pattern without duplicating every stat.
    /// </summary>
    private static Match BuildFinishedPlayoffMatch(
        Stage stage, Team home, Team visitor, int homeScore, int visitorScore,
        List<Venue> venues, DateTime matchDate, int venueIndex)
    {
        Venue venue = venues[venueIndex % venues.Count];
        Team winner = homeScore > visitorScore ? home : visitor;
        Team loser = winner == home ? visitor : home;

        Match match = new()
        {
            CreatedBy = CreatedBy,
            MatchDate = matchDate,
            Type = MatchType.Playoff,
            Slug = SlugGenerator.GenerateSlug($"{home.Name}-vs-{visitor.Name}-{stage.StageType}-{Guid.NewGuid()}"),
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            HomeScore = homeScore,
            VisitorScore = visitorScore,
            IsFinished = true,
            WinningTeam = winner,
            WinningTeamId = winner.Id,
            Stage = stage,
            Venue = venue,
            PlayerStatistics = [],
            Scorers = [],
        };

        Player winnerScorer = winner.Players.ElementAt(0);
        match.Scorers.Add(new Scorer
        {
            CreatedBy = CreatedBy,
            PlayerId = Guid.Empty,
            Player = winnerScorer,
            Points = winner == home ? homeScore : visitorScore,
            MatchId = Guid.Empty,
            Match = match,
        });
        match.PlayerStatistics.Add(new PlayerStatistic
        {
            CreatedBy = CreatedBy,
            Value = 1,
            PlayerId = Guid.Empty,
            Player = winnerScorer,
            MatchId = Guid.Empty,
            Match = match,
            Type = StatisticType.Assists,
        });

        int loserScore = loser == home ? homeScore : visitorScore;
        if (loserScore > 0)
        {
            Player loserScorer = loser.Players.ElementAt(0);
            match.Scorers.Add(new Scorer
            {
                CreatedBy = CreatedBy,
                PlayerId = Guid.Empty,
                Player = loserScorer,
                Points = loserScore,
                MatchId = Guid.Empty,
                Match = match,
            });
        }

        return match;
    }
}
