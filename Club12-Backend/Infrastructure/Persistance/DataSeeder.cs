using Application.Interfaces.Storage;

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
/// Seeds the app's standard reference dataset for the Club 12 basketball
/// league (Paraná, "liga libre"): one <see cref="Season"/> ("Temporada XXV")
/// grouping two FINISHED tournaments —
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
/// Every playoff bracket is built by <see cref="SampleTournamentBuilder"/>'s
/// generic elimination-bracket seeder (RoundOf16/QuarterFinal/SemiFinal/Final
/// as the seed count needs, byes to the best seeds when not a power of two);
/// every cup's SemiFinal and Final rounds are Best-of-3, every earlier round
/// Best-of-1. Divisions carry their tournament's category (HU-48). Team
/// crests are uploaded from a configurable folder (<c>Seed:LogosPath</c>) via
/// the same Supabase storage path the team endpoints use; any logo failure
/// degrades to a placeholder without ever failing the seed.
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
    /// Default medical PDF read from when <c>Seed:MedicalRecordPath</c> is not
    /// configured. Missing file warns and skips the whole backfill step
    /// (medical-records-storage-eligibility, Part 3).
    /// </summary>
#pragma warning disable S1075 // Dev-only seed default path; overridden by the Seed:MedicalRecordPath config key.
    public const string DefaultMedicalRecordPath = @"C:\Users\Franco\Downloads\ficha-medica-club12.pdf";
#pragma warning restore S1075

    // Fixed seed keeps logo-to-team assignment reproducible across reseeds.
    private const int LogoShuffleSeed = 4212;

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
        string? medicalRecordPath = null, bool forceMedicalRecords = false)
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

        List<Venue> venues = BuildVenues();

        // Both tournaments are FINISHED: group stages fully played and every
        // playoff cup decided, so the Campeones page resolves real champions
        // for both categories.
        DateTime stageStart = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime stageEnd = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        // Torneo Femenino: a single zone, ida y vuelta (RoundRobinLegs: 2),
        // feeding one Copa de Oro with all 7 teams (no Copa Plata).
        SampleTournamentBuilder.TournamentDefinition femenino = new(
            Name: "Torneo Femenino",
            Description: "Torneo Femenino de la Liga Club 12 (Paraná), Temporada XXV. Finalizado.",
            TeamRegistrationDeadline: stageStart.AddDays(-14),
            StartDate: stageStart,
            StageStartDate: stageStart,
            StageEndDate: stageEnd,
            FinishedMatchesStart: stageStart,
            UpcomingMatchesStart: stageEnd,
            Divisions:
            [
                new("Zona Única", FemeninoNames, FemeninoCodes, FemeninoColors, FemeninoCups,
                    TeamStyles: FemeninoStyles, TeamSecondaryColors: FemeninoSecondaryColors),
            ],
            Status: TournamentStatus.Finished,
            Category: TournamentCategory.Feminine,
            RoundRobinLegs: 2);

        // Torneo Masculino: 3 zones, each ida y vuelta (every group stage in
        // the club plays home-and-away, confirmed by the owner), each with
        // its own Copa Oro/Copa Plata, plus the cross-division Copa Cruzada
        // (its own 6-zone group stage, also ida y vuelta, feeding one
        // combined 12-team playoff).
        SampleTournamentBuilder.TournamentDefinition masculino = new(
            Name: "Torneo Masculino",
            Description: "Torneo Masculino de la Liga Club 12 (Paraná), Temporada XXV. Finalizado.",
            TeamRegistrationDeadline: stageStart.AddDays(-14),
            StartDate: stageStart,
            StageStartDate: stageStart,
            StageEndDate: stageEnd,
            FinishedMatchesStart: stageStart,
            UpcomingMatchesStart: stageEnd,
            Divisions:
            [
                new("Zona A", ZonaANames, ZonaACodes, ZonaAColors, ZonaABCups,
                    TeamStyles: ZonaAStyles, TeamSecondaryColors: ZonaASecondaryColors),
                new("Zona B", ZonaBNames, ZonaBCodes, ZonaBColors, ZonaABCups,
                    TeamStyles: ZonaBStyles, TeamSecondaryColors: ZonaBSecondaryColors),
                new("Zona C", ZonaCNames, ZonaCCodes, ZonaCColors, ZonaCCups,
                    TeamStyles: ZonaCStyles, TeamSecondaryColors: ZonaCSecondaryColors),
            ],
            CrossCup: CopaCruzada,
            Status: TournamentStatus.Finished,
            Category: TournamentCategory.Masculine,
            RoundRobinLegs: 2);

        int playerCounter = 0;
        // Both tournaments persist together, so their division/stage slugs
        // must stay unique across the whole batch (each has a DB unique index):
        // one shared registry disambiguates repeated base slugs with numeric
        // suffixes. Team slugs are globally unique too — Femenino and
        // Masculino use entirely distinct club names so no Team.Slug ever
        // collides (Copa Cruzada reuses existing Masculino Team rows, not new
        // ones, so it never introduces a new slug).
        SampleTournamentBuilder.SlugRegistry slugRegistry = new();
        SampleTournamentBuilder.BuildResult femeninoResult =
            SampleTournamentBuilder.Build(femenino, venues, ref playerCounter, includePlayoffs: true, slugRegistry);
        SampleTournamentBuilder.BuildResult masculinoResult =
            SampleTournamentBuilder.Build(masculino, venues, ref playerCounter, includePlayoffs: true, slugRegistry);

        SampleTournamentBuilder.BuildResult[] results = [femeninoResult, masculinoResult];

        // One season groups both tournaments so champions show under a real
        // "Temporada XXV" instead of "Sin temporada". Each tournament keeps
        // its own category (HU-48) — the season only groups them.
        Season temporadaXXV = new()
        {
            CreatedBy = Domain.Constants.AuditConstants.SystemUser,
            Name = "Temporada XXV",
            Slug = Application.Utils.Helper.Slug.SlugGenerator.GenerateSlug("Temporada XXV"),
        };
        foreach (SampleTournamentBuilder.BuildResult result in results)
        {
            result.Tournament.Season = temporadaXXV;
        }

        List<Team> allTeams = [.. results.SelectMany(r => r.Tournament.Teams)];
        await UploadTeamLogosAsync(allTeams, string.IsNullOrWhiteSpace(logosPath) ? DefaultLogosPath : logosPath);

        db.Seasons.Add(temporadaXXV);
        foreach (SampleTournamentBuilder.BuildResult result in results)
        {
            db.Tournaments.Add(result.Tournament);
            db.PlayerSanctions.AddRange(result.Sanctions);
        }
        db.BlogPosts.AddRange(BuildBlogPosts());
        db.TeamStaffs.AddRange(BuildTeamStaff(results));

        await db.SaveChangesAsync();

        // Runs AFTER SaveChangesAsync: TeamId/PlayerId are store-generated
        // (EntityBase.Id defaults to Guid.Empty), so there is no real object
        // key to build from before this point (medical-records-storage-eligibility, ADR #6).
        await SeedMedicalRecordsAsync(medicalRecordPath);

        int teamCount = allTeams.Count;
        int playerCount = allTeams.Sum(t => t.Players.Count);
        int divisionCount = results.Sum(r => r.Tournament.Divisions.Count);
        int sanctionCount = results.Sum(r => r.Sanctions.Count);

        logger.LogInformation(
            "Sample data seeded under season '{Season}': 2 tournaments (Femenino + Masculino, both FINISHED), " +
            "{DivisionCount} divisions (incl. Copa Cruzada), {TeamCount} teams, {PlayerCount} players, " +
            "{SanctionCount} sanctions.",
            temporadaXXV.Name, divisionCount, teamCount, playerCount, sanctionCount);
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
    /// Uploads a real medical PDF (<paramref name="medicalRecordPath"/>, or
    /// <see cref="DefaultMedicalRecordPath"/> when unset) for every
    /// <c>Approved</c> registration whose file reference is null or a legacy
    /// <see cref="PlayerTeamRegistration.LegacyReferencePrefix"/> ref, so it
    /// stops reading as not-habilitado under Part 2's file-backed rule
    /// (medical-records-storage-eligibility, Part 3). Idempotent (a
    /// new-scheme ref is skipped), resumable (flushed every
    /// <see cref="MedicalRecordSaveBatchSize"/> rows), and failure-tolerant: a
    /// missing/unreadable PDF warns and skips the whole step, and a per-row
    /// upload failure warns and continues — this step can never fail the
    /// seed, exactly like <see cref="UploadTeamLogosAsync"/>.
    /// </summary>
    private async Task SeedMedicalRecordsAsync(string? medicalRecordPath)
    {
        string path = string.IsNullOrWhiteSpace(medicalRecordPath) ? DefaultMedicalRecordPath : medicalRecordPath;

        byte[] pdf;
        try
        {
            if (!File.Exists(path))
            {
                logger.LogWarning(
                    "Seed medical-record file '{Path}' not found — skipping medical-record seeding.", path);
                return;
            }

            pdf = await File.ReadAllBytesAsync(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read seed medical record from '{Path}' — skipping.", path);
            return;
        }

        string fileName = Path.GetFileName(path);

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
            uploaded, failed, candidates.Count, path);
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
                "Se cerró la Temporada XXV de la Liga Club 12 (Paraná) en las categorías masculina y " +
                "femenina. Con la Copa de Oro femenina, las seis copas de las Zonas A, B y C, y la Copa " +
                "Cruzada masculina ya definidas, conocemos a los campeones de la temporada. Mirá el podio " +
                "completo en la sección Campeones."
            ),
            (
                "Torneo Masculino: así quedaron las Zonas A, B y C",
                "El Torneo Masculino de la Temporada XXV se jugó en tres zonas — A y B de 10 equipos, C de " +
                "13 — todas contra todos a una rueda. Los primeros cuatro de cada zona avanzaron a la Copa " +
                "Oro, el resto a la Copa Plata. Repasá las tablas finales y las llaves de playoffs."
            ),
            (
                "Copa Cruzada: la copa cruzada de la temporada",
                "La Copa Cruzada masculina reunió a equipos de las tres zonas en seis grupos (ida y " +
                "vuelta), con los dos primeros de cada grupo clasificando a una llave combinada de 12 " +
                "equipos. Los partidos de la copa se disputan entre semana para no superponerse con las " +
                "zonas del fin de semana."
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
