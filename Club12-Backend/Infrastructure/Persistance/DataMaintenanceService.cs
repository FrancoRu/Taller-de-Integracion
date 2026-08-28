using Application.DTOs.DataMaintenance.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistance;

/// <inheritdoc cref="IDataMaintenanceService"/>
public sealed class DataMaintenanceService(
    ApplicationDBContext db,
    ILogger<DataMaintenanceService> logger,
    IAuditService auditService)
    : IDataMaintenanceService
{
    // Main tournament — Primera División (8 teams).
    private static readonly string[] PrimeraNames =
    [
        "Atlético Central", "Deportivo Norte", "Club Belgrano", "Unión del Sur",
        "Racing Porteño", "Defensores del Oeste", "San Lorenzo del Valle", "Gimnasia y Tiro",
    ];
    private static readonly string[] PrimeraCodes =
        ["ATC", "DNO", "CBE", "UDS", "RPO", "DOE", "SLV", "GYT"];
    private static readonly string[] PrimeraColors =
        ["#1E3A8A", "#DC2626", "#16A34A", "#EA580C", "#0891B2", "#7C3AED", "#CA8A04", "#0D9488"];

    // Main tournament — Segunda División (8 teams).
    private static readonly string[] SegundaNames =
    [
        "Juventud Unida", "Sportivo Oeste", "Estrella Azul", "Náutico River",
        "Ferro Andino", "Talleres del Norte", "Huracán del Litoral", "Vélez Serrano",
    ];
    private static readonly string[] SegundaCodes =
        ["JUN", "SPO", "EAZ", "NRV", "FAN", "TDN", "HDL", "VSE"];
    private static readonly string[] SegundaColors =
        ["#4338CA", "#B91C1C", "#4D7C0F", "#9333EA", "#0284C7", "#65A30D", "#C026D3", "#B45309"];

    // Playoff cups shared by both main divisions: Copa Oro (positions 1-4),
    // Copa Plata (positions 5-8), each a best-of-3 SemiFinal + Final bracket.
    private static readonly SampleTournamentBuilder.PlayoffCupDefinition[] MainCups =
    [
        new("Copa Oro", 1, 4, 3),
        new("Copa Plata", 5, 8, 3),
    ];

    // Historical Clausura — two small 4-team divisions (single-bracket playoffs).
    private static readonly string[] ClausuraPrimeraNames =
        ["Independiente Rural", "Ferroviario Central", "Atlético Cordillera", "Deportivo Litoral"];
    private static readonly string[] ClausuraPrimeraCodes = ["IRU", "FCE", "ACO", "DLI"];
    private static readonly string[] ClausuraPrimeraColors =
        ["#0D9488", "#B91C1C", "#4D7C0F", "#9333EA"];

    private static readonly string[] ClausuraReservaNames =
        ["Newell's Barrial", "Talleres del Oeste", "Huracán del Valle", "Vélez del Parque"];
    private static readonly string[] ClausuraReservaCodes = ["NBA", "TDO", "HDV", "VDP"];
    private static readonly string[] ClausuraReservaColors =
        ["#0284C7", "#65A30D", "#C026D3", "#B45309"];

    public async Task<DataWipeResult> WipeSampleDataAsync(CancellationToken ct = default)
    {
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await db.Database.BeginTransactionAsync(ct);

        try
        {
            int scorers = await db.Scorers.ExecuteDeleteAsync(ct);
            int playerStatistics = await db.PlayersStatistics.ExecuteDeleteAsync(ct);
            int playerSanctions = await db.PlayerSanctions.ExecuteDeleteAsync(ct);
            int stageTeamMatches = await db.StageTeamMatches.ExecuteDeleteAsync(ct);
            int playerTeamRegistrations = await db.PlayerTeamRegistrations.ExecuteDeleteAsync(ct);
            int matches = await db.Matches.ExecuteDeleteAsync(ct);
            int matchSeries = await db.MatchSeries.ExecuteDeleteAsync(ct);
            int players = await db.Players.ExecuteDeleteAsync(ct);
            int stages = await db.Stages.ExecuteDeleteAsync(ct);
            int teams = await db.Teams.ExecuteDeleteAsync(ct);
            int divisions = await db.Divisions.ExecuteDeleteAsync(ct);
            int tournaments = await db.Tournaments.ExecuteDeleteAsync(ct);
            int venues = await db.Venues.ExecuteDeleteAsync(ct);
            int blogPosts = await db.BlogPosts.ExecuteDeleteAsync(ct);

            await transaction.CommitAsync(ct);

            logger.LogInformation(
                "Sample data wiped: {TournamentCount} tournaments, {DivisionCount} divisions, " +
                "{TeamCount} teams, {PlayerCount} players, {MatchCount} matches, {MatchSeriesCount} match series, " +
                "{PlayerSanctionCount} player sanctions, {PlayerStatisticCount} player statistics, " +
                "{ScorerCount} scorers, {StageTeamMatchCount} stage-team matches, " +
                "{PlayerTeamRegistrationCount} player-team registrations, {StageCount} stages, " +
                "{VenueCount} venues, {BlogPostCount} blog posts.",
                tournaments, divisions, teams, players, matches, matchSeries,
                playerSanctions, playerStatistics, scorers, stageTeamMatches,
                playerTeamRegistrations, stages, venues, blogPosts);

            // HU-101: record the destructive wipe for traceability. Written
            // after commit so it survives the wipe (the audit table is not part
            // of the tournament-domain data being deleted).
            await auditService.LogAsync(
                AuditAction.DataWipe,
                detail:
                    $"Se eliminaron los datos de torneos: {tournaments} torneos, {teams} equipos, " +
                    $"{players} jugadores, {matches} partidos.",
                ct: ct);

            return new DataWipeResult(
                Tournaments: tournaments,
                Divisions: divisions,
                Teams: teams,
                Players: players,
                Matches: matches,
                MatchSeries: matchSeries,
                PlayerSanctions: playerSanctions,
                PlayerStatistics: playerStatistics,
                Scorers: scorers,
                StageTeamMatches: stageTeamMatches,
                PlayerTeamRegistrations: playerTeamRegistrations,
                Stages: stages,
                Venues: venues,
                BlogPosts: blogPosts);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<DataSeedResult> SeedSampleDataAsync(CancellationToken ct = default)
    {
        if (await db.Tournaments.AnyAsync(ct))
        {
            throw new InvalidOperationException(
                "The database already has tournament data — call WipeSampleDataAsync first, then seed again.");
        }

        List<Venue> venues = BuildVenues();

        SampleTournamentBuilder.TournamentDefinition tournament1 = new(
            Name: "Torneo Apertura 2026",
            Description: "Torneo Apertura de la Liga Club 12, temporada 2026.",
            TeamRegistrationDeadline: new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 6, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new("Primera División", PrimeraNames, PrimeraCodes, PrimeraColors, MainCups),
                new("Segunda División", SegundaNames, SegundaCodes, SegundaColors, MainCups),
            ],
            CrossCup: new("Copa Club 12", GroupCount: 4, QualifiersPerGroup: 1));

        SampleTournamentBuilder.TournamentDefinition tournament2 = new(
            Name: "Torneo Clausura 2026",
            Description: "Torneo Clausura de la Liga Club 12, temporada 2026.",
            TeamRegistrationDeadline: new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 12, 14, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new("Primera", ClausuraPrimeraNames, ClausuraPrimeraCodes, ClausuraPrimeraColors),
                new("Reserva", ClausuraReservaNames, ClausuraReservaCodes, ClausuraReservaColors),
            ],
            // Historical: the Clausura is a past, completed edition.
            Status: TournamentStatus.Finished);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result1 =
            SampleTournamentBuilder.Build(tournament1, venues, ref playerCounter, includePlayoffs: true);
        SampleTournamentBuilder.BuildResult result2 =
            SampleTournamentBuilder.Build(tournament2, venues, ref playerCounter, includePlayoffs: true);

        db.Tournaments.Add(result1.Tournament);
        db.Tournaments.Add(result2.Tournament);
        db.PlayerSanctions.AddRange(result1.Sanctions);
        db.PlayerSanctions.AddRange(result2.Sanctions);

        List<BlogPost> blogPosts = BuildBlogPosts(tournament1.Name, tournament2.Name);
        db.BlogPosts.AddRange(blogPosts);

        await db.SaveChangesAsync(ct);

        int teamCount = result1.Tournament.Teams.Count + result2.Tournament.Teams.Count;
        int playerCount = result1.Tournament.Teams.Sum(t => t.Players.Count)
            + result2.Tournament.Teams.Sum(t => t.Players.Count);
        int divisionCount = result1.Tournament.Divisions.Count + result2.Tournament.Divisions.Count;
        int matchCount = result1.Tournament.Divisions.Sum(d => d.Stages.Sum(s => s.Matches.Count))
            + result2.Tournament.Divisions.Sum(d => d.Stages.Sum(s => s.Matches.Count));
        int sanctionCount = result1.Sanctions.Count + result2.Sanctions.Count;

        logger.LogInformation(
            "Sample data seeded: 2 tournaments, {DivisionCount} divisions, {TeamCount} teams, " +
            "{PlayerCount} players, {MatchCount} matches, {SanctionCount} sanctions, {BlogPostCount} blog posts.",
            divisionCount, teamCount, playerCount, matchCount, sanctionCount, blogPosts.Count);

        return new DataSeedResult(
            Tournaments: 2,
            Divisions: divisionCount,
            Teams: teamCount,
            Players: playerCount,
            Matches: matchCount,
            PlayerSanctions: sanctionCount,
            BlogPosts: blogPosts.Count);
    }

    private static List<Venue> BuildVenues()
    {
        (string Name, string Address)[] specs =
        [
            ("Estadio Club 12", "Ruta 5 km 12"),
            ("Polideportivo Municipal", "Av. Siempre Viva 1234"),
            ("Gimnasio Central", "Calle San Martín 640"),
            ("Cancha Norte", "Calle Los Andes 850"),
            ("Cancha Sur", "Av. del Trabajo 2100"),
        ];

        List<Venue> venues = [];
        foreach ((string name, string address) in specs)
        {
            venues.Add(new Venue
            {
                CreatedBy = Domain.Constants.AuditConstants.SystemUser,
                Name = name,
                Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug(name),
                Address = address,
            });
        }

        return venues;
    }

    private static List<BlogPost> BuildBlogPosts(string tournament1Name, string tournament2Name)
    {
        (string Title, string Body, bool Published)[] posts =
        [
            (
                $"Arrancó el {tournament1Name}",
                $"La Liga Club12 dio el puntapié inicial al {tournament1Name}, con la Primera y la " +
                "Segunda División de ocho equipos cada una. Los primeros partidos ya se jugaron y " +
                "prometen una temporada muy pareja.",
                true
            ),
            (
                "Primera División: se juega la fecha 1",
                "La Primera División puso primera con una jornada inaugural cargada de emociones. " +
                "Los candidatos mostraron sus cartas y la tabla de posiciones empieza a tomar forma.",
                true
            ),
            (
                "Copa Cruzada: los cruces confirmados",
                "La Copa Club 12 reúne a los dieciséis equipos del torneo en cuatro grupos. Los " +
                "partidos se disputan los miércoles para no superponerse con las zonas de los domingos. " +
                "Ya están confirmados los cruces de la fase de grupos.",
                true
            ),
            (
                "Copa Oro y Copa Plata: así quedaron los playoffs",
                "Con la fase de grupos terminada, los cuatro primeros de cada división avanzan a la " +
                "Copa Oro y del quinto al octavo a la Copa Plata. Las semifinales se jugarán al mejor " +
                "de tres partidos.",
                true
            ),
            (
                "Resumen de la fecha: goleadores y resultados",
                "Repasá los resultados de la última jornada, la tabla de goleadores y las sanciones " +
                "aplicadas por el tribunal de disciplina en la sección de estadísticas.",
                true
            ),
            (
                $"Se define el calendario del {tournament2Name}",
                $"Con el {tournament1Name} en marcha, la Liga Club12 ya prepara las fechas del " +
                $"{tournament2Name}. La nota está en revisión y se publicará con el fixture completo.",
                false
            ),
        ];

        List<BlogPost> blogPosts = [];

        foreach ((string title, string body, bool published) in posts)
        {
            blogPosts.Add(new BlogPost
            {
                CreatedBy = Domain.Constants.AuditConstants.SystemUser,
                Author = "Redacción Club12",
                Title = title,
                Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug(title),
                MarkdownText = body,
                Views = 0,
                IsPublished = published,
            });
        }

        return blogPosts;
    }
}
