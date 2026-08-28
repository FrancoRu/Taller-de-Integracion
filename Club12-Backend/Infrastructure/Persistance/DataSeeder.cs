using Domain.Entities.Models;

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

    public async Task SeedAsync()
    {
        if (await db.Teams.AnyAsync())
        {
            logger.LogInformation("Sample data already present — skipping data seeding.");
            return;
        }

        List<Venue> venues =
        [
            new() { CreatedBy = Domain.Constants.AuditConstants.SystemUser, Name = "Polideportivo Municipal", Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug("Polideportivo Municipal"), Address = "Av. Siempre Viva 1234" },
            new() { CreatedBy = Domain.Constants.AuditConstants.SystemUser, Name = "Cancha Norte", Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug("Cancha Norte"), Address = "Calle Los Andes 850" },
            new() { CreatedBy = Domain.Constants.AuditConstants.SystemUser, Name = "Estadio Club12", Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug("Estadio Club12"), Address = "Ruta 5 km 12" },
        ];

        SampleTournamentBuilder.TournamentDefinition definition = new(
            Name: "Torneo Apertura 2026",
            Description: "Torneo Apertura de la Liga Club12, temporada 2026.",
            TeamRegistrationDeadline: new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 6, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new(  "Primera División", PrimeraTeamNames, PrimeraTeamCodes, PrimeraTeamColors),
                new("Reserva", ReservaTeamNames, ReservaTeamCodes, ReservaTeamColors),
            ]);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result = SampleTournamentBuilder.Build(definition, venues, ref playerCounter);

        db.Tournaments.Add(result.Tournament);
        db.PlayerSanctions.AddRange(result.Sanctions);
        db.BlogPosts.AddRange(BuildBlogPosts());

        await db.SaveChangesAsync();

        int teamCount = result.Tournament.Teams.Count;
        int playerCount = result.Tournament.Teams.Sum(t => t.Players.Count);
        logger.LogInformation(
            "Sample data seeded: 1 tournament, 2 divisions, {TeamCount} teams, {PlayerCount} players, {SanctionCount} sanctions.",
            teamCount, playerCount, result.Sanctions.Count);
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
