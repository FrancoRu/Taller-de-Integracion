using Application.Utils.Constants.Stage;
using Application.Utils.Helper.Playoff;
using Application.Utils.Helper.RoundRobin;
using Application.Utils.Helper.Series;
using Application.Utils.Helper.Slug;
using Application.Utils.Helper.Standings;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Infrastructure.Persistance;

/// <summary>
/// Builds one fully-populated, coherent sample Tournament (divisions, teams,
/// players, group stages with proper round-robin jornadas and decisive scores,
/// scorers/statistics, position-range playoff cups, an optional
/// cross-division cup, and sanctions) from a declarative definition. Shared by
/// the startup DataSeeder and DataMaintenanceService (the admin-triggered
/// sample reseed) so the construction logic exists once.
///
/// Coherence guarantees:
/// - Every group stage is a COMPLETE circle-method round-robin: every pair meets
///   exactly once per leg, every team plays the same number of games, and a team
///   plays at most once per jornada (with an odd roster exactly one team is idle
///   — "libre" — each jornada). <see cref="Match.Round"/> is the 1-based
///   jornada, <see cref="Stage.EndDate"/> is pushed out to the last jornada the
///   fixture actually needs, and <see cref="Match.MatchDate"/> comes from
///   <see cref="RoundCalendar.DateForRound(DateTime, int, bool)"/> (regular
///   zones on Sundays, cross-division cups on Wednesdays — HU-111, so a team's
///   zone and cup jornadas never collide).
/// - Playoff cups (e.g. Copa Oro 1-4, Copa Plata 5-8) are seeded from the
///   REAL final group standings via <see cref="PositionCalculator"/> and
///   <see cref="PlayoffSeeder"/>.
/// - The cross-division cup pools its group winners via
///   <see cref="CrossCupGroupSeeder"/>. Its teams keep their regular zone AND
///   join a cup group (cross cups are exempt from one-team-one-zone).
/// - Only players a real match sheet would accept are seeded as scorers: their
///   registration is Approved and they are not serving a sanction
///   (HU-57/HU-60/HU-61, the rule PlayerStatisticService enforces). A suspended
///   player's points in the games he must miss are handed to a team-mate, so a
///   team's scorers still add up to its recorded score.
/// </summary>
public static class SampleTournamentBuilder
{
    private const string CreatedBy = AuditConstants.SystemUser;

    // Neither a fake file reference NOR a file NAME is assigned to Approved
    // seeded registrations (medical-records-storage-eligibility, Part 3):
    // DataSeeder.SeedMedicalRecordsAsync fills MedicalRecordFileUrl AND
    // MedicalRecordFileName together with a REAL uploaded object after Build()
    // runs. Leaving both null here means an Approved registration correctly
    // reads as NOT habilitado (Part 2's file-backed rule) until the seed's
    // backfill step gives it a real file, and never shows a file name that
    // resolves to nothing.

    public const int DefaultPlayersPerTeam = 8;

    // Offset mixed into each division's variety seed so two same-sized zones of
    // one tournament don't replay the same season (see Build).
    private const int DivisionVarietySalt = 7717;
    private static readonly DateTime SampleMedicalRecordReviewedAt =
        new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly string[] FirstNames =
    [
        "Juan", "Carlos", "Martín", "Diego", "Facundo", "Lucas", "Nicolás", "Matías",
        "Franco", "Ezequiel", "Agustín", "Bruno", "Iván", "Santiago", "Tomás", "Gonzalo",
        "Joaquín", "Valentín", "Emiliano", "Thiago",
    ];

    private static readonly string[] LastNames =
    [
        "González", "Rodríguez", "Fernández", "López", "Díaz", "Pérez", "Sánchez", "Romero",
        "Álvarez", "Torres", "Ruiz", "Ramírez", "Flores", "Acosta", "Benítez", "Medina",
        "Cabrera", "Sosa", "Vera", "Ledesma", "Quiroga",
    ];

    /// <summary>
    /// A named playoff cup fed by a contiguous range of a division's final
    /// group-standings positions (HU-45), e.g. "Copa Oro" for positions 1-4.
    /// Produces a <see cref="DivisionPlayoffMapping"/> row plus a full
    /// single-elimination bracket (as many rounds as the seed count needs —
    /// RoundOf16/QuarterFinal/SemiFinal/Final, byes to the best seeds when not
    /// a power of two, via <see cref="SeedEliminationBracket"/>) whose stages
    /// carry <see cref="Stage.BracketName"/> = <paramref name="BracketName"/>.
    /// <paramref name="BestOf"/> applies to the SemiFinal AND Final rounds
    /// only — any earlier round (needed once a cup has more than 4 real
    /// seeds) always plays Bo1.
    /// </summary>
    public sealed record PlayoffCupDefinition(
        string BracketName,
        int FromPosition,
        int ToPosition,
        int BestOf);

    /// <summary>
    /// The cross-division cup ("Copa Cruzada"): an extra division with
    /// <see cref="Division.IsCrossDivisionCup"/> = true whose teams are drawn
    /// from the tournament's teams, split into <paramref name="GroupCount"/>
    /// internal groups. The top <paramref name="QualifiersPerGroup"/> of each
    /// group are pooled into one bracket (HU-110).
    /// </summary>
    /// <param name="DivisionName">The cup's division name, e.g. "Copa Cruzada".</param>
    /// <param name="GroupCount">How many internal group stages to split the team pool into.</param>
    /// <param name="QualifiersPerGroup">How many top teams from each group pool into the bracket.</param>
    /// <param name="RoundRobinLegs">
    /// How many times each pair plays within an internal group (1 = single
    /// round-robin, 2 = double/ida y vuelta). Defaults to 1.
    /// </param>
    /// <param name="FinalsBestOf">
    /// BestOf applied to the pooled bracket's SemiFinal and Final rounds only
    /// (earlier rounds, needed once more than 4 teams are pooled, always play
    /// Bo1). Defaults to 1.
    /// </param>
    /// <param name="TeamPoolSize">
    /// When set, only the first <paramref name="TeamPoolSize"/> of the
    /// tournament's teams (in build order) feed the cup, instead of every
    /// team — e.g. a masculine tournament reusing a subset of its zone teams
    /// for a separate cross-division cup. Null (the default) uses every team,
    /// unchanged from the original behavior.
    /// </param>
    public sealed record CrossCupDefinition(
        string DivisionName,
        int GroupCount,
        int QualifiersPerGroup,
        int RoundRobinLegs = 1,
        int FinalsBestOf = 1,
        int? TeamPoolSize = null);

    public sealed record DivisionDefinition(
        string DivisionName,
        string[] TeamNames,
        string[] TeamCodes,
        string[] TeamColors,
        PlayoffCupDefinition[]? PlayoffCups = null,
        string[]? TeamStyles = null,
        string[]? TeamSecondaryColors = null);

    public sealed record TournamentDefinition(
        string Name,
        string Description,
        DateTime TeamRegistrationDeadline,
        DateTime StartDate,
        DateTime StageStartDate,
        DateTime StageEndDate,
        DivisionDefinition[] Divisions,
        CrossCupDefinition? CrossCup = null,
        TournamentStatus Status = TournamentStatus.Ongoing,
        TournamentCategory Category = TournamentCategory.Masculine,
        int RoundRobinLegs = 1,
        int? PlayedRoundsPerZone = null,
        int PlayersPerTeam = DefaultPlayersPerTeam,
        int UpsetPercent = 0,
        int VarietySeed = 0);

    public sealed record BuildResult(Tournament Tournament, List<PlayerSanction> Sanctions);

    /// <summary>
    /// Hands out clean, collision-free kebab-case slugs for the divisions and
    /// stages built in one seeding run. Because the whole object graph is built
    /// in memory before it is saved, there is no repository to ask "does this
    /// slug already exist?"; instead every issued slug is remembered per table
    /// so a repeated base slug gets the same numeric suffix (-2, -3, ...) that
    /// <see cref="SlugGenerator.GenerateUniqueSlugAsync"/> would apply against a
    /// real database. Division and Stage slugs are tracked separately because
    /// each has its own unique index. Share ONE instance across every
    /// <see cref="Build"/> call that is persisted together (e.g. the two sample
    /// tournaments the DataMaintenanceService saves in a single transaction) so
    /// slugs stay unique across the whole batch, never colliding on the DB's
    /// unique index — and never falling back to a GUID.
    /// </summary>
    public sealed class SlugRegistry
    {
        private readonly HashSet<string> _divisionSlugs = [];
        private readonly HashSet<string> _stageSlugs = [];
        private readonly HashSet<string> _playerSlugs = [];
        private readonly HashSet<string> _teamSlugs = [];
        private readonly HashSet<string> _tournamentSlugs = [];

        public string ForDivision(string source) => Register(source, _divisionSlugs);

        public string ForStage(string source) => Register(source, _stageSlugs);

        public string ForPlayer(string source) => Register(source, _playerSlugs);

        public string ForTeam(string source) => Register(source, _teamSlugs);

        public string ForTournament(string source) => Register(source, _tournamentSlugs);

        private static string Register(string source, HashSet<string> used)
        {
            string baseSlug = SlugGenerator.GenerateSlug(source);
            string candidate = baseSlug;
            int suffix = 1;

            while (!used.Add(candidate))
            {
                suffix++;
                candidate = $"{baseSlug}-{suffix}";
            }

            return candidate;
        }
    }

    /// <summary>
    /// Builds one Tournament with every division in <paramref name="definition"/>.
    /// <paramref name="playerCounter"/> is threaded through (and must keep
    /// incrementing) across multiple calls so player names/document numbers
    /// never collide between tournaments built in the same seeding run.
    /// <paramref name="includePlayoffs"/> opts each division into its playoff
    /// bracket(s) built from the group stage's final standings, and — when the
    /// definition declares a <see cref="TournamentDefinition.CrossCup"/> — into
    /// the cross-division cup. When false, each division gets only its group
    /// stage (used for an in-progress/ONGOING tournament that has no decided
    /// playoffs yet).
    /// </summary>
    public static BuildResult Build(
        TournamentDefinition definition,
        List<Venue> venues,
        ref int playerCounter,
        bool includePlayoffs = false,
        SlugRegistry? slugRegistry = null)
    {
        slugRegistry ??= new SlugRegistry();

        Tournament tournament = new()
        {
            CreatedBy = CreatedBy,
            Name = definition.Name,
            Slug = slugRegistry.ForTournament(definition.Name),
            Description = definition.Description,
            TeamRegistrationDeadline = definition.TeamRegistrationDeadline,
            StartDate = definition.StartDate,
            Status = definition.Status,
            // Competitive category (HU-48): every division built below inherits
            // it so the "one tournament, one category" invariant holds in the
            // seeded graph.
            Category = definition.Category,
            Divisions = [],
            Teams = [],
        };

        List<Team> allTeams = [];
        List<Stage> regularGroupStages = [];

        for (int divisionIndex = 0; divisionIndex < definition.Divisions.Length; divisionIndex++)
        {
            DivisionDefinition divisionDef = definition.Divisions[divisionIndex];

            // Every result this zone produces (upset draws, margins, scores) is a
            // pure function of the seed, so two same-sized zones of one
            // tournament shared a seed and played out identically — Zona A and
            // Zona B ended with the very same scorelines and the very same final
            // table, team names aside. Salting per division separates them.
            int divisionVarietySeed = definition.VarietySeed + (divisionIndex * DivisionVarietySalt);

            (Division division, List<Team> teams) = BuildDivisionWithTeams(
                tournament,
                divisionDef.DivisionName,
                divisionDef.TeamNames,
                divisionDef.TeamCodes,
                divisionDef.TeamColors,
                divisionDef.TeamStyles,
                divisionDef.TeamSecondaryColors,
                slugRegistry,
                definition.PlayersPerTeam,
                ref playerCounter);

            division.Category = definition.Category;
            tournament.Divisions.Add(division);
            foreach (Team team in teams)
            {
                tournament.Teams.Add(team);
                allTeams.Add(team);
            }

            Stage stage = new()
            {
                CreatedBy = CreatedBy,
                Name = StageTemplate.Group.Name,
                Slug = slugRegistry.ForStage($"{StageTemplate.Group.Name} {division.Name}"),
                StageType = StageType.Group,
                IsActive = true,
                StartDate = definition.StageStartDate,
                EndDate = definition.StageEndDate,
                DivisionId = Guid.Empty,
                Division = division,
                Matches = [],
                Order = 0,
                RoundRobinLegs = definition.RoundRobinLegs,
            };
            division.Stages.Add(stage);
            regularGroupStages.Add(stage);
            AddStageTeamMatches(stage, teams);

            SeedRoundRobinMatches(
                stage, teams, venues, definition.StageStartDate, isCrossDivisionCup: false,
                legs: definition.RoundRobinLegs, playedRounds: definition.PlayedRoundsPerZone,
                upsetPercent: definition.UpsetPercent, varietySeed: divisionVarietySeed);

            if (includePlayoffs)
            {
                if (divisionDef.PlayoffCups is { Length: > 0 } cups)
                {
                    SeedCupPlayoffs(division, stage, teams, venues, cups, slugRegistry);
                }
                else
                {
                    SeedPlayoffStages(division, stage, teams, venues, slugRegistry);
                }
            }
        }

        if (includePlayoffs && definition.CrossCup is not null)
        {
            List<Team> crossCupTeams = definition.CrossCup.TeamPoolSize is int poolSize
                ? [.. allTeams.Take(poolSize)]
                : allTeams;
            SeedCrossDivisionCup(
                tournament, definition.CrossCup, crossCupTeams, venues, definition.StageStartDate, slugRegistry,
                definition.UpsetPercent, definition.VarietySeed);
        }

        List<PlayerSanction> sanctions = SeedSanctions(regularGroupStages, tournament);

        return new BuildResult(tournament, sanctions);
    }

    private static (Division Division, List<Team> Teams) BuildDivisionWithTeams(
        Tournament tournament,
        string divisionName,
        string[] teamNames,
        string[] teamCodes,
        string[] teamColors,
        string[]? teamStyles,
        string[]? teamSecondaryColors,
        SlugRegistry slugRegistry,
        int playersPerTeam,
        ref int playerCounter)
    {
        Division division = new()
        {
            CreatedBy = CreatedBy,
            Name = divisionName,
            Slug = slugRegistry.ForDivision(divisionName),
            Tournament = tournament,
            Stages = [],
        };

        List<Team> teams = [];

        for (int i = 0; i < teamNames.Length; i++)
        {
            string jerseyStyle = teamStyles is not null && i < teamStyles.Length ? teamStyles[i] : "solid";
            string? secondaryColor =
                teamSecondaryColors is not null && i < teamSecondaryColors.Length ? teamSecondaryColors[i] : null;

            Team team = new()
            {
                Id = Guid.NewGuid(),
                CreatedBy = CreatedBy,
                Name = teamNames[i],
                Slug = slugRegistry.ForTeam(teamNames[i]),
                ThreeLetterCode = teamCodes[i],
                LogoUrl = BuildCrestDataUri(teamCodes[i], teamColors[i], secondaryColor, jerseyStyle),
                ShirtColor = teamColors[i],
                JerseyStyle = jerseyStyle,
                ShirtSecondaryColor = secondaryColor,
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

            for (int p = 0; p < playersPerTeam; p++)
            {
                playerCounter++;

                string firstName = FirstNames[playerCounter % FirstNames.Length];
                string lastName = LastNames[playerCounter % LastNames.Length];
                string documentNumber = (30000000 + playerCounter).ToString();

                Player player = new()
                {
                    CreatedBy = CreatedBy,
                    FirstName = firstName,
                    LastName = lastName,
                    Slug = slugRegistry.ForPlayer(Player.BuildSlugSource(lastName, firstName, secondName: null)),
                    DocumentNumber = documentNumber,
                    IsSanctioned = false,
                    BirthDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc)
                        .AddYears(-(18 + (playerCounter % 20)))
                        .AddDays(playerCounter % 27),
                    SocialSecurity = $"20-{documentNumber}-3",
                    Team = team,
                };

                team.Players.Add(player);

                // Most seeded players' ficha is Approved so the sample data
                // showcases that status; the last player of each team stays
                // Pending (no ficha) to keep the upload/review flow demonstrable.
                // Approved rows are seeded WITHOUT a file reference
                // (medical-records-storage-eligibility, Part 3): DataSeeder.SeedMedicalRecordsAsync
                // fills MedicalRecordFileUrl with a REAL uploaded object after
                // Build() runs, so between Build() and that step an Approved row
                // correctly reads as NOT habilitado under Part 2's file-backed rule.
                bool isHabilitado = p < playersPerTeam - 1;

                team.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
                {
                    CreatedBy = CreatedBy,
                    PlayerId = Guid.Empty,
                    Player = player,
                    TeamId = Guid.Empty,
                    Team = team,
                    TournamentId = Guid.Empty,
                    Tournament = tournament,
                    JerseyNumber = p + 4,
                    MedicalRecordStatus = isHabilitado
                        ? MedicalRecordStatus.Approved
                        : MedicalRecordStatus.Pending,
                    MedicalRecordFileUrl = null,
                    MedicalRecordFileName = null,
                    MedicalRecordReviewedAt = isHabilitado ? SampleMedicalRecordReviewedAt : null,
                });
            }

            teams.Add(team);
        }

        return (division, teams);
    }

    /// <summary>
    /// The team's crest, generated from its own kit: a round badge in the club's
    /// shirt colour carrying its three-letter code, with a band in the secondary
    /// colour following the shirt's <paramref name="jerseyStyle"/>. Returned as a
    /// self-contained <c>data:</c> SVG URI, so a seeded league has distinct,
    /// offline crests without a logos folder, an upload, or a call to an external
    /// placeholder service. <c>DataSeeder.UploadTeamLogosAsync</c> still replaces
    /// it with a real PNG when <c>Seed:LogosPath</c> holds one.
    /// </summary>
    private static string BuildCrestDataUri(
        string code, string primaryColor, string? secondaryColor, string jerseyStyle)
    {
        string secondary = string.IsNullOrWhiteSpace(secondaryColor) ? "#FFFFFF" : secondaryColor;
        string ink = ContrastInk(primaryColor);

        // Each band is drawn across the whole square and clipped to the badge, so
        // a style only has to describe its own shape.
        string band = jerseyStyle switch
        {
            "stripes" => $"<path d='M34 0h20v128H34zM74 0h20v128H74z' fill='{secondary}'/>",
            "hoops" => $"<path d='M0 34h128v20H0zM0 74h128v20H0z' fill='{secondary}'/>",
            "diagonal" => $"<path d='M0 128L128 0v34L34 128z' fill='{secondary}'/>",
            "sash" => $"<path d='M0 96L96 0h26L0 122z' fill='{secondary}'/>",
            "halves" => $"<path d='M64 0h64v128H64z' fill='{secondary}'/>",
            "sides" => $"<path d='M0 0h26v128H0zM102 0h26v128h-26z' fill='{secondary}'/>",
            "chevron" => $"<path d='M64 34l44 44v26L64 60 20 104V78z' fill='{secondary}'/>",
            "circles" => $"<circle cx='64' cy='64' r='40' fill='none' stroke='{secondary}' stroke-width='12'/>",
            "vneck" => $"<path d='M20 0h88L64 52z' fill='{secondary}'/>",
            "gradient" => "<rect width='128' height='128' fill='url(#g)'/>",
            _ => string.Empty,
        };

        string gradient = jerseyStyle == "gradient"
            ? "<linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>"
                + $"<stop offset='0' stop-color='{primaryColor}'/><stop offset='1' stop-color='{secondary}'/>"
                + "</linearGradient>"
            : string.Empty;

        string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 128 128' width='128' height='128'>"
            + $"<defs>{gradient}<clipPath id='c'><circle cx='64' cy='64' r='58'/></clipPath></defs>"
            + $"<g clip-path='url(#c)'><rect width='128' height='128' fill='{primaryColor}'/>{band}</g>"
            + $"<circle cx='64' cy='64' r='58' fill='none' stroke='{ink}' stroke-width='4' stroke-opacity='0.55'/>"
            + "<text x='64' y='80' text-anchor='middle' font-family='Helvetica,Arial,sans-serif' "
            + $"font-size='38' font-weight='700' fill='{ink}'>{code}</text>"
            + "</svg>";

        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
    }

    /// <summary>Near-black ink, the alternative to white for a crest's code.</summary>
    private const string DarkInk = "#0F172A";

    /// <summary>
    /// Whichever of white and <see cref="DarkInk"/> reads better on
    /// <paramref name="backgroundColor"/> (an "#RRGGBB" shirt colour), by actual
    /// WCAG contrast ratio rather than a hand-picked luminance threshold — the
    /// mid greens and cyans in the club palette sit right where a threshold
    /// guesses wrong and would leave a code at ~1.8:1 on its own shirt colour.
    /// </summary>
    private static string ContrastInk(string backgroundColor)
    {
        double? luminance = RelativeLuminance(backgroundColor);
        if (luminance is not double background)
        {
            return "#FFFFFF";
        }

        double onDark = (background + 0.05) / ((RelativeLuminance(DarkInk) ?? 0) + 0.05);
        double onWhite = 1.05 / (background + 0.05);

        return onDark >= onWhite ? DarkInk : "#FFFFFF";
    }

    /// <summary>
    /// WCAG relative luminance of an "#RRGGBB" colour (channels linearised out
    /// of sRGB first), or null when the string is not one.
    /// </summary>
    private static double? RelativeLuminance(string color)
    {
        if (color.Length != 7
            || color[0] != '#'
            || !int.TryParse(color.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
        {
            return null;
        }

        double red = Linearise(((rgb >> 16) & 0xFF) / 255.0);
        double green = Linearise(((rgb >> 8) & 0xFF) / 255.0);
        double blue = Linearise((rgb & 0xFF) / 255.0);

        return (0.2126 * red) + (0.7152 * green) + (0.0722 * blue);

        static double Linearise(double channel) =>
            channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// Builds a real circle-method single round-robin for
    /// <paramref name="teams"/>: for an even N there are N-1 jornadas of N/2
    /// matches each and every team plays once per jornada; for an odd N there
    /// are N jornadas and exactly one team is idle ("libre") per jornada. Either
    /// way every pair meets exactly once per leg and every team plays the same
    /// number of games. Every match is finished with a decisive (never-tied) score
    /// in which the stronger team (earlier in <paramref name="teams"/>) wins,
    /// so <see cref="PositionCalculator"/> yields a full, sensible table.
    /// <see cref="Match.Round"/> is the 1-based jornada and
    /// <see cref="Match.MatchDate"/> is the calendar date for that jornada
    /// (Sundays for zones, Wednesdays for cross-division cups — HU-111).
    ///
    /// <paramref name="legs"/> repeats the whole schedule (2 = a home-and-away
    /// double round-robin, "ida y vuelta"): each extra leg replays the same
    /// pairings with home/away inverted and its jornadas numbered after the
    /// previous leg's, so a mid-season tournament can have many jornadas.
    /// <paramref name="playedRounds"/>, when set, is the number of leading
    /// jornadas that are FINISHED (with scores); every later jornada is seeded
    /// as an UPCOMING (unplayed) match on a future date, so an in-progress
    /// tournament shows both a live standings table and a "Próximos" fixture.
    /// Null (the default) leaves every jornada finished.
    ///
    /// <paramref name="upsetPercent"/> opts into realistic tables: with 0 (the
    /// default) the stronger team wins every single game, so the final
    /// standings are a perfect staircase of the seeding order. Above 0 it is
    /// the base chance that a game goes the other way, damped by how far apart
    /// the two teams are seeded — so favourites still finish on top, but every
    /// team drops and steals games and no record is a clean N-0. The draw is a
    /// deterministic hash of the fixture plus <paramref name="varietySeed"/>,
    /// so a given seed always replays the same season.
    /// </summary>
    private static void SeedRoundRobinMatches(
        Stage stage,
        List<Team> teams,
        List<Venue> venues,
        DateTime anchorDate,
        bool isCrossDivisionCup,
        int legs = 1,
        int? playedRounds = null,
        int upsetPercent = 0,
        int varietySeed = 0)
    {
        int n = teams.Count;
        if (n < 2)
        {
            return;
        }

        // An odd roster is padded with a bye slot so the circle method still
        // produces a COMPLETE round-robin: N odd teams need N jornadas per leg
        // with exactly one team idle ("libre") each jornada — the same shape
        // RoundRobinScheduler builds for the real fixture generator. Running
        // n-1 jornadas on an odd roster instead dropped pairings entirely,
        // replayed others twice per leg, and left the team pinned to slot 0
        // with one extra game (and, with PointsForLoss > 0, free table points).
        const int ByeSlot = -1;
        int slotCount = n % 2 == 0 ? n : n + 1;
        int roundsPerLeg = slotCount - 1;
        int matchIndex = 0;
        DateTime lastMatchDate = stage.StartDate;

        for (int leg = 0; leg < legs; leg++)
        {
            // Circle method: index 0 stays fixed, the remaining slots rotate one
            // position each round. `slots` holds the ORIGINAL team indices in
            // seeding order, so a smaller index means a stronger team. It resets
            // at the start of each leg so the return leg replays the same pairings.
            int[] slots = slotCount == n
                ? [.. Enumerable.Range(0, n)]
                : [.. Enumerable.Range(0, n), ByeSlot];

            for (int r = 0; r < roundsPerLeg; r++)
            {
                // Global 1-based jornada number across all legs.
                int round = (leg * roundsPerLeg) + r + 1;
                DateTime roundDate = RoundCalendar.DateForRound(anchorDate, round, isCrossDivisionCup);
                bool isUpcoming = playedRounds is int played && round > played;

                for (int i = 0; i < slotCount / 2; i++)
                {
                    int first = slots[i];
                    int second = slots[slotCount - 1 - i];

                    // The team drawn against the padding sits this jornada out.
                    if (first == ByeSlot || second == ByeSlot)
                    {
                        continue;
                    }

                    // Alternate home/away by the jornada WITHIN the leg, then
                    // invert it on the return leg so the rematch swaps venue.
                    // A pairing that meets in round r of the first leg meets
                    // again in round r of the second, so deriving this from the
                    // GLOBAL jornada number cancelled the leg inversion whenever
                    // roundsPerLeg was odd (every even-sized zone) and both legs
                    // ended up being played at the same team's home.
                    bool homeIsFirst = r % 2 == 0;
                    if (leg % 2 == 1)
                    {
                        homeIsFirst = !homeIsFirst;
                    }

                    (int homeIdx, int visitorIdx) = homeIsFirst ? (first, second) : (second, first);

                    Team home = teams[homeIdx];
                    Team visitor = teams[visitorIdx];

                    if (isUpcoming)
                    {
                        stage.Matches.Add(BuildUpcomingMatch(
                            stage, home, visitor, MatchType.Regular, venues, roundDate, matchIndex, round));
                    }
                    else
                    {
                        bool isUpset = IsUpset(
                            upsetPercent, varietySeed, stage.Order, round, homeIdx, visitorIdx);
                        bool homeIsStronger = isUpset ? homeIdx > visitorIdx : homeIdx < visitorIdx;

                        int marginFloor = isUpset ? 1 : 2;
                        int margin = upsetPercent > 0
                            ? marginFloor + ((homeIdx + visitorIdx + matchIndex + varietySeed) % 13)
                            : 4 + ((homeIdx + visitorIdx + matchIndex) % 9);
                        int winnerScore = upsetPercent > 0
                            ? 63 + (((matchIndex * 7) + round + varietySeed) % 29)
                            : 68 + ((matchIndex * 5) % 22);
                        int loserScore = winnerScore - margin;
                        int homeScore = homeIsStronger ? winnerScore : loserScore;
                        int visitorScore = homeIsStronger ? loserScore : winnerScore;

                        stage.Matches.Add(BuildFinishedMatch(
                            stage, home, visitor, homeScore, visitorScore, MatchType.Regular,
                            venues, roundDate, matchIndex, round));
                    }

                    if (roundDate > lastMatchDate)
                    {
                        lastMatchDate = roundDate;
                    }

                    matchIndex++;
                }

                Rotate(slots);
            }
        }

        // A stage can never end before its own last jornada. With many teams and
        // two legs the fixture runs past the caller's nominal end date, and the
        // playoff brackets anchor on Stage.EndDate — so a short end date dated
        // cup games BEFORE the zone's final jornadas were even played.
        if (lastMatchDate > stage.EndDate)
        {
            stage.EndDate = lastMatchDate;
        }
    }

    /// <summary>Rotates slots 1..n-1 by one position (index 0 fixed).</summary>
    private static void Rotate(int[] slots)
    {
        int n = slots.Length;
        if (n <= 2)
        {
            return;
        }

        int last = slots[n - 1];
        for (int k = n - 1; k >= 2; k--)
        {
            slots[k] = slots[k - 1];
        }
        slots[1] = last;
    }

    /// <summary>
    /// Builds SemiFinal -> ThirdPlace -> Final stages for one division, seeded
    /// from the (fully finished) group stage's standings. Used for the smaller
    /// historical tournaments that have no position-range cups. With 4 teams
    /// the bracket is SemiFinal(2) -> ThirdPlace(1) + Final(1).
    /// </summary>
    private static void SeedPlayoffStages(Division division, Stage groupStage, List<Team> teams, List<Venue> venues, SlugRegistry slugRegistry)
    {
        List<Position> standings = PositionCalculator.CalculatePositions(groupStage.Matches);
        Dictionary<Guid, Team> teamsById = teams.ToDictionary(t => t.Id);
        List<Guid> orderedTeamIds = [.. standings.Select(p => p.TeamId)];
        List<(Guid HomeTeamId, Guid? VisitorTeamId)> semiFinalPairs = PlayoffSeeder.SeedPairs(orderedTeamIds);

        Stage semiFinalStage = new()
        {
            CreatedBy = CreatedBy,
            Name = StageTemplate.SemiFinal.Name,
            Slug = slugRegistry.ForStage($"{StageTemplate.SemiFinal.Name} {division.Name}"),
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

            Match match = BuildFinishedMatch(
                semiFinalStage, home, visitor, homeScore, visitorScore, MatchType.Playoff,
                venues, semiFinalStage.StartDate.AddDays(i * 2), i, round: null);
            semiFinalStage.Matches.Add(match);

            semiFinalWinners.Add(match.WinningTeam!);
            semiFinalLosers.Add(match.WinningTeam == home ? visitor : home);
        }

        Stage thirdPlaceStage = new()
        {
            CreatedBy = CreatedBy,
            Name = StageTemplate.ThirdPlace.Name,
            Slug = slugRegistry.ForStage($"{StageTemplate.ThirdPlace.Name} {division.Name}"),
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

        Match thirdPlaceMatch = BuildFinishedMatch(
            thirdPlaceStage, semiFinalLosers[0], semiFinalLosers[1], 74, 61, MatchType.Playoff,
            venues, thirdPlaceStage.StartDate, 0, round: null);
        thirdPlaceStage.Matches.Add(thirdPlaceMatch);

        Stage finalStage = new()
        {
            CreatedBy = CreatedBy,
            Name = StageTemplate.Final.Name,
            Slug = slugRegistry.ForStage($"{StageTemplate.Final.Name} {division.Name}"),
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

        Match finalMatch = BuildFinishedMatch(
            finalStage, semiFinalWinners[0], semiFinalWinners[1], 82, 70, MatchType.Playoff,
            venues, finalStage.StartDate, 0, round: null);
        finalStage.Matches.Add(finalMatch);
    }

    /// <summary>
    /// Builds one full elimination bracket per named position-range cup (e.g.
    /// Copa Oro for positions 1-4, Copa Plata for 5-8 or a wider range that
    /// needs byes), each seeded from the REAL final group standings restricted
    /// to the cup's position range via <see cref="SeedEliminationBracket"/>,
    /// and registers the matching <see cref="DivisionPlayoffMapping"/> on the
    /// division. Each cup's stages carry <see cref="Stage.BracketName"/> and
    /// their names embed the cup so they stay unique within the division.
    /// </summary>
    private static void SeedCupPlayoffs(
        Division division,
        Stage groupStage,
        List<Team> teams,
        List<Venue> venues,
        PlayoffCupDefinition[] cups,
        SlugRegistry slugRegistry)
    {
        List<Position> standings = PositionCalculator.CalculatePositions(
            groupStage.Matches, division.PointsForWin, division.PointsForLoss);
        Dictionary<Guid, Team> teamsById = teams.ToDictionary(t => t.Id);

        int order = 1;

        foreach (PlayoffCupDefinition cup in cups)
        {
            division.PlayoffMappings.Add(new DivisionPlayoffMapping
            {
                CreatedBy = CreatedBy,
                DivisionId = Guid.Empty,
                Division = division,
                FromPosition = cup.FromPosition,
                ToPosition = cup.ToPosition,
                Destination = cup.BracketName,
            });

            // Positions are 1-based and inclusive; standings is 0-based best-first.
            List<Guid> seedIds = [.. standings
                .Skip(cup.FromPosition - 1)
                .Take(cup.ToPosition - cup.FromPosition + 1)
                .Select(p => p.TeamId)];

            SeedEliminationBracket(
                division, groupStage.EndDate.AddDays(StageTemplate.StandardGapDays),
                seedIds, teamsById, venues, cup.BracketName, cup.BestOf, slugRegistry, ref order);
        }
    }

    /// <summary>
    /// Builds a full single-elimination bracket for <paramref name="seedIds"/>
    /// (ordered best seed first — a cup's position range, or a cross-cup's
    /// pooled qualifiers), adding every round's <see cref="Stage"/> to
    /// <paramref name="division"/>. Round 1 is seeded via
    /// <see cref="PlayoffSeeder.SeedPairs"/> (byes to the best seeds when the
    /// pool is not a power of two); every later round pairs consecutive
    /// winners in bracket-slot order — always bye-free, since padding only
    /// ever applies to round 1. Stage naming/type follows the bracket size:
    /// RoundOf16 (up to 16 seeds) -&gt; QuarterFinal (8) -&gt; SemiFinal (4)
    /// -&gt; Final (2) — only the rounds a pool of this size actually needs
    /// are built. <paramref name="finalsBestOf"/> is recorded on the
    /// SemiFinal and Final stages' <see cref="Stage.BestOf"/>; every OTHER
    /// round always plays Bo1 (a single decisive match, no
    /// <see cref="MatchSeries"/>). When <paramref name="finalsBestOf"/> is
    /// greater than 1, each SemiFinal/Final pairing is settled by a REAL
    /// <see cref="MatchSeries"/> built by <see cref="BuildDecidedSeries"/> —
    /// as many finished games as it actually takes for one team to reach the
    /// majority (<see cref="SeriesDecisionCalculator"/>), varying between the
    /// series' minimum-to-clinch and its full BestOf across pairings so a
    /// season's worth of series doesn't all end identically. No-ops when
    /// fewer than two teams are seeded.
    /// </summary>
    private static void SeedEliminationBracket(
        Division division,
        DateTime anchorDate,
        List<Guid> seedIds,
        Dictionary<Guid, Team> teamsById,
        List<Venue> venues,
        string? bracketName,
        int finalsBestOf,
        SlugRegistry slugRegistry,
        ref int order)
    {
        if (seedIds.Count < 2)
        {
            return;
        }

        int bracketSize = PlayoffSeeder.NextPowerOfTwo(seedIds.Count);
        List<(StageType Type, Template Template)> rounds = RoundsForBracket(bracketSize);
        List<(Guid HomeTeamId, Guid? VisitorTeamId)> firstRoundPairs = PlayoffSeeder.SeedPairs(seedIds);

        DateTime roundStart = anchorDate;
        List<Team>? advancing = null;

        for (int roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
        {
            (StageType stageType, Template template) = rounds[roundIndex];
            bool isFinalsRound = roundIndex >= rounds.Count - 2;
            int stageBestOf = isFinalsRound ? finalsBestOf : 1;
            string stageName = bracketName is null ? template.Name : $"{template.Name} {bracketName}";

            Stage stage = new()
            {
                CreatedBy = CreatedBy,
                Name = stageName,
                Slug = slugRegistry.ForStage($"{stageName} {division.Name}"),
                StageType = stageType,
                IsActive = true,
                IsElimination = true,
                BracketName = bracketName,
                BestOf = stageBestOf,
                StartDate = roundStart,
                EndDate = roundStart.AddDays(StageTemplate.DurationDays),
                DivisionId = Guid.Empty,
                Division = division,
                Matches = [],
                Order = order++,
            };
            division.Stages.Add(stage);

            List<Team> roundEntrants = roundIndex == 0
                ? [.. seedIds.Select(id => teamsById[id])]
                : advancing!;
            AddStageTeamMatches(stage, roundEntrants);

            List<Team> winners = [];

            if (roundIndex == 0)
            {
                for (int i = 0; i < firstRoundPairs.Count; i++)
                {
                    (Guid homeId, Guid? visitorId) = firstRoundPairs[i];
                    Team home = teamsById[homeId];

                    if (visitorId is null)
                    {
                        // BYE: the top seed advances automatically. It still gets
                        // a match row with no visitor and no score, exactly the
                        // shape StageService.FillStageWithSeedsAsync writes for a
                        // bye — otherwise the stage held more StageTeamMatch rows
                        // than its matches had slots, and re-seeding it from the
                        // admin panel threw SeedTeamCountOutOfRange.
                        stage.Matches.Add(BuildByeMatch(stage, home, venues, roundStart.AddDays(i)));
                        winners.Add(home);
                        continue;
                    }

                    Team visitor = teamsById[visitorId.Value];

                    if (stageBestOf > 1)
                    {
                        Team seriesWinner = BuildDecidedSeries(
                            stage, home, visitor, stageBestOf, venues, roundStart.AddDays(i),
                            seriesSeed: (stage.Order * 97) + (i * 13));
                        winners.Add(seriesWinner);
                    }
                    else
                    {
                        Match match = BuildFinishedMatch(
                            stage, home, visitor, 79 + (i * 3), 66 + (i * 2), MatchType.Playoff,
                            venues, roundStart.AddDays(i), i, round: null);
                        stage.Matches.Add(match);
                        winners.Add(match.WinningTeam!);
                    }
                }
            }
            else
            {
                for (int i = 0; i < roundEntrants.Count; i += 2)
                {
                    int pairIndex = i / 2;
                    Team home = roundEntrants[i];
                    Team visitor = roundEntrants[i + 1];

                    if (stageBestOf > 1)
                    {
                        Team seriesWinner = BuildDecidedSeries(
                            stage, home, visitor, stageBestOf, venues, roundStart.AddDays(pairIndex),
                            seriesSeed: (stage.Order * 97) + (pairIndex * 13) + 5);
                        winners.Add(seriesWinner);
                    }
                    else
                    {
                        Match match = BuildFinishedMatch(
                            stage, home, visitor, 84 + i, 71 + i, MatchType.Playoff,
                            venues, roundStart.AddDays(pairIndex), pairIndex, round: null);
                        stage.Matches.Add(match);
                        winners.Add(match.WinningTeam!);
                    }
                }
            }

            advancing = winners;
            roundStart = stage.EndDate.AddDays(StageTemplate.StandardGapDays);
        }
    }

    /// <summary>
    /// Builds a REAL best-of-N <see cref="MatchSeries"/> between
    /// <paramref name="home"/> and <paramref name="visitor"/> and adds it (and
    /// every game it plays) to <paramref name="stage"/>, so both the admin
    /// panel and the public site can see the actual per-game series instead of
    /// one collapsed result. Generates just enough finished games for one team
    /// to reach the majority of <paramref name="bestOf"/>
    /// (<see cref="SeriesDecisionCalculator.DetermineWinner"/> is the single
    /// source of truth for when the series is decided — the same threshold
    /// <c>Application.Services.MatchSeriesService</c> uses for a live admin's
    /// "agregar partido" action) and stops there — no games are generated past
    /// the decisive one, matching how a live series behaves once
    /// <c>AddGameToSeriesAsync</c> would start throwing.
    ///
    /// The "home" team (this builder's convention for the designated winner —
    /// every other decisive result it seeds has home win too) always wins the
    /// series, but how many games the visitor takes off it before losing
    /// varies deterministically per pairing: <paramref name="seriesSeed"/> —
    /// built from stable indices (stage order, pairing position), never a
    /// randomly-generated id — selects 0..(gamesToWin-1) "away" wins the same
    /// way <see cref="AddScoring"/> spreads points without System.Random, so a
    /// reseed is reproducible while a batch of series still mixes sweeps and
    /// full-distance series instead of ending identically.
    /// </summary>
    private static Team BuildDecidedSeries(
        Stage stage,
        Team home,
        Team visitor,
        int bestOf,
        List<Venue> venues,
        DateTime anchorDate,
        int seriesSeed)
    {
        MatchSeries series = new()
        {
            StageId = Guid.Empty,
            Stage = stage,
            // Unlike Stage.Id (EF-generated at save time, hence the
            // Guid.Empty placeholder above), Team.Id is already assigned
            // when teams are built (BuildDivisionWithTeams), so the real id
            // is set here directly — matching BuildFinishedMatch's HomeTeamId
            // convention. SeriesDecisionCalculator.DetermineWinner runs
            // in-memory below, before any EF save/fixup, and compares game
            // winners against these ids, so they must be the real values now.
            HomeTeamId = home.Id,
            HomeTeam = home,
            VisitorTeamId = visitor.Id,
            VisitorTeam = visitor,
            BestOf = bestOf,
            CreatedBy = CreatedBy,
        };
        stage.MatchSeries.Add(series);

        int gamesToWin = (bestOf / 2) + 1;
        int visitorWins = gamesToWin <= 1 ? 0 : Math.Abs(seriesSeed) % gamesToWin;
        int totalGames = gamesToWin + visitorWins;

        for (int gameNumber = 1; gameNumber <= totalGames; gameNumber++)
        {
            // The visitor takes the first `visitorWins` games of the series
            // (a "comeback" shape); the home team wins every game after that,
            // clinching on the last generated game.
            bool homeWinsThisGame = gameNumber > visitorWins;
            int margin = 4 + ((gameNumber + seriesSeed) % 9);
            int winnerScore = 68 + ((gameNumber * 5) % 22);
            int loserScore = winnerScore - margin;
            int homeScore = homeWinsThisGame ? winnerScore : loserScore;
            int visitorScore = homeWinsThisGame ? loserScore : winnerScore;

            Match game = BuildFinishedMatch(
                stage, home, visitor, homeScore, visitorScore, MatchType.Playoff,
                venues, anchorDate.AddDays(gameNumber - 1), seriesSeed + gameNumber, round: null);
            game.Series = series;
            game.GameNumber = gameNumber;

            stage.Matches.Add(game);
            series.Matches.Add(game);
        }

        Guid? winningTeamId = SeriesDecisionCalculator.DetermineWinner(series);
        Team winnerTeam = winningTeamId == visitor.Id ? visitor : home;
        series.WinningTeamId = winningTeamId;
        series.WinningTeam = winnerTeam;

        return winnerTeam;
    }

    /// <summary>
    /// The rounds a single-elimination bracket of <paramref name="bracketSize"/>
    /// seeds needs, in play order (first round first, Final last). Supports up
    /// to a 16-seed bracket — the largest this builder's tournaments need.
    /// </summary>
    private static List<(StageType Type, Template Template)> RoundsForBracket(int bracketSize)
    {
        List<(StageType, Template)> rounds = [];

        for (int remaining = bracketSize; remaining >= 2; remaining /= 2)
        {
            rounds.Add(remaining switch
            {
                2 => (StageType.Final, StageTemplate.Final),
                4 => (StageType.SemiFinal, StageTemplate.SemiFinal),
                8 => (StageType.QuarterFinal, StageTemplate.QuarterFinal),
                16 => (StageType.RoundOf16, StageTemplate.RoundOf16),
                _ => throw new NotSupportedException(
                    $"Elimination brackets larger than 16 seeds are not supported (requested {bracketSize})."),
            });
        }

        return rounds;
    }

    /// <summary>
    /// Builds the cross-division cup (HU-110): one division with
    /// <see cref="Division.IsCrossDivisionCup"/> = true whose teams are
    /// <paramref name="allTeams"/> (the tournament's teams, or a subset via
    /// <see cref="CrossCupDefinition.TeamPoolSize"/>), split into
    /// <paramref name="crossCup"/>.GroupCount finished round-robin groups
    /// ("Grupo 1".."Grupo N", jornadas on Wednesdays). The top
    /// <c>QualifiersPerGroup</c> of every group are pooled via
    /// <see cref="CrossCupGroupSeeder"/> into one bracket, built by
    /// <see cref="SeedEliminationBracket"/> (as many rounds as the pool needs).
    /// Each team gets a StageTeamMatch in its cup group IN ADDITION to its
    /// regular zone — cross cups are exempt from one-team-one-zone.
    /// </summary>
    private static void SeedCrossDivisionCup(
        Tournament tournament,
        CrossCupDefinition crossCup,
        List<Team> allTeams,
        List<Venue> venues,
        DateTime anchorDate,
        SlugRegistry slugRegistry,
        int upsetPercent,
        int varietySeed)
    {
        Division cupDivision = new()
        {
            CreatedBy = CreatedBy,
            Name = crossCup.DivisionName,
            Slug = slugRegistry.ForDivision(crossCup.DivisionName),
            Tournament = tournament,
            Stages = [],
            IsCrossDivisionCup = true,
            QualifiersPerGroup = crossCup.QualifiersPerGroup,
            // The cup lives inside its tournament, so it shares the tournament's
            // category (HU-48) like every other division.
            Category = tournament.Category,
        };
        tournament.Divisions.Add(cupDivision);

        int groupCount = crossCup.GroupCount;

        List<List<Position>> groupStandings = [];
        Dictionary<Guid, Team> teamsById = allTeams.ToDictionary(t => t.Id);

        // Distribute teams round-robin across the groups so each group mixes
        // teams from both zones (index % groupCount), keeping groups balanced.
        for (int g = 0; g < groupCount; g++)
        {
            List<Team> groupTeams = [.. allTeams.Where((_, idx) => idx % groupCount == g)];

            Stage groupStage = new()
            {
                CreatedBy = CreatedBy,
                Name = $"Grupo {g + 1}",
                Slug = slugRegistry.ForStage($"Grupo {g + 1} {cupDivision.Name}"),
                StageType = StageType.Group,
                IsActive = true,
                StartDate = anchorDate,
                EndDate = anchorDate.AddDays(7 * groupTeams.Count * crossCup.RoundRobinLegs),
                DivisionId = Guid.Empty,
                Division = cupDivision,
                Matches = [],
                Order = g,
                RoundRobinLegs = crossCup.RoundRobinLegs,
            };
            cupDivision.Stages.Add(groupStage);
            AddStageTeamMatches(groupStage, groupTeams);

            SeedRoundRobinMatches(
                groupStage, groupTeams, venues, anchorDate, isCrossDivisionCup: true, legs: crossCup.RoundRobinLegs,
                playedRounds: null, upsetPercent: upsetPercent, varietySeed: varietySeed + g);

            groupStandings.Add(PositionCalculator.CalculatePositions(groupStage.Matches));
        }

        List<Guid> seedOrder = CrossCupGroupSeeder.ResolveSeedOrder(groupStandings, crossCup.QualifiersPerGroup);
        if (seedOrder.Count < 2)
        {
            return;
        }

        // The bracket starts after the LAST group jornada actually scheduled
        // (SeedRoundRobinMatches pushes a group stage's EndDate out to its own
        // last match), not after an estimate built from the average group size —
        // which could date a knockout game before the groups had finished.
        DateTime bracketStart = cupDivision.Stages.Max(s => s.EndDate).AddDays(StageTemplate.StandardGapDays);
        int order = groupCount;

        SeedEliminationBracket(
            cupDivision, bracketStart, seedOrder, teamsById, venues,
            bracketName: null, crossCup.FinalsBestOf, slugRegistry, ref order);
    }

    /// <summary>
    /// Deterministic "does this fixture go against the seeding?" draw. The base
    /// <paramref name="upsetPercent"/> is damped by the seeding gap, so
    /// neighbours in the table trade wins often while a bottom side beating the
    /// leader stays rare — the shape a real standings table has. 0 disables
    /// upsets entirely (the stronger team always wins), which is the historical
    /// behaviour every caller that does not opt in keeps.
    /// </summary>
    private static bool IsUpset(
        int upsetPercent, int varietySeed, int stageOrder, int round, int homeIdx, int visitorIdx)
    {
        if (upsetPercent <= 0)
        {
            return false;
        }

        int gap = Math.Abs(homeIdx - visitorIdx);
        int chance = Math.Max(6, upsetPercent - (gap * 2));

        int hash = 17;
        hash = (hash * 31) + varietySeed;
        hash = (hash * 31) + stageOrder;
        hash = (hash * 31) + round;
        hash = (hash * 31) + Math.Min(homeIdx, visitorIdx);
        hash = (hash * 31) + Math.Max(homeIdx, visitorIdx);
        hash ^= hash >> 13;
        hash *= 0x5BD1E995;
        hash ^= hash >> 15;

        return ((hash & 0x7FFFFFFF) % 100) < chance;
    }

    /// <summary>
    /// The gym a team plays its home games at. Picked by a stable hash of the
    /// team name so a club always hosts at the same venue, instead of the match
    /// index picking an unrelated gym on the other side of the province for
    /// every game.
    /// </summary>
    private static Venue VenueForHome(Team home, List<Venue> venues)
    {
        int hash = 17;
        foreach (char character in home.Name)
        {
            hash = (hash * 31) + character;
        }

        return venues[(hash & 0x7FFFFFFF) % venues.Count];
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
    /// Builds one finished, decisive (never-tied) Match with a scorer/statistic
    /// for both teams (mirroring the goleadores ranking source, HU-72). Sets
    /// <see cref="Match.Round"/> (the jornada) for round-robin games and leaves
    /// it null for knockout games. <see cref="Match.Status"/> is set to
    /// <see cref="MatchStatus.Played"/> so the result lifecycle is coherent.
    /// </summary>
    private static Match BuildFinishedMatch(
        Stage stage,
        Team home,
        Team visitor,
        int homeScore,
        int visitorScore,
        MatchType type,
        List<Venue> venues,
        DateTime matchDate,
        int venueIndex,
        int? round)
    {
        Venue venue = VenueForHome(home, venues);
        Team winner = homeScore > visitorScore ? home : visitor;

        Match match = new()
        {
            CreatedBy = CreatedBy,
            MatchDate = matchDate,
            Round = round,
            Type = type,
            Slug = SlugGenerator.GenerateSlug($"{home.Name}-vs-{visitor.Name}-{stage.StageType}-{Guid.NewGuid()}"),
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            HomeScore = homeScore,
            VisitorScore = visitorScore,
            IsFinished = true,
            Status = MatchStatus.Played,
            WinningTeam = winner,
            WinningTeamId = winner.Id,
            Stage = stage,
            Venue = venue,
            PlayerStatistics = [],
            Scorers = [],
        };

        AddScoring(match, home, homeScore, venueIndex);
        AddScoring(match, visitor, visitorScore, venueIndex + 1);

        return match;
    }

    /// <summary>
    /// Builds the match row that represents a first-round BYE: the seed that
    /// advances is the home team, there is no visitor and no score, and the
    /// match is already finished with that team as the winner — the same
    /// representation <c>StageService.FillStageWithSeedsAsync</c> writes when it
    /// seeds a bracket that is not a power of two. <see cref="PositionCalculator"/>
    /// ignores it (it has no visitor), so it never reaches a standings table.
    /// </summary>
    private static Match BuildByeMatch(Stage stage, Team home, List<Venue> venues, DateTime matchDate) =>
        new()
        {
            CreatedBy = CreatedBy,
            MatchDate = matchDate,
            Round = null,
            Type = MatchType.Playoff,
            Slug = SlugGenerator.GenerateSlug($"{home.Name}-bye-{stage.StageType}-{Guid.NewGuid()}"),
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = null,
            VisitorTeamId = null,
            HomeScore = null,
            VisitorScore = null,
            IsFinished = true,
            Status = MatchStatus.Played,
            WinningTeam = home,
            WinningTeamId = home.Id,
            Stage = stage,
            Venue = VenueForHome(home, venues),
            PlayerStatistics = [],
            Scorers = [],
        };

    /// <summary>
    /// Builds one UPCOMING (unplayed) regular match: teams and a future
    /// <see cref="Match.MatchDate"/>/<see cref="Match.Round"/> are set, but there
    /// is no score, no winner and no scorers, and
    /// <see cref="Match.Status"/> stays <see cref="MatchStatus.Scheduled"/> with
    /// <see cref="Match.IsFinished"/> = false. Used to fill the still-to-play
    /// jornadas of an in-progress tournament so its "Próximos" fixture has data.
    /// </summary>
    private static Match BuildUpcomingMatch(
        Stage stage,
        Team home,
        Team visitor,
        MatchType type,
        List<Venue> venues,
        DateTime matchDate,
        int venueIndex,
        int? round)
    {
        Venue venue = VenueForHome(home, venues);

        return new Match
        {
            CreatedBy = CreatedBy,
            MatchDate = matchDate,
            Round = round,
            Type = type,
            Slug = SlugGenerator.GenerateSlug($"{home.Name}-vs-{visitor.Name}-{stage.StageType}-{Guid.NewGuid()}"),
            HomeTeam = home,
            HomeTeamId = home.Id,
            VisitorTeam = visitor,
            VisitorTeamId = visitor.Id,
            HomeScore = null,
            VisitorScore = null,
            IsFinished = false,
            Status = MatchStatus.Scheduled,
            WinningTeam = null,
            WinningTeamId = null,
            Stage = stage,
            Venue = venue,
            PlayerStatistics = [],
            Scorers = [],
        };
    }

    /// <summary>
    /// The players of <paramref name="team"/> a seeded match sheet may list:
    /// their season registration is Approved and they are not serving a
    /// sanction. This is the same eligibility rule
    /// <c>PlayerStatisticService.ValidateEligibilityAsync</c> enforces on every
    /// real match sheet (HU-57/HU-60/HU-61) — the seed used to cycle through the
    /// whole roster and hand points to the deliberately Pending player too.
    /// Falls back to the full roster only if a team somehow has no registration
    /// at all, so a match is never left without scorers.
    /// </summary>
    private static List<Player> EligibleScorers(Team team)
    {
        List<Player> eligible =
        [.. team.PlayerTeamRegistrations
            .Where(registration => registration.MedicalRecordStatus == MedicalRecordStatus.Approved)
            .Select(registration => registration.Player)
            .OfType<Player>()
            .Where(player => !player.IsSanctioned)];

        return eligible.Count > 0 ? eligible : [.. team.Players];
    }

    /// <summary>
    /// Keeps a suspended player off the match sheets he is not allowed to
    /// appear on: his scoring rows in the games he must miss are handed to a
    /// team-mate who is not already scoring in that game, so the team's points
    /// still add up to the recorded result. The window is the next
    /// <paramref name="duration"/> games his team plays after the ruling; while
    /// the sanction is still ACTIVE (<see cref="Player.IsSanctioned"/> says he
    /// is serving it right now) it runs to the end of the tournament.
    /// </summary>
    private static void ApplySuspension(
        List<Match> playedMatches, Player player, Team team, Match sanctionMatch, int duration, bool active)
    {
        List<Match> afterTheRuling =
        [.. playedMatches.Where(match =>
            (match.HomeTeam == team || match.VisitorTeam == team)
            && match.MatchDate > sanctionMatch.MatchDate)];

        foreach (Match match in active ? afterTheRuling : afterTheRuling.Take(duration))
        {
            ReassignScoring(match, player, team);
        }
    }

    /// <summary>
    /// Moves every scoring row <paramref name="player"/> holds in
    /// <paramref name="match"/> onto an eligible team-mate who is not already
    /// scoring in it, so the points still sum to the team's recorded score. A
    /// no-op when the player did not score in that game.
    /// </summary>
    private static void ReassignScoring(Match match, Player player, Team team)
    {
        List<Scorer> scorers = [.. match.Scorers.Where(scorer => scorer.Player == player)];
        if (scorers.Count == 0)
        {
            return;
        }

        HashSet<Player> alreadyScoring = [.. match.Scorers.Select(scorer => scorer.Player).OfType<Player>()];
        Player? substitute = EligibleScorers(team)
            .FirstOrDefault(candidate => candidate != player && !alreadyScoring.Contains(candidate));

        if (substitute is null)
        {
            return;
        }

        foreach (Scorer scorer in scorers)
        {
            scorer.Player = substitute;
        }

        foreach (PlayerStatistic statistic in match.PlayerStatistics.Where(s => s.Player == player))
        {
            statistic.Player = substitute;
        }
    }

    /// <summary>
    /// Adds a scorer plus Points/Assists PlayerStatistic rows for one team in a
    /// match (HU-72: the goleadores ranking reads PlayerStatistic). Skips a
    /// zero-score team so no phantom scorer is created.
    /// </summary>
    private static void AddScoring(Match match, Team team, int score, int scorerSeed)
    {
        List<Player> eligible = EligibleScorers(team);

        if (score <= 0 || eligible.Count == 0)
        {
            return;
        }

        // Spread the team's points across a handful of players (deterministic, no
        // RNG) so the goleadores read realistically instead of one player scoring
        // the whole game. Weights taper off; the lead scorer takes the remainder.
        int[] weights = [5, 4, 3, 2, 1];
        int scorerCount = Math.Min(weights.Length, eligible.Count);
        int weightTotal = 0;
        for (int i = 0; i < scorerCount; i++)
        {
            weightTotal += weights[i];
        }

        int[] shares = new int[scorerCount];
        int distributed = 0;
        for (int i = 1; i < scorerCount; i++)
        {
            shares[i] = score * weights[i] / weightTotal;
            distributed += shares[i];
        }
        shares[0] = score - distributed;

        for (int i = 0; i < scorerCount; i++)
        {
            if (shares[i] <= 0)
            {
                continue;
            }

            Player player = eligible[Math.Abs(scorerSeed + i) % eligible.Count];
            AddPlayerScoring(match, player, shares[i]);
        }
    }

    private static void AddPlayerScoring(Match match, Player player, int points)
    {
        match.Scorers.Add(new Scorer
        {
            CreatedBy = CreatedBy,
            PlayerId = Guid.Empty,
            Player = player,
            Points = points,
            MatchId = Guid.Empty,
            Match = match,
        });
        match.PlayerStatistics.Add(new PlayerStatistic
        {
            CreatedBy = CreatedBy,
            Value = points,
            PlayerId = Guid.Empty,
            Player = player,
            MatchId = Guid.Empty,
            Match = match,
            Type = StatisticType.Points,
        });
        match.PlayerStatistics.Add(new PlayerStatistic
        {
            CreatedBy = CreatedBy,
            Value = 1,
            PlayerId = Guid.Empty,
            Player = player,
            MatchId = Guid.Empty,
            Match = match,
            Type = StatisticType.Assists,
        });
    }

    /// <summary>
    /// Seeds a coherent, varied set of basketball sanctions tied to real
    /// finished group matches/players (HU-75/HU-77): a mix of active
    /// (IsSanctioned) and served sanctions, one under appeal, and one
    /// institutional Team sanction. All descriptions are Spanish, basketball
    /// terms (technical/unsportsmanlike/disqualifying fouls, not soccer cards).
    /// </summary>
    private static List<PlayerSanction> SeedSanctions(List<Stage> groupStages, Tournament tournament)
    {
        List<PlayerSanction> sanctions = [];

        // Only finished matches carry a real result/winner, so sanctions are
        // tied to those (an in-progress tournament's group stage also holds
        // still-to-play upcoming matches, which must never seed a sanction).
        // Ordered by date because whether a sanction reads as already served or
        // as still being served depends on where in the calendar it sits.
        List<Match> matches = [.. groupStages
            .SelectMany(s => s.Matches)
            .Where(m => m.IsFinished && m.VisitorTeam is not null)
            .OrderBy(m => m.MatchDate)];
        if (matches.Count == 0)
        {
            return sanctions;
        }

        // Every played game of the tournament, playoffs included: a suspension
        // handed out in the last jornadas of a zone also rules the player out of
        // his team's cup games.
        List<Match> playedMatches = [.. tournament.Divisions
            .SelectMany(d => d.Stages)
            .SelectMany(s => s.Matches)
            .Where(m => m.IsFinished && m.VisitorTeam is not null)
            .OrderBy(m => m.MatchDate)];

        // (description, duration, subjectType, appealStatus, active) tuples.
        (string Description, int Duration, SanctionSubjectType Subject, SanctionAppealStatus Appeal, bool Active)[] specs =
        [
            ("Falta descalificante por conducta antideportiva.", 2, SanctionSubjectType.Player, SanctionAppealStatus.None, true),
            ("Expulsión por doble falta técnica.", 1, SanctionSubjectType.Player, SanctionAppealStatus.Pending, true),
            ("Agresión a un rival durante el partido.", 3, SanctionSubjectType.Player, SanctionAppealStatus.None, true),
            ("Reclamos reiterados al árbitro.", 1, SanctionSubjectType.Player, SanctionAppealStatus.Rejected, false),
            ("Falta antideportiva reiterada.", 2, SanctionSubjectType.Player, SanctionAppealStatus.None, false),
            ("Suspensión de cancha por incidentes del público.", 1, SanctionSubjectType.Team, SanctionAppealStatus.None, true),
        ];

        for (int i = 0; i < specs.Length; i++)
        {
            (string description, int duration, SanctionSubjectType subject, SanctionAppealStatus appeal, bool active) = specs[i];

            // A sanction is issued ON the game it came from — never before it.
            // What separates a served sanction from one still being served is
            // WHICH game: a served one is drawn from the opening third of the
            // calendar (its fechas have long elapsed), an active one from the
            // closing third, where the player is still sitting it out.
            int window = Math.Max(1, matches.Count / 3);
            Match match = active
                ? matches[matches.Count - 1 - ((i * 3) % window)]
                : matches[(i * 3) % window];
            Team losingTeam = match.WinningTeam == match.HomeTeam ? match.VisitorTeam! : match.HomeTeam!;

            PlayerSanction sanction = new()
            {
                CreatedBy = CreatedBy,
                Duration = duration,
                IssuedDate = match.MatchDate,
                Description = description,
                SubjectType = subject,
                Match = match,
                MatchId = Guid.Empty,
                AppealStatus = appeal,
                Slug = SlugGenerator.GenerateSlug($"sancion-{match.Slug}-{i}-{Guid.NewGuid()}"),
            };

            if (subject == SanctionSubjectType.Team)
            {
                sanction.Team = losingTeam;
                sanction.TeamId = Guid.Empty;
            }
            else
            {
                Player player = losingTeam.Players.ElementAt((i + 1) % losingTeam.Players.Count);
                player.IsSanctioned = active;
                sanction.Player = player;
                sanction.PlayerId = Guid.Empty;

                ApplySuspension(playedMatches, player, losingTeam, match, duration, active);
            }

            // An appeal can only be filed after the ruling, and resolved after
            // it is filed — both used to be back-dated before the match.
            if (appeal == SanctionAppealStatus.Pending)
            {
                sanction.AppealReason = "El jugador sostiene que la falta no existió.";
                sanction.AppealDate = match.MatchDate.AddDays(1);
            }
            else if (appeal == SanctionAppealStatus.Rejected)
            {
                sanction.AppealReason = "Se solicitó revisión de la jugada.";
                sanction.AppealDate = match.MatchDate.AddDays(2);
                sanction.AppealResolution = "El tribunal ratificó la sanción.";
                sanction.AppealResolvedDate = match.MatchDate.AddDays(5);
            }

            sanctions.Add(sanction);
        }

        return sanctions;
    }
}
