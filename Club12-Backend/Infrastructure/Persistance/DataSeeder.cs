using Application.Utils.Helper.Slug;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistance;

/// <summary>
/// Seeds sample tournament data (venues, a tournament, divisions, teams,
/// players, a group stage with matches, sanctions and blog posts) so the
/// application has something to look at in a fresh database. Controlled by
/// configuration (Seed:Enabled) and runs only once — skips silently if any
/// team already exists.
/// </summary>
public sealed class DataSeeder(ApplicationDBContext db, ILogger<DataSeeder> logger)
{
    private const string CreatedBy = AuditConstants.SystemUser;

    private static readonly string[] PrimeraTeamNames =
        ["Atlético Central", "Deportivo Norte", "Club Belgrano", "Unión del Sur"];

    private static readonly string[] PrimeraTeamCodes = ["ATC", "DNO", "CBE", "UDS"];

    private static readonly string[] PrimeraTeamColors =
        ["#1E3A8A", "#DC2626", "#16A34A", "#EA580C"];

    private static readonly string[] ReservaTeamNames =
        ["Juventud Unida", "Sportivo Oeste", "Estrella Azul", "Náutico River"];

    private static readonly string[] ReservaTeamCodes = ["JUN", "SPO", "EAZ", "NRV"];

    private static readonly string[] ReservaTeamColors =
        ["#7C3AED", "#0891B2", "#CA8A04", "#4338CA"];

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

    public async Task SeedAsync()
    {
        if (await db.Teams.AnyAsync())
        {
            logger.LogInformation("Sample data already present — skipping data seeding.");
            return;
        }

        List<Venue> venues =
        [
            new()
            {
                CreatedBy = CreatedBy,
                Name = "Polideportivo Municipal",
                Address = "Av. Siempre Viva 1234",
            },
            new()
            {
                CreatedBy = CreatedBy,
                Name = "Cancha Norte",
                Address = "Calle Los Andes 850",
            },
            new()
            {
                CreatedBy = CreatedBy,
                Name = "Estadio Club12",
                Address = "Ruta 5 km 12",
            },
        ];

        Tournament tournament = new()
        {
            CreatedBy = CreatedBy,
            Name = "Torneo Apertura 2026",
            Slug = SlugGenerator.GenerateSlug("Torneo Apertura 2026"),
            Description = "Torneo Apertura de la Liga Club12, temporada 2026.",
            TeamRegistrationDeadline = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            MinTeams = 4,
            MaxTeams = 16,
            Status = TournamentStatus.Ongoing,
            Divisions = [],
            Teams = [],
        };

        int playerCounter = 0;

        (Division Division, List<Team> Teams) primera = BuildDivisionWithTeams(
            tournament, "Primera División", PrimeraTeamNames, PrimeraTeamCodes, PrimeraTeamColors, ref playerCounter);

        (Division Division, List<Team> Teams) reserva = BuildDivisionWithTeams(
            tournament, "Reserva", ReservaTeamNames, ReservaTeamCodes, ReservaTeamColors, ref playerCounter);

        List<PlayerSanction> sanctions = [];

        foreach ((Division division, List<Team> teams) in new[] { primera, reserva })
        {
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
                StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
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

            sanctions.AddRange(SeedRoundRobinMatches(stage, teams, venues));
        }

        db.Tournaments.Add(tournament);
        db.PlayerSanctions.AddRange(sanctions);
        db.BlogPosts.AddRange(BuildBlogPosts());

        await db.SaveChangesAsync();

        int teamCount = primera.Teams.Count + reserva.Teams.Count;
        int playerCount = primera.Teams.Sum(t => t.Players.Count) + reserva.Teams.Sum(t => t.Players.Count);
        logger.LogInformation(
            "Sample data seeded: 1 tournament, 2 divisions, {TeamCount} teams, {PlayerCount} players, {SanctionCount} sanctions.",
            teamCount, playerCount, sanctions.Count);
    }

    private (Division Division, List<Team> Teams) BuildDivisionWithTeams(
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

    /// <summary>
    /// Generates every pairing among <paramref name="teams"/> as a match in
    /// <paramref name="stage"/>. The first half of the pairings are marked
    /// finished with scores, scorers and player statistics; the rest are
    /// left as upcoming, unscored matches. Returns a couple of sample
    /// sanctions raised against players from the losing side of finished
    /// matches.
    /// </summary>
    private List<PlayerSanction> SeedRoundRobinMatches(Stage stage, List<Team> teams, List<Venue> venues)
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
                    ? new DateTime(2026, 6, 8, 0, 0, 0, DateTimeKind.Utc).AddDays(i * 7)
                    : new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc).AddDays((i - finishedCount) * 7),
                Type = MatchType.Regular,
                Slug = SlugGenerator.GenerateSlug($"{home.Name}-vs-{visitor.Name}-{i}"),
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

    private static List<BlogPost> BuildBlogPosts()
    {
        (string Title, string Body)[] posts =
        [
            (
                "Arrancó el Torneo Apertura 2026",
                "La Liga Club12 dio el puntapié inicial al Torneo Apertura 2026, con ocho equipos " +
                "distribuidos en Primera División y Reserva. Los primeros partidos ya se jugaron y " +
                "prometen una temporada muy pareja."
            ),
            (
                "Se viene la fecha 4",
                "Con la fase de grupos en marcha, los equipos se preparan para una nueva fecha. " +
                "Repasá los resultados y las próximas fechas en la sección de partidos."
            ),
        ];

        List<BlogPost> blogPosts = [];

        foreach ((string title, string body) in posts)
        {
            blogPosts.Add(new BlogPost
            {
                CreatedBy = CreatedBy,
                Author = "Redacción Club12",
                Title = title,
                Slug = SlugGenerator.GenerateSlug(title),
                MarkdownText = body,
                Views = 0,
            });
        }

        return blogPosts;
    }
}
