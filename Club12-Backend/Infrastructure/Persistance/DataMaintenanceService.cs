using Application.DTOs.DataMaintenance.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistance;

/// <inheritdoc cref="IDataMaintenanceService"/>
public sealed class DataMaintenanceService(ApplicationDBContext db, ILogger<DataMaintenanceService> logger)
    : IDataMaintenanceService
{
    private static readonly string[] Tournament1PrimeraNames =
        ["Atlético Central", "Deportivo Norte", "Club Belgrano", "Unión del Sur"];
    private static readonly string[] Tournament1PrimeraCodes = ["ATC", "DNO", "CBE", "UDS"];
    private static readonly string[] Tournament1PrimeraColors =
        ["#1E3A8A", "#DC2626", "#16A34A", "#EA580C"];

    private static readonly string[] Tournament1ReservaNames =
        ["Juventud Unida", "Sportivo Oeste", "Estrella Azul", "Náutico River"];
    private static readonly string[] Tournament1ReservaCodes = ["JUN", "SPO", "EAZ", "NRV"];
    private static readonly string[] Tournament1ReservaColors =
        ["#7C3AED", "#0891B2", "#CA8A04", "#4338CA"];

    private static readonly string[] Tournament2PrimeraNames =
        ["Independiente Rural", "Ferroviario Central", "Atlético Cordillera", "Deportivo Litoral"];
    private static readonly string[] Tournament2PrimeraCodes = ["IRU", "FCE", "ACO", "DLI"];
    private static readonly string[] Tournament2PrimeraColors =
        ["#0D9488", "#B91C1C", "#4D7C0F", "#9333EA"];

    private static readonly string[] Tournament2ReservaNames =
        ["Newell's Barrial", "Talleres del Oeste", "Huracán del Valle", "Vélez Serrano"];
    private static readonly string[] Tournament2ReservaCodes = ["NBA", "TDO", "HDV", "VSE"];
    private static readonly string[] Tournament2ReservaColors =
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

        List<Venue> venues =
        [
            new() { CreatedBy = Domain.Constants.AuditConstants.SystemUser, Name = "Polideportivo Municipal", Address = "Av. Siempre Viva 1234" },
            new() { CreatedBy = Domain.Constants.AuditConstants.SystemUser, Name = "Cancha Norte", Address = "Calle Los Andes 850" },
            new() { CreatedBy = Domain.Constants.AuditConstants.SystemUser, Name = "Estadio Club12", Address = "Ruta 5 km 12" },
        ];

        SampleTournamentBuilder.TournamentDefinition tournament1 = new(
            Name: "Torneo Apertura 2026",
            Description: "Torneo Apertura de la Liga Club12, temporada 2026 — dato de muestra.",
            TeamRegistrationDeadline: new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 6, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
            MinTeams: 4,
            MaxTeams: 16,
            Divisions:
            [
                new("Primera División", Tournament1PrimeraNames, Tournament1PrimeraCodes, Tournament1PrimeraColors),
                new("Reserva", Tournament1ReservaNames, Tournament1ReservaCodes, Tournament1ReservaColors),
            ]);

        SampleTournamentBuilder.TournamentDefinition tournament2 = new(
            Name: "Torneo Clausura 2026",
            Description: "Torneo Clausura de la Liga Club12, temporada 2026 — dato de muestra.",
            TeamRegistrationDeadline: new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 12, 14, 0, 0, 0, DateTimeKind.Utc),
            MinTeams: 4,
            MaxTeams: 16,
            Divisions:
            [
                new("Primera División", Tournament2PrimeraNames, Tournament2PrimeraCodes, Tournament2PrimeraColors),
                new("Reserva", Tournament2ReservaNames, Tournament2ReservaCodes, Tournament2ReservaColors),
            ]);

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

    private static List<BlogPost> BuildBlogPosts(string tournament1Name, string tournament2Name)
    {
        (string Title, string Body)[] posts =
        [
            (
                $"Arrancó el {tournament1Name}",
                $"La Liga Club12 dio el puntapié inicial al {tournament1Name}, con dos divisiones " +
                "y ocho equipos en cada una. Los primeros partidos ya se jugaron y prometen una " +
                "temporada muy pareja."
            ),
            (
                $"Se define el calendario del {tournament2Name}",
                $"Con el {tournament1Name} en marcha, la Liga Club12 ya confirmó las fechas del " +
                $"{tournament2Name}. Repasá los equipos inscriptos y el fixture completo en la " +
                "sección de torneos."
            ),
        ];

        List<BlogPost> blogPosts = [];

        foreach ((string title, string body) in posts)
        {
            blogPosts.Add(new BlogPost
            {
                CreatedBy = Domain.Constants.AuditConstants.SystemUser,
                Author = "Redacción Club12",
                Title = title,
                Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug(title),
                MarkdownText = body,
                Views = 0,
            });
        }

        return blogPosts;
    }
}
