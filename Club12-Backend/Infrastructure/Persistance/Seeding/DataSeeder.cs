using Application.Interfaces.Storage;
using Application.Utils.Helper.Slug;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Storage;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistance;

/// <summary>
/// Seeds the app's standard reference dataset for the Club 12 basketball
/// league (Paraná, "liga libre"). <c>Seed:Seasons</c> says how many
/// consecutive <see cref="Season"/> rows to build, counting backwards from
/// "Temporada XXV" (2026) one per calendar year; 1 (the default) keeps the
/// original single-season dataset, higher values produce a demo-sized league
/// whose <see cref="Club"/> rows carry real multi-season history (HU-99).
/// Every season repeats the same two FINISHED tournaments —
/// <list type="bullet">
/// <item><b>Torneo Femenino</b>: a single zone of 7 teams, ida y vuelta,
///   feeding one Copa de Oro (all 7 teams, byes to the top seeds since 7 is
///   not a power of two); and</item>
/// <item><b>Torneo Masculino</b>: 3 zones (Zona A/B, 10 teams each; Zona C, 13
///   teams), each single round-robin, each with its own Copa Oro (top 4) and
///   Copa Plata (the rest) brackets — plus a 4th, parallel competition, Copa
///   Cruzada: 6 group-stage zones (5 of 4 teams + 1 of 3, ida y vuelta, drawn
///   from a 23-team subset of the zone rosters) feeding one combined 12-team
///   playoff (byes to the top seeds).</item>
/// </list>
/// The most recent season additionally gets two ONGOING Clausura tournaments
/// (masculine and feminine) whose first <c>ClausuraPlayedRounds</c> jornadas
/// are played and whose remaining jornadas are scheduled, so the app also has
/// a live standings table and a real "Próximos partidos" fixture to show.
/// Every playoff bracket is built by <see cref="SampleTournamentBuilder"/>'s
/// generic elimination-bracket seeder (RoundOf16/QuarterFinal/SemiFinal/Final
/// as the seed count needs, byes to the best seeds when not a power of two);
/// every cup's SemiFinal and Final rounds are Best-of-3, every earlier round
/// Best-of-1. Divisions carry their tournament's category (HU-48). Team
/// crests are uploaded from a configurable folder (<c>Seed:LogosPath</c>) via
/// the same Supabase storage path the team endpoints use; any logo failure
/// degrades to a placeholder without ever failing the seed. The ficha médica
/// backfill works the same way: <c>Seed:MedicalRecordPath</c> when configured,
/// otherwise the generic ficha médica embedded in the assembly (see
/// <see cref="EmbeddedMedicalRecordResourceName"/>), so the seeded rosters
/// end up habilitado on any machine — including a deployed server — instead
/// of only on the one a hardcoded local path happens to point at (a league
/// whose players are all un-habilitado while holding scorer rows contradicts
/// HU-57/HU-60).
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
    SupabaseHelper supabaseHelper,
    IMedicalRecordStorage medicalRecordStorage)
{
    /// <summary>
    /// Default folder team crest PNGs are read from when <c>Seed:LogosPath</c>
    /// is not configured. Missing folder falls back to placeholder logos.
    /// </summary>
#pragma warning disable S1075 // Dev-only seed default path; overridden by the Seed:LogosPath config key.
    public const string DefaultLogosPath = @"D:\Escudos\Logos de Argentina\clubs\normal";
#pragma warning restore S1075

    /// <summary>
    /// Embedded resource name of the generic ficha médica shipped inside the
    /// assembly (see <c>Persistance/Seeding/Assets/ficha-medica-generica.pdf</c>,
    /// wired via <c>Infrastructure.csproj</c>'s <c>EmbeddedResource</c> glob).
    /// Used when <c>Seed:MedicalRecordPath</c> is not configured, so the
    /// backfill works on any machine — including a deployed server — instead
    /// of only the one a hardcoded local path happens to point at.
    /// </summary>
    private const string EmbeddedMedicalRecordResourceName =
        "Infrastructure.Persistance.Seeding.Assets.ficha-medica-generica.pdf";

    /// <summary>
    /// File name recorded for the seeded ficha médica, whether it came from
    /// the embedded resource or the last-resort generated placeholder (see
    /// <see cref="BuildPlaceholderMedicalRecordPdf"/>).
    /// </summary>
    private const string PlaceholderMedicalRecordFileName = "ficha-medica-generica.pdf";

    // Fixed seed keeps logo-to-team assignment reproducible across reseeds.
    private const int LogoShuffleSeed = 4212;

    private const int LatestSeasonNumber = 25;
    private const int LatestSeasonYear = 2026;
    private const int MaxSeasonCount = 12;
    private const int MinPlayersPerTeam = 5;
    private const int MaxPlayersPerTeam = 20;

    private const int ClausuraPlayedRounds = 5;

    private const int UpsetPercent = 26;

    // Flushes progress every N uploaded rows so an interruption loses at most
    // this many refs and the step stays resumable (medical-records-storage-eligibility, ADR #7).
    private const int MedicalRecordSaveBatchSize = 50;

    // --- Torneo Femenino — Zona Única (7 real Paraná/Entre Ríos clubs).
    private static readonly string[] FemeninoNames =
    [
        "Patronato", "Peñarol de Paraná", "Bancario", "Sportivo Urquiza",
        "Belgrano de Paraná", "Deportivo Libertad", "Juventud Unida de Gualeguaychú",
    ];
    private static readonly string[] FemeninoCodes =
        ["PAT", "PEN", "BAN", "SUR", "BEP", "LIB", "JUG"];
    private static readonly string[] FemeninoColors =
        ["#0F766E", "#BE123C", "#15803D", "#7E22CE", "#1D4ED8", "#A16207", "#C2410C"];
    private static readonly string[] FemeninoStyles =
        ["hoops", "stripes", "chevron", "diagonal", "halves", "sash", "circles"];
    private static readonly string[] FemeninoSecondaryColors =
        ["#FFFFFF", "#FDE047", "#FFFFFF", "#1E293B", "#FFFFFF", "#1E293B", "#FFFFFF"];

    // --- Torneo Masculino — Zona A (10 real Paraná/Entre Ríos clubs).
    private static readonly string[] ZonaANames =
    [
        "Echagüe", "Estudiantes de Paraná", "Paraná Rowing Club", "Sionista",
        "Recreativo", "Quique", "Olimpia", "Talleres de Paraná",
        "Central Entrerriano", "Parque Sur",
    ];
    private static readonly string[] ZonaACodes =
        ["ECH", "EDP", "ROW", "SIO", "REC", "QUI", "OLI", "TDP", "CEN", "PSU"];
    private static readonly string[] ZonaAColors =
    [
        "#1E3A8A", "#DC2626", "#16A34A", "#EA580C", "#0891B2",
        "#7C3AED", "#CA8A04", "#0D9488", "#4338CA", "#B91C1C",
    ];
    private static readonly string[] ZonaAStyles =
    [
        "stripes", "hoops", "diagonal", "chevron", "sash",
        "sides", "halves", "circles", "gradient", "vneck",
    ];
    private static readonly string[] ZonaASecondaryColors =
    [
        "#FFFFFF", "#FFFFFF", "#FFFFFF", "#1E293B", "#FFFFFF",
        "#FDE047", "#1E293B", "#FFFFFF", "#FDE047", "#FFFFFF",
    ];

    // --- Torneo Masculino — Zona B (10 real Paraná/Entre Ríos clubs).
    private static readonly string[] ZonaBNames =
    [
        "Regatas Uruguay", "Estudiantes de Concordia", "Social y Deportivo Colón", "Neptunia",
        "Independiente de Victoria", "Ciudad de Paraná",
        "Rocamora", "Gimnasia y Esgrima de CdelU", "Unión de Crespo", "Sarmiento de La Paz",
    ];
    private static readonly string[] ZonaBCodes =
        ["REG", "EDC", "SDC", "NEP", "IDV", "CDP", "ROC", "GEU", "UCR", "SLP"];
    private static readonly string[] ZonaBColors =
    [
        "#4D7C0F", "#9333EA", "#0284C7", "#65A30D", "#C026D3",
        "#B45309", "#1D4ED8", "#B91C1C", "#15803D", "#C2410C",
    ];
    private static readonly string[] ZonaBStyles =
    [
        "solid", "halves", "sides", "circles", "stripes",
        "hoops", "stripes", "hoops", "diagonal", "chevron",
    ];
    private static readonly string[] ZonaBSecondaryColors =
    [
        "#FFFFFF", "#FFFFFF", "#FFFFFF", "#1E293B", "#FFFFFF",
        "#FDE047", "#FFFFFF", "#FFFFFF", "#FFFFFF", "#1E293B",
    ];

    // --- Torneo Masculino — Zona C (13 real/plausible Paraná/Entre Ríos clubs).
    private static readonly string[] ZonaCNames =
    [
        "Atlético Gualeguay", "Capuchinos", "Ministerio de Villa Elisa", "Litoral de Colón",
        "Sportivo Diamante", "Atlético Federación", "Defensores de Bovril", "Unión de Nogoyá",
        "Ferrocarril Concepción", "Deportivo Chajarí", "Sarmiento de Villaguay",
        "Atlético María Grande", "Unión de Viale",
    ];
    private static readonly string[] ZonaCCodes =
        ["AGY", "CAP", "MVE", "LDC", "SPD", "ATF", "DBO", "UNO", "FCC", "DCH", "SVI", "AMG", "UVI"];
    private static readonly string[] ZonaCColors =
    [
        "#0E7490", "#6D28D9", "#A16207", "#0F766E", "#0EA5E9", "#DB2777", "#65A30D",
        "#EA580C", "#7C2D12", "#4C1D95", "#0F172A", "#B45309", "#166534",
    ];
    private static readonly string[] ZonaCStyles =
    [
        "sash", "sides", "halves", "circles", "stripes", "hoops", "diagonal",
        "chevron", "sash", "sides", "halves", "circles", "solid",
    ];
    private static readonly string[] ZonaCSecondaryColors =
    [
        "#FFFFFF", "#FDE047", "#1E293B", "#FFFFFF", "#FFFFFF", "#FDE047", "#FFFFFF",
        "#1E293B", "#FFFFFF", "#1E293B", "#FFFFFF", "#FDE047", "#FFFFFF",
    ];

    private sealed record ClubSpec(string Name, string Code, string Color, string Style, string Secondary);

    private static ClubSpec[] Zip(
        string[] names, string[] codes, string[] colors, string[] styles, string[] secondary) =>
        [.. names.Select((name, i) => new ClubSpec(name, codes[i], colors[i], styles[i], secondary[i]))];

    private static readonly ClubSpec[] FeminineClubs =
        Zip(FemeninoNames, FemeninoCodes, FemeninoColors, FemeninoStyles, FemeninoSecondaryColors);

    private static readonly ClubSpec[] MasculineClubs =
    [
        .. Zip(ZonaANames, ZonaACodes, ZonaAColors, ZonaAStyles, ZonaASecondaryColors),
        .. Zip(ZonaBNames, ZonaBCodes, ZonaBColors, ZonaBStyles, ZonaBSecondaryColors),
        .. Zip(ZonaCNames, ZonaCCodes, ZonaCColors, ZonaCStyles, ZonaCSecondaryColors),
    ];

    // Femenino: a single Copa de Oro spans the whole standings (all 7 teams
    // qualify — there is no Copa Plata for this tournament). Masculino: each
    // zone earns its own Copa Oro (top 4) + Copa Plata (the rest of the
    // standings; positions past ToPosition are out of both cups) from its
    // final group standings (HU-45), via SeedCupPlayoffs/SeedEliminationBracket
    // — each registers a DivisionPlayoffMapping the standings colouring reads
    // to tint the qualifying positions, and byes pad brackets that aren't a
    // power of two (e.g. Zona C's 9-team Copa Plata) to the best seeds. BestOf
    // 3 applies to every cup's SemiFinal + Final only; any earlier round
    // always plays Bo1.
    private static readonly SampleTournamentBuilder.PlayoffCupDefinition[] FemeninoCups =
    [
        new("Copa de Oro", FromPosition: 1, ToPosition: 7, BestOf: 3),
    ];
    private static readonly SampleTournamentBuilder.PlayoffCupDefinition[] ZonaABCups =
    [
        new("Copa Oro", FromPosition: 1, ToPosition: 4, BestOf: 3),
        new("Copa Plata", FromPosition: 5, ToPosition: 8, BestOf: 3),
    ];
    private static readonly SampleTournamentBuilder.PlayoffCupDefinition[] ZonaCCups =
    [
        new("Copa Oro", FromPosition: 1, ToPosition: 4, BestOf: 3),
        new("Copa Plata", FromPosition: 5, ToPosition: 13, BestOf: 3),
    ];

    // Copa Cruzada: a 4th masculine competition, structurally its own
    // division (parallel to, not nested inside, Zonas A/B/C) built via the
    // tournament's CrossCup wiring restricted to a 23-team pool (TeamPoolSize)
    // — the first 23 of the 33 Zona A/B/C teams in build order (Zona A + Zona
    // B in full, Zona C's first 3), reusing real rosters instead of inventing
    // a disjoint set of teams, mirroring how real leagues have overlapping
    // club rosters across parallel competitions. 23 teams / 6 groups splits
    // into 5 zones of 4 + 1 of 3; the top 2 of each (12 teams, byes to the
    // best seeds since 12 isn't a power of two) feed the combined playoff.
    private static readonly SampleTournamentBuilder.CrossCupDefinition CopaCruzada = new(
        "Copa Cruzada",
        GroupCount: 6,
        QualifiersPerGroup: 2,
        RoundRobinLegs: 2,
        FinalsBestOf: 3,
        TeamPoolSize: 23);

    /// <summary>
    /// Seeds the sample league. <paramref name="reset"/> (from <c>Seed:Reset</c>)
    /// first wipes existing sample domain data FK-safely so a clean reseed can be
    /// forced; otherwise the seeder skips when any team already exists.
    /// <paramref name="logosPath"/> (from <c>Seed:LogosPath</c>) is the folder
    /// real team crests are read from; null/empty falls back to
    /// <see cref="DefaultLogosPath"/>, and a missing folder degrades to
    /// placeholder logos.
    /// </summary>
    public async Task SeedAsync(
        bool reset = false, string? logosPath = null,
        string? medicalRecordPath = null, bool forceMedicalRecords = false,
        int seasonCount = 1, int playersPerTeam = SampleTournamentBuilder.DefaultPlayersPerTeam)
    {
        if (reset)
        {
            await ResetSeededDataAsync();
        }
        else if (await db.Teams.AnyAsync())
        {
            // Standalone backfill (Seed:MedicalRecords=true): bypass the
            // skip-if-teams-exist guard so the medical-records step alone can
            // run against an already-seeded database, without a full reset
            // (medical-records-storage-eligibility, Part 3, ADR #8).
            if (forceMedicalRecords)
            {
                logger.LogInformation("Sample data already present — running the medical-records backfill only.");
                await SeedMedicalRecordsAsync(medicalRecordPath);
                return;
            }

            logger.LogInformation("Sample data already present — skipping data seeding.");
            return;
        }

        seasonCount = Math.Clamp(seasonCount, 1, MaxSeasonCount);
        playersPerTeam = Math.Clamp(playersPerTeam, MinPlayersPerTeam, MaxPlayersPerTeam);

        List<Venue> venues = BuildVenues();
        SampleTournamentBuilder.SlugRegistry slugRegistry = new();
        int playerCounter = 0;

        List<Season> seasons = [];
        List<SampleTournamentBuilder.BuildResult> allResults = [];

        bool autoDetect = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        for (int offset = seasonCount - 1; offset >= 0; offset--)
        {
            int number = LatestSeasonNumber - offset;
            List<SampleTournamentBuilder.BuildResult> results = BuildSeasonTournaments(
                number, isCurrent: offset == 0, playersPerTeam, venues, slugRegistry, ref playerCounter);

            Season season = new()
            {
                CreatedBy = AuditConstants.SystemUser,
                Name = SeasonName(number),
                Slug = SlugGenerator.GenerateSlug(SeasonName(number)),
                Year = SeasonYear(number),
            };

            foreach (SampleTournamentBuilder.BuildResult result in results)
            {
                result.Tournament.Season = season;
            }

            seasons.Add(season);
            allResults.AddRange(results);
        }

        List<Team> allTeams = [.. allResults.SelectMany(r => r.Tournament.Teams)];
        List<Club> clubs = BuildClubs(allTeams);

        await UploadTeamLogosAsync(allTeams, string.IsNullOrWhiteSpace(logosPath) ? DefaultLogosPath : logosPath);

        foreach (Club club in clubs)
        {
            club.LogoUrl = club.Teams.Select(t => t.LogoUrl).FirstOrDefault();
        }

        db.Venues.AddRange(venues);
        db.Clubs.AddRange(clubs);
        db.Seasons.AddRange(seasons);

        foreach (SampleTournamentBuilder.BuildResult result in allResults)
        {
            db.Tournaments.Add(result.Tournament);
            db.PlayerSanctions.AddRange(result.Sanctions);
        }

        db.BlogPosts.AddRange(BuildBlogPosts());
        db.TeamStaffs.AddRange(BuildTeamStaff([.. allResults]));
        db.TeamPointDeductions.AddRange(BuildPointDeductions(allResults));

        db.ChangeTracker.AutoDetectChangesEnabled = autoDetect;
        await db.SaveChangesAsync();

        // Runs AFTER SaveChangesAsync: TeamId/PlayerId are store-generated
        // (EntityBase.Id defaults to Guid.Empty), so there is no real object
        // key to build from before this point (medical-records-storage-eligibility, ADR #6).
        await SeedMedicalRecordsAsync(medicalRecordPath);

        logger.LogInformation(
            "Sample data seeded: {SeasonCount} seasons ({FirstSeason}–{LastSeason}), {TournamentCount} tournaments, " +
            "{ClubCount} clubs, {DivisionCount} divisions, {TeamCount} teams, {PlayerCount} players, " +
            "{MatchCount} matches, {SanctionCount} sanctions.",
            seasons.Count,
            seasons[0].Name,
            seasons[^1].Name,
            allResults.Count,
            clubs.Count,
            allResults.Sum(r => r.Tournament.Divisions.Count),
            allTeams.Count,
            allTeams.Sum(t => t.Players.Count),
            allResults.Sum(r => r.Tournament.Divisions.Sum(d => d.Stages.Sum(s => s.Matches.Count))),
            allResults.Sum(r => r.Sanctions.Count));
    }

    private static List<SampleTournamentBuilder.BuildResult> BuildSeasonTournaments(
        int seasonNumber,
        bool isCurrent,
        int playersPerTeam,
        List<Venue> venues,
        SampleTournamentBuilder.SlugRegistry slugRegistry,
        ref int playerCounter)
    {
        int year = SeasonYear(seasonNumber);
        string seasonName = SeasonName(seasonNumber);
        int seed = (seasonNumber * 7919) + 13;

        ClubSpec[] feminine = Draw(FeminineClubs, seed);
        ClubSpec[] masculine = Draw(MasculineClubs, seed + 1);

        DateTime aperturaStart = new(year, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime aperturaEnd = new(year, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        SampleTournamentBuilder.TournamentDefinition femenino = new(
            Name: $"Torneo Apertura Femenino {year}",
            Description:
                $"Torneo Apertura Femenino de la Liga Club 12 (Paraná), {seasonName}. " +
                "Zona única a dos ruedas y Copa de Oro para las siete clasificadas. Finalizado.",
            TeamRegistrationDeadline: aperturaStart.AddDays(-14),
            StartDate: aperturaStart,
            StageStartDate: aperturaStart,
            StageEndDate: aperturaEnd,
            Divisions: [ZoneOf("Zona Única", feminine, FemeninoCups)],
            Status: TournamentStatus.Finished,
            Category: TournamentCategory.Feminine,
            RoundRobinLegs: 2,
            PlayersPerTeam: playersPerTeam,
            UpsetPercent: UpsetPercent,
            VarietySeed: seed);

        SampleTournamentBuilder.TournamentDefinition masculino = new(
            Name: $"Torneo Apertura Masculino {year}",
            Description:
                $"Torneo Apertura Masculino de la Liga Club 12 (Paraná), {seasonName}. " +
                "Tres zonas a dos ruedas con Copa Oro y Copa Plata cada una, más la Copa Cruzada. Finalizado.",
            TeamRegistrationDeadline: aperturaStart.AddDays(-14),
            StartDate: aperturaStart,
            StageStartDate: aperturaStart,
            StageEndDate: aperturaEnd,
            Divisions:
            [
                ZoneOf("Zona A", [.. masculine[..10]], ZonaABCups),
                ZoneOf("Zona B", [.. masculine[10..20]], ZonaABCups),
                ZoneOf("Zona C", [.. masculine[20..]], ZonaCCups),
            ],
            CrossCup: CopaCruzada,
            Status: TournamentStatus.Finished,
            Category: TournamentCategory.Masculine,
            RoundRobinLegs: 2,
            PlayersPerTeam: playersPerTeam,
            UpsetPercent: UpsetPercent,
            VarietySeed: seed + 2);

        List<SampleTournamentBuilder.BuildResult> results =
        [
            SampleTournamentBuilder.Build(femenino, venues, ref playerCounter, includePlayoffs: true, slugRegistry),
            SampleTournamentBuilder.Build(masculino, venues, ref playerCounter, includePlayoffs: true, slugRegistry),
        ];

        if (!isCurrent)
        {
            return results;
        }

        DateTime clausuraStart = new(year, 8, 2, 0, 0, 0, DateTimeKind.Utc);
        DateTime clausuraEnd = new(year, 12, 20, 0, 0, 0, DateTimeKind.Utc);

        ClubSpec[] clausuraMasculine = Draw(MasculineClubs, seed + 3);
        ClubSpec[] clausuraFeminine = Draw(FeminineClubs, seed + 4);

        SampleTournamentBuilder.TournamentDefinition clausuraMasculino = new(
            Name: $"Torneo Clausura Masculino {year}",
            Description:
                $"Torneo Clausura Masculino de la Liga Club 12 (Paraná), {seasonName}. " +
                $"En disputa: {ClausuraPlayedRounds} jornadas jugadas, el resto ya programadas.",
            TeamRegistrationDeadline: clausuraStart.AddDays(-14),
            StartDate: clausuraStart,
            StageStartDate: clausuraStart,
            StageEndDate: clausuraEnd,
            Divisions:
            [
                ZoneOf("Zona A", [.. clausuraMasculine[..8]], null),
                ZoneOf("Zona B", [.. clausuraMasculine[8..16]], null),
            ],
            Status: TournamentStatus.Ongoing,
            Category: TournamentCategory.Masculine,
            RoundRobinLegs: 2,
            PlayedRoundsPerZone: ClausuraPlayedRounds,
            PlayersPerTeam: playersPerTeam,
            UpsetPercent: UpsetPercent,
            VarietySeed: seed + 5);

        SampleTournamentBuilder.TournamentDefinition clausuraFemenino = new(
            Name: $"Torneo Clausura Femenino {year}",
            Description:
                $"Torneo Clausura Femenino de la Liga Club 12 (Paraná), {seasonName}. " +
                $"En disputa: {ClausuraPlayedRounds} jornadas jugadas, el resto ya programadas.",
            TeamRegistrationDeadline: clausuraStart.AddDays(-14),
            StartDate: clausuraStart,
            StageStartDate: clausuraStart,
            StageEndDate: clausuraEnd,
            Divisions: [ZoneOf("Zona Única", [.. clausuraFeminine[..6]], null)],
            Status: TournamentStatus.Ongoing,
            Category: TournamentCategory.Feminine,
            RoundRobinLegs: 2,
            PlayedRoundsPerZone: ClausuraPlayedRounds,
            PlayersPerTeam: playersPerTeam,
            UpsetPercent: UpsetPercent,
            VarietySeed: seed + 6);

        results.Add(SampleTournamentBuilder.Build(
            clausuraMasculino, venues, ref playerCounter, includePlayoffs: false, slugRegistry));
        results.Add(SampleTournamentBuilder.Build(
            clausuraFemenino, venues, ref playerCounter, includePlayoffs: false, slugRegistry));

        return results;
    }

    private static SampleTournamentBuilder.DivisionDefinition ZoneOf(
        string name, ClubSpec[] clubs, SampleTournamentBuilder.PlayoffCupDefinition[]? cups) =>
        new(name,
            [.. clubs.Select(c => c.Name)],
            [.. clubs.Select(c => c.Code)],
            [.. clubs.Select(c => c.Color)],
            cups,
            TeamStyles: [.. clubs.Select(c => c.Style)],
            TeamSecondaryColors: [.. clubs.Select(c => c.Secondary)]);

    /// <summary>
    /// The season's draw: the same club pool re-sorted deterministically for
    /// this season. Because the builder ranks teams by their position in the
    /// zone list, re-drawing every season is what makes each season produce a
    /// different table, different zone composition and a different champion
    /// instead of replaying one identical year N times.
    /// </summary>
    private static ClubSpec[] Draw(ClubSpec[] clubs, int seed)
    {
        int[] order = [.. Enumerable.Range(0, clubs.Length)];
        Shuffle(order, new Random(seed));

        return [.. order.Select(i => clubs[i])];
    }

    private static string SeasonName(int number) => $"Temporada {ToRoman(number)}";

    private static int SeasonYear(int number) => LatestSeasonYear - (LatestSeasonNumber - number);

    private static string ToRoman(int number)
    {
        (int Value, string Symbol)[] map =
        [
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"),
            (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
        ];

        StringBuilder roman = new();
        foreach ((int value, string symbol) in map)
        {
            while (number >= value)
            {
                roman.Append(symbol);
                number -= value;
            }
        }

        return roman.ToString();
    }

    private static List<Club> BuildClubs(IReadOnlyList<Team> teams)
    {
        Dictionary<string, Club> bySlug = [];
        List<Club> clubs = [];

        foreach (Team team in teams)
        {
            string slug = SlugGenerator.GenerateSlug(team.Name);

            if (!bySlug.TryGetValue(slug, out Club? club))
            {
                club = new Club
                {
                    CreatedBy = AuditConstants.SystemUser,
                    Name = team.Name,
                    Slug = slug,
                };
                bySlug[slug] = club;
                clubs.Add(club);
            }

            team.Club = club;
            club.Teams.Add(team);
        }

        return clubs;
    }

    private static List<TeamPointDeduction> BuildPointDeductions(
        IReadOnlyList<SampleTournamentBuilder.BuildResult> results)
    {
        (int Points, string Reason)[] specs =
        [
            (2, "Incomparecencia en la jornada 4."),
            (1, "Inclusión de un jugador no habilitado."),
            (3, "Incidentes de la parcialidad local."),
            (1, "Presentación tardía de la planilla."),
        ];

        // Ongoing tournaments first, newest season first. A penalty is only
        // worth demonstrating where the table is still live — the previous
        // order put every deduction on the OLDEST season's zones, so the season
        // actually on screen never showed one. Preferring an ongoing zone also
        // avoids a second-order mismatch: a finished zone's bracket was seeded
        // (exactly like StageService.SeedPlayoffCupsAsync does) from standings
        // WITHOUT deductions, so a penalty there can rank the visible table
        // differently from the cup it fed.
        List<(Division Division, List<Team> Teams)> candidates =
        [
            .. results
                .Select((result, index) => (result, index))
                .OrderBy(entry => entry.result.Tournament.Status == TournamentStatus.Ongoing ? 0 : 1)
                .ThenByDescending(entry => entry.index)
                .SelectMany(entry => entry.result.Tournament.Divisions)
                .Where(d => !d.IsCrossDivisionCup)
                .Select(d => (
                    Division: d,
                    Teams: d.Stages
                        .Where(s => s.StageType == StageType.Group)
                        .SelectMany(s => s.StageTeamMatches)
                        .Select(stm => stm.Team)
                        .OfType<Team>()
                        .ToList()))
                .Where(pair => pair.Teams.Count > 3),
        ];

        List<TeamPointDeduction> deductions = [];

        for (int i = 0; i < specs.Length && i < candidates.Count; i++)
        {
            (Division division, List<Team> teams) = candidates[i];

            deductions.Add(new TeamPointDeduction
            {
                CreatedBy = AuditConstants.SystemUser,
                Division = division,
                Team = teams[^(1 + (i % teams.Count))],
                Points = specs[i].Points,
                Reason = specs[i].Reason,
            });
        }

        return deductions;
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
        await db.TeamStaffs.ExecuteDeleteAsync();
        await db.StageTeamMatches.ExecuteDeleteAsync();
        await db.PlayerTeamRegistrations.ExecuteDeleteAsync();
        await db.TeamTournamentRegistrations.ExecuteDeleteAsync();
        // Matches carry the optional SeriesId FK, so they go before MatchSeries.
        await db.Matches.ExecuteDeleteAsync();
        await db.MatchSeries.ExecuteDeleteAsync();
        await db.Players.ExecuteDeleteAsync();
        await db.DivisionPlayoffMappings.ExecuteDeleteAsync();
        await db.TeamPointDeductions.ExecuteDeleteAsync();
        await db.Stages.ExecuteDeleteAsync();
        await db.Teams.ExecuteDeleteAsync();
        await db.Clubs.ExecuteDeleteAsync();
        await db.Divisions.ExecuteDeleteAsync();
        await db.Tournaments.ExecuteDeleteAsync();
        await db.Seasons.ExecuteDeleteAsync();
        await db.Venues.ExecuteDeleteAsync();
        await db.BlogPosts.ExecuteDeleteAsync();

        logger.LogInformation("Seed reset: existing sample domain data deleted before reseeding.");
    }

    /// <summary>
    /// Uploads a real medical PDF (<paramref name="medicalRecordPath"/>, or
    /// the embedded generic ficha médica when unset — see
    /// <see cref="EmbeddedMedicalRecordResourceName"/>) for every
    /// <c>Approved</c> registration whose file reference is null or a legacy
    /// <see cref="PlayerTeamRegistration.LegacyReferencePrefix"/> ref, so it
    /// stops reading as not-habilitado under Part 2's file-backed rule
    /// (medical-records-storage-eligibility, Part 3). Idempotent (a
    /// new-scheme ref is skipped), resumable (flushed every
    /// <see cref="MedicalRecordSaveBatchSize"/> rows), and failure-tolerant: a
    /// missing/unreadable configured PDF warns and skips the whole step, and a
    /// per-row upload failure warns and continues — this step can never fail
    /// the seed, exactly like <see cref="UploadTeamLogosAsync"/>.
    /// </summary>
    private async Task SeedMedicalRecordsAsync(string? medicalRecordPath)
    {
        bool isConfigured = !string.IsNullOrWhiteSpace(medicalRecordPath);

        byte[] pdf;
        string fileName;
        if (isConfigured)
        {
            try
            {
                if (!File.Exists(medicalRecordPath))
                {
                    // An explicitly configured path that is not there is a
                    // misconfiguration (a typo, a file that moved) — warn and
                    // skip rather than papering over it with the generic one.
                    logger.LogWarning(
                        "Seed medical-record file '{Path}' not found — skipping medical-record seeding.",
                        medicalRecordPath);
                    return;
                }

                pdf = await File.ReadAllBytesAsync(medicalRecordPath!);
                fileName = Path.GetFileName(medicalRecordPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not read seed medical record from '{Path}' — skipping.", medicalRecordPath);
                return;
            }
        }
        else
        {
            // Nothing configured — the normal case, including on a deployed
            // server. Falling back to the generic ficha médica embedded in
            // the assembly keeps the seeded league coherent: without a REAL
            // stored file every Approved registration reads as NOT
            // habilitado, while the same players hold scorer/statistic rows
            // for thousands of played matches — exactly the combination
            // PlayerStatisticService rejects on a real match sheet
            // (HU-57/HU-60).
            pdf = LoadEmbeddedMedicalRecordPdf();
            fileName = PlaceholderMedicalRecordFileName;
            logger.LogInformation(
                "No Seed:MedicalRecordPath configured — seeding the built-in generic ficha médica so "
                + "approved registrations end up habilitado.");
        }

        // Superset filter, EF-translatable (StartsWith on a constant -> LIKE 'medical-records/%').
        // The per-row IsStoredReference check below is the authoritative
        // skip-vs-upload decision — the same predicate the read sites and the
        // approve-time write guard use, so the three can never drift.
        List<PlayerTeamRegistration> candidates = await db.PlayerTeamRegistrations
            .Where(r => r.MedicalRecordStatus == MedicalRecordStatus.Approved
                && (r.MedicalRecordFileUrl == null
                    || r.MedicalRecordFileUrl == ""
                    || r.MedicalRecordFileUrl.StartsWith(PlayerTeamRegistration.LegacyReferencePrefix)))
            .ToListAsync();

        int uploaded = 0;
        int failed = 0;
        int pending = 0;
        foreach (PlayerTeamRegistration registration in candidates)
        {
            if (PlayerTeamRegistration.IsStoredReference(registration.MedicalRecordFileUrl))
            {
                continue;
            }

            try
            {
                using MemoryStream content = new(pdf, writable: false);
                string objectPath = await medicalRecordStorage.StoreAsync(
                    registration.TeamId, registration.PlayerId, fileName, content);

                registration.MedicalRecordFileUrl = objectPath;
                registration.MedicalRecordFileName = fileName;
                uploaded++;
                pending++;
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning(ex,
                    "Failed to upload the seed medical record for player {PlayerId} / team {TeamId} — leaving it without a file.",
                    registration.PlayerId, registration.TeamId);
            }

            if (pending >= MedicalRecordSaveBatchSize)
            {
                await db.SaveChangesAsync();
                pending = 0;
            }
        }

        if (pending > 0)
        {
            await db.SaveChangesAsync();
        }

        logger.LogInformation(
            "Medical-record seed: {Uploaded} uploaded, {Failed} failed, {Total} candidates, from '{Path}'.",
            uploaded, failed, candidates.Count, medicalRecordPath);
    }

    /// <summary>
    /// Reads the generic ficha médica embedded in the assembly (see
    /// <see cref="EmbeddedMedicalRecordResourceName"/>). This is the normal
    /// no-config fallback; <see cref="BuildPlaceholderMedicalRecordPdf"/> only
    /// backstops the (should-never-happen) case where the resource fails to
    /// load, so the seed still never fails on this step.
    /// </summary>
    private static byte[] LoadEmbeddedMedicalRecordPdf()
    {
        try
        {
            using Stream? stream = typeof(DataSeeder).Assembly
                .GetManifestResourceStream(EmbeddedMedicalRecordResourceName);

            if (stream is null)
            {
                return BuildPlaceholderMedicalRecordPdf();
            }

            using MemoryStream buffer = new();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
        catch
        {
            return BuildPlaceholderMedicalRecordPdf();
        }
    }

    /// <summary>
    /// A real, valid one-page PDF built in memory (correct xref table and
    /// offsets, so it opens like any other file). Last-resort fallback for
    /// <see cref="LoadEmbeddedMedicalRecordPdf"/>. Deterministic: the same
    /// bytes on every run.
    /// </summary>
    private static byte[] BuildPlaceholderMedicalRecordPdf()
    {
        const string content =
            "BT /F1 16 Tf 60 760 Td (Ficha medica de ejemplo - Liga Club 12) Tj ET";

        string[] bodies =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] "
                + "/Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
        ];

        StringBuilder pdf = new("%PDF-1.4\n");
        List<int> offsets = [];

        for (int i = 0; i < bodies.Length; i++)
        {
            // Every character written here is ASCII, so the builder's length is
            // also the byte offset the xref table has to point at.
            offsets.Add(pdf.Length);
            pdf.Append(i + 1).Append(" 0 obj\n").Append(bodies[i]).Append("\nendobj\n");
        }

        int xrefOffset = pdf.Length;
        pdf.Append("xref\n0 ").Append(bodies.Length + 1).Append("\n")
            .Append("0000000000 65535 f \n");

        foreach (int offset in offsets)
        {
            pdf.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        pdf.Append("trailer\n<< /Size ").Append(bodies.Length + 1).Append(" /Root 1 0 R >>\n")
            .Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    /// <summary>
    /// Uploads a real PNG crest per team from <paramref name="logosPath"/> using
    /// the same Supabase image path the team endpoints use, replacing each team's
    /// generated SVG crest (<c>SampleTournamentBuilder.BuildCrestDataUri</c>).
    /// Best-effort: a missing folder, no PNGs, or any per-file failure logs a
    /// warning and leaves the generated crest in place — logos never fail the seed. Assignment is deterministic (fixed-seed
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
                    "Seed logos path '{Path}' not found — keeping the generated team crests.", logosPath);
                return;
            }

            files = Directory.GetFiles(logosPath, "*.png");
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex, "Could not read seed logos from '{Path}' — keeping the generated team crests.", logosPath);
            return;
        }

        if (files.Length == 0)
        {
            logger.LogWarning(
                "No PNG logos found in '{Path}' — keeping the generated team crests.", logosPath);
            return;
        }

        int[] order = [.. Enumerable.Range(0, files.Length)];
        Shuffle(order, new Random(LogoShuffleSeed));

        List<IGrouping<string, Team>> byClub = [.. teams.GroupBy(t => t.Name)];

        int uploaded = 0;
        for (int i = 0; i < byClub.Count; i++)
        {
            string file = files[order[i % files.Length]];
            try
            {
                await using FileStream stream = File.OpenRead(file);
                string url = await supabaseHelper.UploadImageAsync<Team>(stream, Path.GetFileName(file));

                foreach (Team team in byClub[i])
                {
                    team.LogoUrl = url;
                    uploaded++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex, "Failed to upload logo '{File}' for club '{Club}' — keeping its generated crest.",
                    file, byClub[i].Key);
            }
        }

        logger.LogInformation(
            "Uploaded {ClubCount} real crests from '{Path}', applied to {Uploaded}/{Total} teams.",
            byClub.Count, logosPath, uploaded, teams.Count);
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

    // Cuerpo técnico (HU-cuerpo-tecnico): 8 plausible Argentine coach/assistant
    // name pairs, cycled by team index so every seeded team gets one DT and
    // one Asistente for its current tournament.
    private static readonly string[] CoachFirstNames =
        ["Carlos", "Fernando", "Miguel", "Diego", "Sergio", "Pablo", "Martín", "Gustavo"];
    private static readonly string[] CoachLastNames =
        ["Gómez", "Rodríguez", "Fernández", "Sosa", "Benítez", "Acosta", "Ibarra", "Peralta"];
    private static readonly string[] AssistantFirstNames =
        ["Javier", "Alejandro", "Nicolás", "Ezequiel", "Federico", "Rodrigo", "Matías", "Emiliano"];
    private static readonly string[] AssistantLastNames =
        ["Coronel", "Duarte", "Aguirre", "Medina", "Bordón", "Cabrera", "Zabala", "Leguizamón"];

    /// <summary>
    /// Builds a DT + Asistente for every seeded team, scoped to the tournament
    /// it was built for. Uses the Team/Tournament navigations (rather than
    /// their ids) because these entities have not been saved yet at this
    /// point, so EF resolves the TeamId/TournamentId FKs via relationship
    /// fixup once the batch is persisted.
    /// </summary>
    private static List<TeamStaff> BuildTeamStaff(SampleTournamentBuilder.BuildResult[] results)
    {
        List<TeamStaff> staff = [];
        int index = 0;

        IEnumerable<(Team Team, Tournament Tournament)> teamsByTournament = results
            .SelectMany(result => result.Tournament.Teams.Select(team => (team, result.Tournament)));

        foreach ((Team team, Tournament tournament) in teamsByTournament)
        {
            string coachName =
                $"{CoachFirstNames[index % CoachFirstNames.Length]} {CoachLastNames[index % CoachLastNames.Length]}";
            string assistantName =
                $"{AssistantFirstNames[index % AssistantFirstNames.Length]} {AssistantLastNames[index % AssistantLastNames.Length]}";

            staff.Add(new TeamStaff
            {
                CreatedBy = Domain.Constants.AuditConstants.SystemUser,
                Team = team,
                Tournament = tournament,
                FullName = coachName,
                Role = TeamStaffRole.Coach,
            });
            staff.Add(new TeamStaff
            {
                CreatedBy = Domain.Constants.AuditConstants.SystemUser,
                Team = team,
                Tournament = tournament,
                FullName = assistantName,
                Role = TeamStaffRole.AssistantCoach,
            });

            index++;
        }

        return staff;
    }

    private static List<Venue> BuildVenues()
    {
        // Real basketball club gyms from Paraná, Entre Ríos, with approximate
        // coordinates around the city (~-31.73, -60.52).
        (string Name, string Address, double Latitude, double Longitude)[] specs =
        [
            ("Estadio Ángel Malvicino (Echagüe)", "Av. Almafuerte, Paraná", -31.7398, -60.5060),
            ("Gimnasio Estudiantes de Paraná", "Gualeguaychú 100, Paraná", -31.7255, -60.5150),
            ("Paraná Rowing Club", "Av. Costanera, Paraná", -31.7150, -60.4890),
            ("Club Sionista", "25 de Mayo, Paraná", -31.7345, -60.5250),
            ("Club Atlético Talleres (Paraná)", "Av. Ramírez, Paraná", -31.7460, -60.5300),
            ("Polideportivo Municipal Paraná", "Parque Urquiza, Paraná", -31.7205, -60.5050),
            ("Gimnasio Central Entrerriano", "Av. Ramírez 2100, Paraná", -31.7502, -60.5188),
            ("Club Recreativo Paraná", "Sarmiento 800, Paraná", -31.7310, -60.5215),
            ("Club Olimpia (Paraná)", "Bertozzi 1500, Paraná", -31.7415, -60.5402),
            ("Club Bancario", "Córdoba 400, Paraná", -31.7290, -60.5285),
            ("Gimnasio Parque Sur", "Av. Zanni, Paraná", -31.7562, -60.5121),
            ("Club Neptunia", "Av. de las Américas, Paraná", -31.7480, -60.5480),
            ("Estadio Ciudad de Concordia", "Av. Monseñor Rösch, Concordia", -31.3930, -58.0209),
            ("Gimnasio Rocamora", "25 de Mayo 200, Concepción del Uruguay", -32.4835, -58.2320),
        ];

        List<Venue> venues = [];
        foreach ((string name, string address, double latitude, double longitude) in specs)
        {
            venues.Add(new Venue
            {
                CreatedBy = Domain.Constants.AuditConstants.SystemUser,
                Name = name,
                Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug(name),
                Address = address,
                PhotoUrl = SampleArtwork.VenuePhotoDataUri(name),
                Latitude = latitude,
                Longitude = longitude,
            });
        }

        return venues;
    }

    private static List<BlogPost> BuildBlogPosts()
    {
        (string Title, string Body)[] posts =
        [
            (
                "La Temporada XXV ya tiene campeones",
                "Se cerró el Apertura de la Liga Club 12 (Paraná) en las categorías masculina y " +
                "femenina. Con la Copa de Oro femenina, las seis copas de las Zonas A, B y C, y la Copa " +
                "Cruzada masculina ya definidas, conocemos a los campeones de la temporada. Mirá el podio " +
                "completo en la sección Campeones."
            ),
            (
                "Torneo Apertura Masculino: así quedaron las Zonas A, B y C",
                "El Torneo Apertura Masculino se jugó en tres zonas — A y B de 10 equipos, C de 13 — todas " +
                "contra todos a ida y vuelta. Los primeros cuatro de cada zona avanzaron a la Copa Oro y " +
                "el resto a la Copa Plata. Repasá las tablas finales y las llaves de playoffs."
            ),
            (
                "Copa Cruzada: la copa cruzada de la temporada",
                "La Copa Cruzada masculina reunió a equipos de las tres zonas en seis grupos (ida y " +
                "vuelta), con los dos primeros de cada grupo clasificando a una llave combinada de 12 " +
                "equipos. Los partidos de la copa se disputan entre semana para no superponerse con las " +
                "zonas del fin de semana."
            ),
            (
                "Arrancó el Clausura: así se juega la segunda mitad del año",
                "Con las zonas del Clausura ya en marcha — dos zonas masculinas de ocho equipos y una zona " +
                "femenina de seis, todas ida y vuelta — la Liga Club 12 encara la segunda mitad del año. " +
                "Las primeras jornadas ya tienen resultado y el resto del fixture está publicado, así que " +
                "podés seguir la tabla en vivo y ver cuándo juega tu equipo."
            ),
            (
                "Los clubes de la liga, temporada por temporada",
                "Cada club de la Liga Club 12 tiene ahora su ficha propia: el historial completo de las " +
                "temporadas que disputó, los planteles de cada año y los torneos en los que participó. " +
                "Es la forma más rápida de ver de dónde viene cada institución más allá de la tabla de " +
                "la temporada en curso."
            ),
            (
                "Fichas médicas: qué necesitás para estar habilitado",
                "Un jugador queda habilitado cuando su ficha médica está aprobada y el archivo cargado en " +
                "el sistema. Desde el panel, cada delegado puede subir la ficha de sus jugadores y seguir " +
                "el estado de la revisión. Los jugadores con ficha pendiente aparecen marcados en el " +
                "plantel para que nadie llegue al partido con una sorpresa."
            ),
            (
                "Sanciones y descuentos de puntos: cómo se aplican",
                "El tribunal de disciplina publica las sanciones a jugadores y clubes junto con la " +
                "cantidad de fechas de suspensión y el estado de cada apelación. Cuando la sanción es " +
                "institucional puede incluir un descuento de puntos, que se resta directamente del total " +
                "del equipo en la tabla de posiciones de su zona."
            ),
            (
                "Goleadores: quiénes lideran la tabla de anotadores",
                "La tabla de goleadores se arma con las planillas de cada partido, así que se actualiza " +
                "apenas se carga un resultado. Podés filtrarla por torneo y por zona para ver quién " +
                "lidera en cada categoría de la temporada."
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
                PhotoUrl = SampleArtwork.BlogCoverDataUri(title),
                MarkdownText = body,
                Views = 0,
            });
        }

        return blogPosts;
    }
}
