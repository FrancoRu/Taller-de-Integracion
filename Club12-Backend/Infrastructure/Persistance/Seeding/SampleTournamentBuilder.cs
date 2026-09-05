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
/// Builds one fully-populated, coherent sample Tournament from a declarative definition.
/// </summary>
public static class SampleTournamentBuilder
{
    private const string CreatedBy = AuditConstants.SystemUser;

    public const int DefaultPlayersPerTeam = 8;

    // Offset mixed into each division's variety seed so two same-sized zones of one tournament don't replay the same season.
    private const int DivisionVarietySalt = 7717;
    private static readonly DateTime SampleMedicalRecordReviewedAt =
        new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly string[] FirstNames =
    [
        "Juan", "Carlos", "Martín", "Diego", "Facundo", "Lucas", "Nicolás", "Matías",
        "Franco", "Ezequiel", "Agustín", "Bruno", "Iván", "Santiago", "Tomás", "Gonzalo",
        "Joaquín", "Valentín", "Emiliano", "Thiago",
    ];

    // Used instead of FirstNames when the team's tournament Category is Feminine, so feminine divisions don't end up with male rosters.
    private static readonly string[] FeminineFirstNames =
    [
        "María", "Ana", "Lucía", "Sofía", "Camila", "Valentina", "Julieta", "Florencia",
        "Agustina", "Micaela", "Rocío", "Antonella", "Belén", "Candela", "Milagros", "Paula",
        "Martina", "Victoria", "Carla", "Daniela",
    ];

    // Eleven health-insurance providers, led by the Entre Ríos provincial IOSPER since the league is from Paraná, kept co-prime with 20 first names and 21 surnames so a roster never ends up with one obra social per first name.
    private static readonly string[] HealthInsuranceProviders =
    [
        "IOSPER", "OSDE", "Swiss Medical", "Sancor Salud", "Galeno", "Medifé",
        "OSECAC", "Avalian", "Federada Salud", "Prevención Salud", "Jerárquicos Salud",
    ];

    private static readonly string[] LastNames =
    [
        "González", "Rodríguez", "Fernández", "López", "Díaz", "Pérez", "Sánchez", "Romero",
        "Álvarez", "Torres", "Ruiz", "Ramírez", "Flores", "Acosta", "Benítez", "Medina",
        "Cabrera", "Sosa", "Vera", "Ledesma", "Quiroga",
    ];

    /// <summary>
    /// A named playoff cup fed by a contiguous range of a division's final group-standings positions.
    /// </summary>
    public sealed record PlayoffCupDefinition(
        string BracketName,
        int FromPosition,
        int ToPosition,
        int BestOf);

    /// <summary>
    /// The cross-division cup is an extra division whose teams are drawn from the tournament's teams and split into internal groups.
    /// </summary>
    /// <param name="DivisionName">The cup's division name.</param>
    /// <param name="GroupCount">How many internal group stages to split the team pool into.</param>
    /// <param name="QualifiersPerGroup">How many top teams from each group pool into the bracket.</param>
    /// <param name="RoundRobinLegs">
    /// How many times each pair plays within an internal group. A value of 1
    /// plays a single round-robin and 2 plays a double round-robin, also
    /// called ida y vuelta. Defaults to 1.
    /// </param>
    /// <param name="FinalsBestOf">
    /// BestOf applied to the pooled bracket's SemiFinal and Final rounds
    /// only. Earlier rounds, needed once more than 4 teams are pooled,
    /// always play Bo1. Defaults to 1.
    /// </param>
    /// <param name="TeamPoolSize">
    /// When set, only the first TeamPoolSize of the
    /// tournament's teams, in build order, feed the cup, instead of every
    /// team. Null, the default, uses every team, unchanged from the
    /// original behavior.
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
    /// Hands out clean, collision-free kebab-case slugs for the divisions and stages built in one seeding run.
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
    /// Builds one Tournament with every division in definition.
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
            // Every division built below inherits the tournament's category so the one tournament, one category invariant holds in the seeded graph.
            Category = definition.Category,
            Divisions = [],
            Teams = [],
        };

        List<Team> allTeams = [];
        List<Stage> regularGroupStages = [];

        for (int divisionIndex = 0; divisionIndex < definition.Divisions.Length; divisionIndex++)
        {
            DivisionDefinition divisionDef = definition.Divisions[divisionIndex];

            // Every result a zone produces is a pure function of the seed, so two same-sized zones sharing a seed played out identically, once leaving Zona A and Zona B with the same scorelines and final table; salting per division separates them.
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

        // Feminine divisions draw first names from the feminine pool so their rosters don't read as male.
        string[] firstNames = tournament.Category == TournamentCategory.Feminine ? FeminineFirstNames : FirstNames;

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

            // Season-scoped participation source of truth, mirroring the PlayerTeamRegistration seeding below, since the denormalized Team.TournamentId pointer alone is not authoritative.
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

                string firstName = firstNames[playerCounter % firstNames.Length];
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
                    SocialSecurity =
                        HealthInsuranceProviders[playerCounter % HealthInsuranceProviders.Length],
                    Team = team,
                };

                team.Players.Add(player);

                // Most seeded players' ficha is Approved and the last player of each team stays Pending to keep the upload/review flow demonstrable; Approved rows are seeded without a file reference since DataSeeder.SeedMedicalRecordsAsync fills MedicalRecordFileUrl with a real uploaded object after Build() runs, so until then an Approved row correctly reads as not habilitado.
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
    /// The team's crest, generated from its own kit, is a round badge in the club's shirt colour carrying its three-letter code.
    /// </summary>
    private static string BuildCrestDataUri(
        string code, string primaryColor, string? secondaryColor, string jerseyStyle)
    {
        string secondary = string.IsNullOrWhiteSpace(secondaryColor) ? "#FFFFFF" : secondaryColor;
        string ink = ContrastInk(primaryColor);

        // Each band is drawn across the whole square and clipped to the badge, so a style only has to describe its own shape.
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

    /// <summary>
    /// Near-black ink, the alternative to white for a crest's code.
    /// </summary>
    private const string DarkInk = "#0F172A";

    /// <summary>
    /// Whichever of white and DarkInk reads better on backgroundColor, by actual WCAG contrast ratio rather than a hand-picked luminance threshold.
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
    /// WCAG relative luminance of an #RRGGBB colour, or null when the string is not a valid one.
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
    /// Builds a real circle-method single round-robin for teams, so every pair meets exactly once per leg.
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

        // An odd roster gets a bye slot so the circle method still produces a complete round-robin, since running n-1 jornadas on an odd roster instead dropped pairings, replayed others twice per leg, and left one team pinned to extra games.
        const int ByeSlot = -1;
        int slotCount = n % 2 == 0 ? n : n + 1;
        int roundsPerLeg = slotCount - 1;
        int matchIndex = 0;
        DateTime lastMatchDate = stage.StartDate;

        for (int leg = 0; leg < legs; leg++)
        {
            // The circle method rotates slots 1..n-1 each round while keeping index 0 fixed, and resets at the start of each leg so the return leg replays the same pairings in seeding order, where a smaller index means a stronger team.
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

                    // Alternates home and away by the jornada within the leg, then inverts it on the return leg so the rematch swaps venue, since deriving this from the global jornada number instead cancelled the leg inversion whenever roundsPerLeg was odd and both legs were played at the same team's home.
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
                            stage, home, visitor, Domain.Enums.MatchType.Regular, venues, roundDate, round));
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
                            stage, home, visitor, homeScore, visitorScore, Domain.Enums.MatchType.Regular,
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

        // A stage can never end before its own last jornada, since playoff brackets anchor on Stage.EndDate and a short end date would date cup games before the zone's final jornadas were even played.
        if (lastMatchDate > stage.EndDate)
        {
            stage.EndDate = lastMatchDate;
        }
    }

    /// <summary>
    /// Rotates slots 1..n-1 by one position, keeping index 0 fixed.
    /// </summary>
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
    /// Builds SemiFinal, ThirdPlace, and Final stages for one division, seeded from the group stage's final standings.
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
                semiFinalStage, home, visitor, homeScore, visitorScore, Domain.Enums.MatchType.Playoff,
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
            thirdPlaceStage, semiFinalLosers[0], semiFinalLosers[1], 74, 61, Domain.Enums.MatchType.Playoff,
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
            finalStage, semiFinalWinners[0], semiFinalWinners[1], 82, 70, Domain.Enums.MatchType.Playoff,
            venues, finalStage.StartDate, 0, round: null);
        finalStage.Matches.Add(finalMatch);
    }

    /// <summary>
    /// Builds one full elimination bracket per named position-range cup, each seeded from the final group standings restricted to the cup's position range.
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
    /// Builds a full single-elimination bracket for seedIds, adding every round's Stage to division.
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
                        // The top seed advances automatically but still gets a match row with no visitor and no score, matching the shape StageService.FillStageWithSeedsAsync writes for a bye, since otherwise the stage held more StageTeamMatch rows than its matches had slots and re-seeding from the admin panel threw SeedTeamCountOutOfRange.
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
                            stage, home, visitor, 79 + (i * 3), 66 + (i * 2), Domain.Enums.MatchType.Playoff,
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
                            stage, home, visitor, 84 + i, 71 + i, Domain.Enums.MatchType.Playoff,
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
    /// Builds a real best-of-N MatchSeries between home and visitor and adds it, along with every game it plays, to stage.
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
            // Team.Id is already assigned when teams are built, unlike Stage.Id which is EF-generated at save time, so the real id is set here directly since SeriesDecisionCalculator.DetermineWinner runs in-memory before any EF save and compares game winners against these ids.
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
            // The visitor takes the first visitorWins games of the series in a comeback shape, then the home team wins every game after that, clinching on the last generated game.
            bool homeWinsThisGame = gameNumber > visitorWins;
            int margin = 4 + ((gameNumber + seriesSeed) % 9);
            int winnerScore = 68 + ((gameNumber * 5) % 22);
            int loserScore = winnerScore - margin;
            int homeScore = homeWinsThisGame ? winnerScore : loserScore;
            int visitorScore = homeWinsThisGame ? loserScore : winnerScore;

            Match game = BuildFinishedMatch(
                stage, home, visitor, homeScore, visitorScore, Domain.Enums.MatchType.Playoff,
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
    /// The rounds a single-elimination bracket of bracketSize seeds needs, in play order with the first round first and Final last.
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
    /// Builds the cross-division cup, one division whose teams are drawn from allTeams and split into internal round-robin groups.
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
            // The cup lives inside its tournament, so it shares the tournament's category like every other division.
            Category = tournament.Category,
        };
        tournament.Divisions.Add(cupDivision);

        int groupCount = crossCup.GroupCount;

        List<List<Position>> groupStandings = [];
        Dictionary<Guid, Team> teamsById = allTeams.ToDictionary(t => t.Id);

        // Distributes teams round-robin across the groups using index % groupCount so each group mixes teams from both zones and stays balanced.
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

        // The bracket starts after the last group jornada actually scheduled, since SeedRoundRobinMatches pushes a group stage's EndDate out to its own last match, rather than after an estimate built from the average group size which could date a knockout game before the groups had finished.
        DateTime bracketStart = cupDivision.Stages.Max(s => s.EndDate).AddDays(StageTemplate.StandardGapDays);
        int order = groupCount;

        SeedEliminationBracket(
            cupDivision, bracketStart, seedOrder, teamsById, venues,
            bracketName: null, crossCup.FinalsBestOf, slugRegistry, ref order);
    }

    /// <summary>
    /// Deterministic draw for whether this fixture goes against the seeding, with the base upsetPercent damped by the seeding gap.
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
    /// The gym a team plays its home games at, picked by a stable hash of the team name so a club always hosts at the same venue.
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
    /// Builds one finished, decisive, never-tied Match with a scorer and statistic for both teams.
    /// </summary>
    private static Match BuildFinishedMatch(
        Stage stage,
        Team home,
        Team visitor,
        int homeScore,
        int visitorScore,
        Domain.Enums.MatchType type,
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
    /// Builds the match row that represents a first-round BYE, where the home team advances with no visitor and no score.
    /// </summary>
    private static Match BuildByeMatch(Stage stage, Team home, List<Venue> venues, DateTime matchDate) =>
        new()
        {
            CreatedBy = CreatedBy,
            MatchDate = matchDate,
            Round = null,
            Type = Domain.Enums.MatchType.Playoff,
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
    /// Builds one UPCOMING, unplayed regular match with teams and a future date set but no score, winner, or scorers.
    /// </summary>
    private static Match BuildUpcomingMatch(
        Stage stage,
        Team home,
        Team visitor,
        MatchType type,
        List<Venue> venues,
        DateTime matchDate,
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
    /// The players of team that a seeded match sheet may list, meaning their season registration is Approved and they are not serving a sanction.
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
    /// Hands a suspended player's scoring rows in the games he must miss to a team-mate who is not already scoring in that game.
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
    /// Moves every scoring row player holds in match onto an eligible team-mate who is not already scoring in it.
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
            .Find(candidate => candidate != player && !alreadyScoring.Contains(candidate));

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
    /// Adds a scorer plus Points and Assists PlayerStatistic rows for one team in a match.
    /// </summary>
    private static void AddScoring(Match match, Team team, int score, int scorerSeed)
    {
        List<Player> eligible = EligibleScorers(team);

        if (score <= 0 || eligible.Count == 0)
        {
            return;
        }

        // Spreads the team's points deterministically across a handful of players, with tapering weights and the lead scorer taking the remainder, so the goleadores read realistically instead of one player scoring the whole game.
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
    /// Seeds a coherent, varied set of basketball sanctions tied to real finished group matches and players.
    /// </summary>
    private static List<PlayerSanction> SeedSanctions(List<Stage> groupStages, Tournament tournament)
    {
        List<PlayerSanction> sanctions = [];

        // Sanctions are tied only to finished matches, since an in-progress tournament's group stage also holds still-to-play upcoming matches which must never seed a sanction, and matches are ordered by date because whether a sanction reads as already served or still being served depends on where in the calendar it sits.
        List<Match> matches = [.. groupStages
            .SelectMany(s => s.Matches)
            .Where(m => m.IsFinished && m.VisitorTeam is not null)
            .OrderBy(m => m.MatchDate)];
        if (matches.Count == 0)
        {
            return sanctions;
        }

        // Every played game of the tournament, playoffs included, since a suspension handed out in the last jornadas of a zone also rules the player out of his team's cup games.
        List<Match> playedMatches = [.. tournament.Divisions
            .SelectMany(d => d.Stages)
            .SelectMany(s => s.Matches)
            .Where(m => m.IsFinished && m.VisitorTeam is not null)
            .OrderBy(m => m.MatchDate)];

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

            // A sanction is issued on the game it came from and never before it; a served sanction is drawn from the opening third of the calendar since its fechas have long elapsed, while an active one is drawn from the closing third where the player is still sitting it out.
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

            // An appeal can only be filed after the ruling and resolved after it is filed; both used to be back-dated before the match.
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
