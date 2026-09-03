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
/// otherwise a generated placeholder PDF, so the seeded rosters end up
/// habilitado on any machine instead of only on the one the default path
/// points at (a league whose players are all un-habilitado while holding
/// scorer rows contradicts HU-57/HU-60).
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
    MedicalRecordSeedBackfiller medicalRecordSeedBackfiller)
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

    private const int LatestSeasonNumber = 25;
    private const int LatestSeasonYear = 2026;
    private const int MaxSeasonCount = 12;
    private const int MinPlayersPerTeam = 5;
    private const int MaxPlayersPerTeam = 20;

    private const int ClausuraPlayedRounds = 5;

    private const int UpsetPercent = 26;

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
        string? medicalRecordPath = null,
        int seasonCount = 1, int playersPerTeam = SampleTournamentBuilder.DefaultPlayersPerTeam)
    {
        if (reset)
        {
            await ResetSeededDataAsync();
        }
        else if (await db.Teams.AnyAsync())
        {
            // The medical-records backfill (medical-records-storage-eligibility,
            // Part 3) always runs here: it is idempotent and a no-op once every
            // Approved registration already has a real stored file (ADR #8), so
            // a database seeded before this feature existed self-heals into
            // habilitado players on the very next startup, with no config flag
            // to remember to flip.
            logger.LogInformation("Sample data already present — running the medical-records backfill and skipping the rest.");
            await medicalRecordSeedBackfiller.BackfillMedicalRecordsAsync(medicalRecordPath);
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
        await medicalRecordSeedBackfiller.BackfillMedicalRecordsAsync(medicalRecordPath);

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
