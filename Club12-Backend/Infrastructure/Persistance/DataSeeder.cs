using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Storage;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistance;

/// <summary>
/// Seeds a rich, realistic sample of the Club 12 basketball league (Paraná,
/// "liga libre"): one <see cref="Season"/> ("Temporada 2026") grouping four
/// tournaments that mirror a real Argentine calendar —
/// <list type="bullet">
/// <item>a FINISHED Apertura (masculine: Primera División + Reserva; feminine:
///   Damas A), each with a cross-division cup ("Copa Club 12"), fully-played
///   group stages and decided playoffs so the Campeones page resolves real
///   champions for both categories; and</item>
/// <item>an ONGOING Clausura (masculine + feminine) played as a mid-season
///   double round-robin — the first jornadas finished with scores, the rest
///   still upcoming — so the standings and "Próximos"/"Últimos" fixture have
///   live data.</item>
/// </list>
/// Divisions carry their tournament's category (HU-48). Team crests are uploaded
/// from a configurable folder (<c>Seed:LogosPath</c>) via the same Supabase
/// storage path the team endpoints use; any logo failure degrades to a
/// placeholder without ever failing the seed.
///
/// Controlled by configuration: <c>Seed:Enabled</c> gates the whole path (checked
/// by the caller). By default it runs once and skips if any team already exists;
/// with <c>Seed:Reset</c> = true it first deletes the existing sample domain data
/// (FK-safe) so the orchestrator can force a clean reseed. Reset is dev-only —
/// it can only run because the surrounding <c>Seed:Enabled</c> gate is on.
/// </summary>
public sealed class DataSeeder(
    ApplicationDBContext db,
    ILogger<DataSeeder> logger,
    SupabaseHelper supabaseHelper)
{
    /// <summary>
    /// Default folder team crest PNGs are read from when <c>Seed:LogosPath</c>
    /// is not configured. Missing folder falls back to placeholder logos.
    /// </summary>
#pragma warning disable S1075 // Dev-only seed default path; overridden by the Seed:LogosPath config key.
    public const string DefaultLogosPath = @"D:\Escudos\Logos de Argentina\clubs\normal";
#pragma warning restore S1075

    // Fixed seed keeps logo-to-team assignment reproducible across reseeds.
    private const int LogoShuffleSeed = 4212;

    // --- Masculine tournament — Primera División (8 real Paraná/Entre Ríos clubs).
    private static readonly string[] PrimeraNames =
    [
        "Echagüe", "Estudiantes de Paraná", "Paraná Rowing Club", "Sionista",
        "Recreativo", "Quique", "Olimpia", "Talleres de Paraná",
    ];
    private static readonly string[] PrimeraCodes =
        ["ECH", "EDP", "ROW", "SIO", "REC", "QUI", "OLI", "TDP"];
    private static readonly string[] PrimeraColors =
        ["#1E3A8A", "#DC2626", "#16A34A", "#EA580C", "#0891B2", "#7C3AED", "#CA8A04", "#0D9488"];
    private static readonly string[] PrimeraStyles =
        ["stripes", "hoops", "diagonal", "chevron", "sash", "sides", "halves", "circles"];
    private static readonly string[] PrimeraSecondaryColors =
        ["#FFFFFF", "#FFFFFF", "#FFFFFF", "#1E293B", "#FFFFFF", "#FDE047", "#1E293B", "#FFFFFF"];

    // --- Masculine tournament — Reserva (8 more Entre Ríos clubs).
    private static readonly string[] ReservaNames =
    [
        "Central Entrerriano", "Parque Sur", "Regatas Uruguay", "Estudiantes de Concordia",
        "Social y Deportivo Colón", "Neptunia", "Independiente de Victoria", "Ciudad de Paraná",
    ];
    private static readonly string[] ReservaCodes =
        ["CEN", "PSU", "REG", "EDC", "SDC", "NEP", "IDV", "CDP"];
    private static readonly string[] ReservaColors =
        ["#4338CA", "#B91C1C", "#4D7C0F", "#9333EA", "#0284C7", "#65A30D", "#C026D3", "#B45309"];
    private static readonly string[] ReservaStyles =
        ["gradient", "vneck", "solid", "halves", "sides", "circles", "stripes", "hoops"];
    private static readonly string[] ReservaSecondaryColors =
        ["#FDE047", "#FFFFFF", "#FFFFFF", "#FFFFFF", "#FFFFFF", "#1E293B", "#FFFFFF", "#FDE047"];

    // --- Feminine tournament — Damas A (8 distinct clubs; distinct names keep
    // the globally-unique Team.Slug index happy).
    private static readonly string[] DamasNames =
    [
        "Patronato", "Peñarol de Paraná", "Bancario", "Sportivo Urquiza",
        "Belgrano de Paraná", "Deportivo Libertad", "Juventud Unida de Gualeguaychú", "San Benito",
    ];
    private static readonly string[] DamasCodes =
        ["PAT", "PEN", "BAN", "SUR", "BEP", "LIB", "JUG", "SBE"];
    private static readonly string[] DamasColors =
        ["#0F766E", "#BE123C", "#15803D", "#7E22CE", "#1D4ED8", "#A16207", "#C2410C", "#0369A1"];
    private static readonly string[] DamasStyles =
        ["hoops", "stripes", "chevron", "diagonal", "halves", "sash", "circles", "solid"];
    private static readonly string[] DamasSecondaryColors =
        ["#FFFFFF", "#FDE047", "#FFFFFF", "#1E293B", "#FFFFFF", "#1E293B", "#FFFFFF", "#FDE047"];

    // --- Clausura (ONGOING) masculine — Primera División (8 more distinct clubs,
    // so no Team.Slug collides with the finished Apertura's teams).
    private static readonly string[] ClausuraPrimeraNames =
    [
        "Rocamora", "Gimnasia y Esgrima de CdelU", "Unión de Crespo", "Sarmiento de La Paz",
        "Atlético Gualeguay", "Capuchinos", "Ministerio de Villa Elisa", "Litoral de Colón",
    ];
    private static readonly string[] ClausuraPrimeraCodes =
        ["ROC", "GEU", "UCR", "SLP", "AGY", "CAP", "MVE", "LDC"];
    private static readonly string[] ClausuraPrimeraColors =
        ["#1D4ED8", "#B91C1C", "#15803D", "#C2410C", "#0E7490", "#6D28D9", "#A16207", "#0F766E"];
    private static readonly string[] ClausuraPrimeraStyles =
        ["stripes", "hoops", "diagonal", "chevron", "sash", "sides", "halves", "circles"];
    private static readonly string[] ClausuraPrimeraSecondaryColors =
        ["#FFFFFF", "#FFFFFF", "#FFFFFF", "#1E293B", "#FFFFFF", "#FDE047", "#1E293B", "#FFFFFF"];

    // --- Clausura (ONGOING) feminine — Damas A (8 more distinct clubs).
    private static readonly string[] ClausuraDamasNames =
    [
        "Deportivo Nogoyá", "Círculo Médico", "Villaguay Central", "Atlético Diamante",
        "Ferro de Concordia", "Unión de Feliciano", "Deportivo Chajarí", "Racing de Bovril",
    ];
    private static readonly string[] ClausuraDamasCodes =
        ["NOG", "CME", "VIC", "ADI", "FCO", "UFE", "CHA", "BOV"];
    private static readonly string[] ClausuraDamasColors =
        ["#0369A1", "#BE123C", "#166534", "#9A3412", "#155E75", "#5B21B6", "#854D0E", "#115E59"];
    private static readonly string[] ClausuraDamasStyles =
        ["hoops", "stripes", "chevron", "diagonal", "halves", "sash", "circles", "solid"];
    private static readonly string[] ClausuraDamasSecondaryColors =
        ["#FFFFFF", "#FDE047", "#FFFFFF", "#1E293B", "#FFFFFF", "#1E293B", "#FFFFFF", "#FDE047"];

    // Position-range playoff cups every FINISHED regular division earns from its
    // final group standings (HU-45): 1-4 -> Copa Oro, 5-8 -> Copa Plata. Seeded
    // via SeedCupPlayoffs, so each registers a DivisionPlayoffMapping the
    // standings colouring reads to tint the qualifying positions. Single-game
    // brackets (BestOf = 1) keep the seed simple; the division champion is the
    // top cup's (Copa Oro) Final winner.
    private static readonly SampleTournamentBuilder.PlayoffCupDefinition[] MainCups =
    [
        new("Copa Oro", FromPosition: 1, ToPosition: 4, BestOf: 1),
        new("Copa Plata", FromPosition: 5, ToPosition: 8, BestOf: 1),
    ];

    // Cross-division cup shared shape: 4 internal groups, top 1 per group pooled
    // into the bracket. With 16 masculine teams that is 4 per group; with 8
    // feminine teams, 2 per group — both yield a full 4-seed bracket.
    private static readonly SampleTournamentBuilder.CrossCupDefinition CopaClub12 =
        new("Copa Club 12", GroupCount: 4, QualifiersPerGroup: 1);

    // The ongoing Clausura is mid-season: this many leading jornadas of each
    // zone are already played (finished with scores); the rest are seeded as
    // upcoming (unplayed) games so the in-progress fixture has "Próximos" data.
    private const int ClausuraPlayedRounds = 8;

    /// <summary>
    /// Seeds the sample league. <paramref name="reset"/> (from <c>Seed:Reset</c>)
    /// first wipes existing sample domain data FK-safely so a clean reseed can be
    /// forced; otherwise the seeder skips when any team already exists.
    /// <paramref name="logosPath"/> (from <c>Seed:LogosPath</c>) is the folder
    /// real team crests are read from; null/empty falls back to
    /// <see cref="DefaultLogosPath"/>, and a missing folder degrades to
    /// placeholder logos.
    /// </summary>
    public async Task SeedAsync(bool reset = false, string? logosPath = null)
    {
        if (reset)
        {
            await ResetSeededDataAsync();
        }
        else if (await db.Teams.AnyAsync())
        {
            logger.LogInformation("Sample data already present — skipping data seeding.");
            return;
        }

        List<Venue> venues = BuildVenues();

        // Apertura (first half of 2026): FINISHED — group stages fully played,
        // playoffs and the cross cup decided, tournament status Finished so the
        // Campeones page resolves real champions for it.
        DateTime aperturaStageStart = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        SampleTournamentBuilder.TournamentDefinition aperturaMasculine = new(
            Name: "Torneo Apertura 2026",
            Description: "Torneo Apertura masculino de la Liga Club 12 (Paraná), temporada 2026. Finalizado.",
            TeamRegistrationDeadline: new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate: aperturaStageStart,
            StageStartDate: aperturaStageStart,
            StageEndDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: aperturaStageStart,
            UpcomingMatchesStart: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new("Primera División", PrimeraNames, PrimeraCodes, PrimeraColors, MainCups,
                    TeamStyles: PrimeraStyles, TeamSecondaryColors: PrimeraSecondaryColors),
                new("Reserva", ReservaNames, ReservaCodes, ReservaColors, MainCups,
                    TeamStyles: ReservaStyles, TeamSecondaryColors: ReservaSecondaryColors),
            ],
            CrossCup: CopaClub12,
            Status: TournamentStatus.Finished,
            Category: TournamentCategory.Masculine);

        SampleTournamentBuilder.TournamentDefinition aperturaFeminine = new(
            Name: "Torneo Apertura Femenino 2026",
            Description: "Torneo Apertura femenino de la Liga Club 12 (Paraná), temporada 2026. Finalizado.",
            TeamRegistrationDeadline: new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            StartDate: aperturaStageStart,
            StageStartDate: aperturaStageStart,
            StageEndDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: aperturaStageStart,
            UpcomingMatchesStart: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new("Damas A", DamasNames, DamasCodes, DamasColors, MainCups,
                    TeamStyles: DamasStyles, TeamSecondaryColors: DamasSecondaryColors),
            ],
            CrossCup: CopaClub12,
            Status: TournamentStatus.Finished,
            Category: TournamentCategory.Feminine);

        // Clausura (second half of 2026): ONGOING and mid-season — a double
        // round-robin (ida y vuelta) whose first ClausuraPlayedRounds jornadas
        // are played and the rest still upcoming, so the live standings AND the
        // "Próximos"/"Últimos" fixture both have data. No playoffs yet.
        DateTime clausuraStageStart = new(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);
        SampleTournamentBuilder.TournamentDefinition clausuraMasculine = new(
            Name: "Torneo Clausura 2026",
            Description: "Torneo Clausura masculino de la Liga Club 12 (Paraná), temporada 2026. En curso.",
            TeamRegistrationDeadline: new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc),
            StartDate: clausuraStageStart,
            StageStartDate: clausuraStageStart,
            StageEndDate: new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: clausuraStageStart,
            UpcomingMatchesStart: new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new("Primera División", ClausuraPrimeraNames, ClausuraPrimeraCodes, ClausuraPrimeraColors,
                    TeamStyles: ClausuraPrimeraStyles, TeamSecondaryColors: ClausuraPrimeraSecondaryColors),
            ],
            Status: TournamentStatus.Ongoing,
            Category: TournamentCategory.Masculine,
            RoundRobinLegs: 2,
            PlayedRoundsPerZone: ClausuraPlayedRounds);

        SampleTournamentBuilder.TournamentDefinition clausuraFeminine = new(
            Name: "Torneo Clausura Femenino 2026",
            Description: "Torneo Clausura femenino de la Liga Club 12 (Paraná), temporada 2026. En curso.",
            TeamRegistrationDeadline: new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc),
            StartDate: clausuraStageStart,
            StageStartDate: clausuraStageStart,
            StageEndDate: new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: clausuraStageStart,
            UpcomingMatchesStart: new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new("Damas A", ClausuraDamasNames, ClausuraDamasCodes, ClausuraDamasColors,
                    TeamStyles: ClausuraDamasStyles, TeamSecondaryColors: ClausuraDamasSecondaryColors),
            ],
            Status: TournamentStatus.Ongoing,
            Category: TournamentCategory.Feminine,
            RoundRobinLegs: 2,
            PlayedRoundsPerZone: ClausuraPlayedRounds);

        int playerCounter = 0;
        // All four tournaments persist together, so their division/stage slugs
        // must stay unique across the whole batch (each has a DB unique index):
        // one shared registry disambiguates repeated base slugs with numeric
        // suffixes. Team slugs are globally unique too — every tournament uses a
        // distinct set of club names so no Team.Slug ever collides.
        SampleTournamentBuilder.SlugRegistry slugRegistry = new();
        // The Apertura runs are built with playoffs and the cup so they crown
        // champions; the Clausura runs stay group-only because they are still
        // in progress.
        SampleTournamentBuilder.BuildResult aperturaMascResult =
            SampleTournamentBuilder.Build(aperturaMasculine, venues, ref playerCounter, includePlayoffs: true, slugRegistry);
        SampleTournamentBuilder.BuildResult aperturaFemResult =
            SampleTournamentBuilder.Build(aperturaFeminine, venues, ref playerCounter, includePlayoffs: true, slugRegistry);
        SampleTournamentBuilder.BuildResult clausuraMascResult =
            SampleTournamentBuilder.Build(clausuraMasculine, venues, ref playerCounter, includePlayoffs: false, slugRegistry);
        SampleTournamentBuilder.BuildResult clausuraFemResult =
            SampleTournamentBuilder.Build(clausuraFeminine, venues, ref playerCounter, includePlayoffs: false, slugRegistry);

        SampleTournamentBuilder.BuildResult[] results =
            [aperturaMascResult, aperturaFemResult, clausuraMascResult, clausuraFemResult];

        // One season groups every 2026 tournament so champions show under a real
        // "Temporada 2026" instead of "Sin temporada". Each tournament keeps its
        // own category (HU-48) — the season only groups them.
        Season season2026 = new()
        {
            CreatedBy = Domain.Constants.AuditConstants.SystemUser,
            Name = "Temporada 2026",
            Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug("Temporada 2026"),
            Year = 2026,
        };
        foreach (SampleTournamentBuilder.BuildResult result in results)
        {
            result.Tournament.Season = season2026;
        }

        List<Team> allTeams = [.. results.SelectMany(r => r.Tournament.Teams)];
        await UploadTeamLogosAsync(allTeams, string.IsNullOrWhiteSpace(logosPath) ? DefaultLogosPath : logosPath);

        db.Seasons.Add(season2026);
        foreach (SampleTournamentBuilder.BuildResult result in results)
        {
            db.Tournaments.Add(result.Tournament);
            db.PlayerSanctions.AddRange(result.Sanctions);
        }
        db.BlogPosts.AddRange(BuildBlogPosts());

        await db.SaveChangesAsync();

        int teamCount = allTeams.Count;
        int playerCount = allTeams.Sum(t => t.Players.Count);
        int divisionCount = results.Sum(r => r.Tournament.Divisions.Count);
        int sanctionCount = results.Sum(r => r.Sanctions.Count);

        logger.LogInformation(
            "Sample data seeded under season '{Season}': 4 tournaments (Apertura FINISHED + Clausura ONGOING, " +
            "masculine + feminine), {DivisionCount} divisions (incl. cross-division cups), {TeamCount} teams, " +
            "{PlayerCount} players, {SanctionCount} sanctions.",
            season2026.Name, divisionCount, teamCount, playerCount, sanctionCount);
    }

    /// <summary>
    /// Deletes existing sample domain data in FK-safe order (leaf rows first) so
    /// a reseed starts from a clean slate. Uses provider-agnostic EF
    /// <c>ExecuteDeleteAsync</c> so it works against both Npgsql and the SQLite
    /// test harness. Dev-only: only reachable because the surrounding
    /// <c>Seed:Enabled</c> gate is on.
    /// </summary>
    private async Task ResetSeededDataAsync()
    {
        await db.Scorers.ExecuteDeleteAsync();
        await db.PlayersStatistics.ExecuteDeleteAsync();
        await db.PlayerSanctions.ExecuteDeleteAsync();
        await db.StageTeamMatches.ExecuteDeleteAsync();
        await db.PlayerTeamRegistrations.ExecuteDeleteAsync();
        await db.TeamTournamentRegistrations.ExecuteDeleteAsync();
        // Matches carry the optional SeriesId FK, so they go before MatchSeries.
        await db.Matches.ExecuteDeleteAsync();
        await db.MatchSeries.ExecuteDeleteAsync();
        await db.Players.ExecuteDeleteAsync();
        await db.DivisionPlayoffMappings.ExecuteDeleteAsync();
        await db.Stages.ExecuteDeleteAsync();
        await db.Teams.ExecuteDeleteAsync();
        await db.Divisions.ExecuteDeleteAsync();
        await db.Tournaments.ExecuteDeleteAsync();
        await db.Seasons.ExecuteDeleteAsync();
        await db.Venues.ExecuteDeleteAsync();
        await db.BlogPosts.ExecuteDeleteAsync();

        logger.LogInformation("Seed reset: existing sample domain data deleted before reseeding.");
    }

    /// <summary>
    /// Uploads a real PNG crest per team from <paramref name="logosPath"/> using
    /// the same Supabase image path the team endpoints use, replacing each team's
    /// placeholder <see cref="Team.LogoUrl"/>. Best-effort: a missing folder, no
    /// PNGs, or any per-file failure logs a warning and leaves the placeholder in
    /// place — logos never fail the seed. Assignment is deterministic (fixed-seed
    /// shuffle) and distinct while the folder holds at least as many files as
    /// teams.
    /// </summary>
    private async Task UploadTeamLogosAsync(IReadOnlyList<Team> teams, string logosPath)
    {
        string[] files;
        try
        {
            if (!Directory.Exists(logosPath))
            {
                logger.LogWarning(
                    "Seed logos path '{Path}' not found — keeping placeholder team logos.", logosPath);
                return;
            }

            files = Directory.GetFiles(logosPath, "*.png");
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex, "Could not read seed logos from '{Path}' — keeping placeholder team logos.", logosPath);
            return;
        }

        if (files.Length == 0)
        {
            logger.LogWarning(
                "No PNG logos found in '{Path}' — keeping placeholder team logos.", logosPath);
            return;
        }

        int[] order = [.. Enumerable.Range(0, files.Length)];
        Shuffle(order, new Random(LogoShuffleSeed));

        int uploaded = 0;
        for (int i = 0; i < teams.Count; i++)
        {
            string file = files[order[i % files.Length]];
            try
            {
                await using FileStream stream = File.OpenRead(file);
                string url = await supabaseHelper.UploadImageAsync<Team>(stream, Path.GetFileName(file));
                teams[i].LogoUrl = url;
                uploaded++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex, "Failed to upload logo '{File}' for team '{Team}' — keeping placeholder.",
                    file, teams[i].Name);
            }
        }

        logger.LogInformation(
            "Uploaded {Uploaded}/{Total} real team logos from '{Path}'.", uploaded, teams.Count, logosPath);
    }

    /// <summary>In-place Fisher-Yates shuffle with a caller-provided RNG.</summary>
    private static void Shuffle(int[] values, Random random)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
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

    private static List<BlogPost> BuildBlogPosts()
    {
        (string Title, string Body)[] posts =
        [
            (
                "El Apertura 2026 ya tiene campeones",
                "Se cerró el Torneo Apertura 2026 de la Liga Club 12 (Paraná) en las categorías masculina " +
                "y femenina. Con los playoffs y la Copa Club 12 definidos, ya conocemos a los campeones de " +
                "la primera mitad de la temporada. Mirá el podio completo en la sección Campeones."
            ),
            (
                "Arrancó el Clausura 2026: en marcha la segunda mitad",
                "El Torneo Clausura 2026 ya está en curso, con el masculino y el femenino jugando su fase " +
                "regular a dos ruedas (ida y vuelta). Repasá los resultados de las últimas fechas y el " +
                "calendario de los próximos partidos en la sección de partidos."
            ),
            (
                "Copa Club 12: la copa cruzada de la temporada",
                "La Copa Club 12 cruza a los equipos de todas las divisiones en una misma llave. Los " +
                "partidos de la copa se disputan entre semana para no superponerse con las zonas del fin " +
                "de semana."
            ),
        ];

        List<BlogPost> blogPosts = [];

        foreach ((string title, string body) in posts)
        {
            blogPosts.Add(new BlogPost
            {
                CreatedBy = Domain.Constants.AuditConstants.SystemUser,
                Author = "Redacción Club 12",
                Title = title,
                Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug(title),
                MarkdownText = body,
                Views = 0,
            });
        }

        return blogPosts;
    }
}
