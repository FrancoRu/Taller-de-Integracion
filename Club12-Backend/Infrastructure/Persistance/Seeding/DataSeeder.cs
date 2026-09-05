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
/// Seeds the standard sample dataset for the Club 12 basketball league used in local development and demos.
/// </summary>
public sealed class DataSeeder(
    ApplicationDBContext db,
    ILogger<DataSeeder> logger,
    SupabaseHelper supabaseHelper,
    MedicalRecordSeedBackfiller medicalRecordSeedBackfiller)
{
    /// <summary>
    /// Default folder team crest PNGs are read from when Seed:LogosPath is not configured.
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

    // BestOf 3 applies to each cup's SemiFinal and Final only; every earlier round always plays best-of-1.
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

    // TeamPoolSize reuses real rosters from the Zona A, B, and C teams in build order instead of inventing a disjoint set of teams, mirroring how real leagues share club rosters across parallel competitions.
    private static readonly SampleTournamentBuilder.CrossCupDefinition CopaCruzada = new(
        "Copa Cruzada",
        GroupCount: 6,
        QualifiersPerGroup: 2,
        RoundRobinLegs: 2,
        FinalsBestOf: 3,
        TeamPoolSize: 23);

    /// <summary>
    /// Seeds the sample league, skipping the work when any team already exists.
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
            // The medical-records backfill always runs here because it is idempotent and a no-op once every Approved registration already has a real stored file, so a database seeded before this feature existed self-heals into habilitado players on the next startup with no config flag to flip.
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

        // Runs after SaveChangesAsync because TeamId and PlayerId are store-generated, since EntityBase.Id defaults to Guid.Empty, so there is no real object key to build from before this point.
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
    /// Re-sorts the club pool deterministically to produce one season's draw.
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

        // Prefers ongoing tournaments and the newest season first so a penalty lands where the standings table is actually visible, and avoids ranking a finished zone's bracket differently from the cup it fed since that bracket was seeded from standings without deductions.
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
    /// Deletes existing sample domain data in FK-safe order so a reseed starts from a clean slate.
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
    /// Uploads a real PNG crest per team from logosPath, replacing each team's generated SVG crest.
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

    /// <summary>
    /// In-place Fisher-Yates shuffle with a caller-provided RNG.
    /// </summary>
    private static void Shuffle(int[] values, Random random)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    // 8 plausible Argentine coach and assistant name pairs, cycled by team index so every seeded team gets one DT and one Asistente for its current tournament.
    private static readonly string[] CoachFirstNames =
        ["Carlos", "Fernando", "Miguel", "Diego", "Sergio", "Pablo", "Martín", "Gustavo"];
    private static readonly string[] CoachLastNames =
        ["Gómez", "Rodríguez", "Fernández", "Sosa", "Benítez", "Acosta", "Ibarra", "Peralta"];
    private static readonly string[] AssistantFirstNames =
        ["Javier", "Alejandro", "Nicolás", "Ezequiel", "Federico", "Rodrigo", "Matías", "Emiliano"];
    private static readonly string[] AssistantLastNames =
        ["Coronel", "Duarte", "Aguirre", "Medina", "Bordón", "Cabrera", "Zabala", "Leguizamón"];

    /// <summary>
    /// Builds a DT and Asistente staff pair for every seeded team, scoped to the tournament it was built for.
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
        // Real basketball club gyms from Paraná, Entre Ríos, with approximate coordinates around the city near -31.73, -60.52.
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
