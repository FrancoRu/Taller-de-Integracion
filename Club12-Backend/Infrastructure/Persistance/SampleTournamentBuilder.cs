using Application.Utils.Constants.Stage;
using Application.Utils.Helper.Playoff;
using Application.Utils.Helper.RoundRobin;
using Application.Utils.Helper.Slug;
using Application.Utils.Helper.Standings;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Persistance;

/// <summary>
/// Builds one fully-populated, coherent sample Tournament (divisions, teams,
/// players, group stages with proper round-robin jornadas and decisive scores,
/// scorers/statistics, position-range playoff cups, an optional
/// cross-division cup, and sanctions) from a declarative definition. Shared by
/// the startup DataSeeder (one call, no playoffs) and DataMaintenanceService
/// (the full coherent sample) so the construction logic exists once.
///
/// Coherence guarantees:
/// - Every group stage is a real circle-method round-robin: every team plays
///   exactly once per jornada, <see cref="Match.Round"/> is the 1-based
///   jornada, and <see cref="Match.MatchDate"/> comes from
///   <see cref="RoundCalendar.DateForRound(DateTime, int, bool)"/> (regular
///   zones on Sundays, cross-division cups on Wednesdays — HU-111, so a team's
///   zone and cup jornadas never collide).
/// - Playoff cups (e.g. Copa Oro 1-4, Copa Plata 5-8) are seeded from the
///   REAL final group standings via <see cref="PositionCalculator"/> and
///   <see cref="PlayoffSeeder"/>.
/// - The cross-division cup pools its group winners via
///   <see cref="CrossCupGroupSeeder"/>. Its teams keep their regular zone AND
///   join a cup group (cross cups are exempt from one-team-one-zone).
/// </summary>
public static class SampleTournamentBuilder
{
    private const string CreatedBy = AuditConstants.SystemUser;

    private static readonly string[] FirstNames =
    [
        "Juan", "Carlos", "Martín", "Diego", "Facundo", "Lucas", "Nicolás", "Matías",
        "Franco", "Ezequiel", "Agustín", "Bruno", "Iván", "Santiago", "Tomás", "Gonzalo",
    ];

    private static readonly string[] LastNames =
    [
        "González", "Rodríguez", "Fernández", "López", "Díaz", "Pérez", "Sánchez", "Romero",
        "Álvarez", "Torres", "Ruiz", "Ramírez", "Flores", "Acosta", "Benítez", "Medina",
    ];

    /// <summary>
    /// A named playoff cup fed by a contiguous range of a division's final
    /// group-standings positions (HU-45), e.g. "Copa Oro" for positions 1-4.
    /// Produces a <see cref="DivisionPlayoffMapping"/> row plus a SemiFinal +
    /// Final bracket whose <see cref="Stage.BracketName"/> equals
    /// <paramref name="BracketName"/>.
    /// </summary>
    public sealed record PlayoffCupDefinition(
        string BracketName,
        int FromPosition,
        int ToPosition,
        int BestOf);

    /// <summary>
    /// The cross-division cup ("Copa Cruzada"): an extra division with
    /// <see cref="Division.IsCrossDivisionCup"/> = true whose teams are ALL the
    /// tournament's teams, split into <paramref name="GroupCount"/> internal
    /// groups. The top <paramref name="QualifiersPerGroup"/> of each group are
    /// pooled into one bracket (HU-110).
    /// </summary>
    public sealed record CrossCupDefinition(
        string DivisionName,
        int GroupCount,
        int QualifiersPerGroup);

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
        DateTime FinishedMatchesStart,
        DateTime UpcomingMatchesStart,
        DivisionDefinition[] Divisions,
        CrossCupDefinition? CrossCup = null,
        TournamentStatus Status = TournamentStatus.Ongoing);

    public sealed record BuildResult(Tournament Tournament, List<PlayerSanction> Sanctions);

    /// <summary>
    /// Builds one Tournament with every division in <paramref name="definition"/>.
    /// <paramref name="playerCounter"/> is threaded through (and must keep
    /// incrementing) across multiple calls so player names/document numbers
    /// never collide between tournaments built in the same seeding run.
    /// <paramref name="includePlayoffs"/> opts each division into its playoff
    /// bracket(s) built from the group stage's final standings, and — when the
    /// definition declares a <see cref="TournamentDefinition.CrossCup"/> — into
    /// the cross-division cup. When false (the startup DataSeeder's default)
    /// each division gets only its group stage.
    /// </summary>
    public static BuildResult Build(
        TournamentDefinition definition,
        List<Venue> venues,
        ref int playerCounter,
        bool includePlayoffs = false)
    {
        Tournament tournament = new()
        {
            CreatedBy = CreatedBy,
            Name = definition.Name,
            Slug = SlugGenerator.GenerateSlug(definition.Name),
            Description = definition.Description,
            TeamRegistrationDeadline = definition.TeamRegistrationDeadline,
            StartDate = definition.StartDate,
            Status = definition.Status,
            Divisions = [],
            Teams = [],
        };

        List<Team> allTeams = [];
        List<Stage> regularGroupStages = [];

        foreach (DivisionDefinition divisionDef in definition.Divisions)
        {
            (Division division, List<Team> teams) = BuildDivisionWithTeams(
                tournament,
                divisionDef.DivisionName,
                divisionDef.TeamNames,
                divisionDef.TeamCodes,
                divisionDef.TeamColors,
                divisionDef.TeamStyles,
                divisionDef.TeamSecondaryColors,
                ref playerCounter);

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
                Slug = SlugGenerator.GenerateSlug($"{StageTemplate.Group.Name} {division.Name} {Guid.NewGuid()}"),
                StageType = StageType.Group,
                IsActive = true,
                StartDate = definition.StageStartDate,
                EndDate = definition.StageEndDate,
                DivisionId = Guid.Empty,
                Division = division,
                Matches = [],
                Order = 0,
                RoundRobinLegs = 1,
            };
            division.Stages.Add(stage);
            regularGroupStages.Add(stage);
            AddStageTeamMatches(stage, teams);

            SeedRoundRobinMatches(
                stage, teams, venues, definition.StageStartDate, isCrossDivisionCup: false);

            if (includePlayoffs)
            {
                if (divisionDef.PlayoffCups is { Length: > 0 } cups)
                {
                    SeedCupPlayoffs(division, stage, teams, venues, cups);
                }
                else
                {
                    SeedPlayoffStages(division, stage, teams, venues);
                }
            }
        }

        if (includePlayoffs && definition.CrossCup is not null)
        {
            SeedCrossDivisionCup(
                tournament, definition.CrossCup, allTeams, venues, definition.StageStartDate);
        }

        List<PlayerSanction> sanctions = SeedSanctions(regularGroupStages);

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
        ref int playerCounter)
    {
        Division division = new()
        {
            CreatedBy = CreatedBy,
            Name = divisionName,
            Slug = SlugGenerator.GenerateSlug($"{divisionName} {Guid.NewGuid()}"),
            Tournament = tournament,
            Stages = [],
        };

        List<Team> teams = [];

        for (int i = 0; i < teamNames.Length; i++)
        {
            Team team = new()
            {
                Id = Guid.NewGuid(),
                CreatedBy = CreatedBy,
                Name = teamNames[i],
                Slug = SlugGenerator.GenerateSlug(teamNames[i]),
                ThreeLetterCode = teamCodes[i],
                LogoUrl = $"https://placehold.co/128x128?text={teamCodes[i]}",
                ShirtColor = teamColors[i],
                JerseyStyle = teamStyles is not null && i < teamStyles.Length ? teamStyles[i] : "solid",
                ShirtSecondaryColor = teamSecondaryColors is not null && i < teamSecondaryColors.Length ? teamSecondaryColors[i] : null,
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

            for (int p = 0; p < 8; p++)
            {
                playerCounter++;

                string firstName = FirstNames[playerCounter % FirstNames.Length];
                string lastName = LastNames[(playerCounter * 3) % LastNames.Length];
                string documentNumber = (30000000 + playerCounter).ToString();

                Player player = new()
                {
                    CreatedBy = CreatedBy,
                    FirstName = firstName,
                    LastName = lastName,
                    Slug = SlugGenerator.GenerateSlug($"{lastName} {firstName} {documentNumber}"),
                    DocumentNumber = documentNumber,
                    IsSanctioned = false,
                    BirthDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc)
                        .AddYears(-(18 + (playerCounter % 20)))
                        .AddDays(playerCounter % 27),
                    SocialSecurity = $"20-{documentNumber}-3",
                    Team = team,
                };

                team.Players.Add(player);

                team.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
                {
                    CreatedBy = CreatedBy,
                    PlayerId = Guid.Empty,
                    Player = player,
                    TeamId = Guid.Empty,
                    Team = team,
                    TournamentId = Guid.Empty,
                    Tournament = tournament,
                });
            }

            teams.Add(team);
        }

        return (division, teams);
    }

    /// <summary>
    /// Builds a real circle-method single round-robin for
    /// <paramref name="teams"/>: for N teams there are N-1 jornadas of N/2
    /// matches each, every team plays exactly once per jornada, and no pair
    /// meets twice. Every match is finished with a decisive (never-tied) score
    /// in which the stronger team (earlier in <paramref name="teams"/>) wins,
    /// so <see cref="PositionCalculator"/> yields a full, sensible table.
    /// <see cref="Match.Round"/> is the 1-based jornada and
    /// <see cref="Match.MatchDate"/> is the calendar date for that jornada
    /// (Sundays for zones, Wednesdays for cross-division cups — HU-111).
    /// </summary>
    private static void SeedRoundRobinMatches(
        Stage stage,
        List<Team> teams,
        List<Venue> venues,
        DateTime anchorDate,
        bool isCrossDivisionCup)
    {
        int n = teams.Count;
        if (n < 2)
        {
            return;
        }

        // Circle method: index 0 stays fixed, indices 1..n-1 rotate one slot
        // each round. `slots` holds the ORIGINAL team indices in seeding order,
        // so a smaller index means a stronger team.
        int[] slots = [.. Enumerable.Range(0, n)];
        int rounds = n - 1;
        int matchIndex = 0;

        for (int r = 0; r < rounds; r++)
        {
            int round = r + 1;
            DateTime roundDate = RoundCalendar.DateForRound(anchorDate, round, isCrossDivisionCup);

            for (int i = 0; i < n / 2; i++)
            {
                int first = slots[i];
                int second = slots[n - 1 - i];

                // Alternate home/away by round so no team is always home.
                (int homeIdx, int visitorIdx) = round % 2 == 1 ? (first, second) : (second, first);

                Team home = teams[homeIdx];
                Team visitor = teams[visitorIdx];

                bool homeIsStronger = homeIdx < visitorIdx;
                int margin = 4 + ((homeIdx + visitorIdx + matchIndex) % 9);
                int winnerScore = 68 + ((matchIndex * 5) % 22);
                int loserScore = winnerScore - margin;
                int homeScore = homeIsStronger ? winnerScore : loserScore;
                int visitorScore = homeIsStronger ? loserScore : winnerScore;

                Match match = BuildFinishedMatch(
                    stage, home, visitor, homeScore, visitorScore, MatchType.Regular,
                    venues, roundDate, matchIndex, round);

                stage.Matches.Add(match);
                matchIndex++;
            }

            Rotate(slots);
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
    private static void SeedPlayoffStages(Division division, Stage groupStage, List<Team> teams, List<Venue> venues)
    {
        List<Position> standings = PositionCalculator.CalculatePositions(groupStage.Matches);
        Dictionary<Guid, Team> teamsById = teams.ToDictionary(t => t.Id);
        List<Guid> orderedTeamIds = [.. standings.Select(p => p.TeamId)];
        List<(Guid HomeTeamId, Guid? VisitorTeamId)> semiFinalPairs = PlayoffSeeder.SeedPairs(orderedTeamIds);

        Stage semiFinalStage = new()
        {
            CreatedBy = CreatedBy,
            Name = StageTemplate.SemiFinal.Name,
            Slug = SlugGenerator.GenerateSlug($"{StageTemplate.SemiFinal.Name} {division.Name} {Guid.NewGuid()}"),
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
            Slug = SlugGenerator.GenerateSlug($"{StageTemplate.ThirdPlace.Name} {division.Name} {Guid.NewGuid()}"),
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
            Slug = SlugGenerator.GenerateSlug($"{StageTemplate.Final.Name} {division.Name} {Guid.NewGuid()}"),
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
    /// Builds one SemiFinal + Final bracket per named position-range cup
    /// (e.g. Copa Oro for positions 1-4, Copa Plata for 5-8), each seeded from
    /// the REAL final group standings restricted to the cup's position range,
    /// and registers the matching <see cref="DivisionPlayoffMapping"/> on the
    /// division. Each cup's stages carry <see cref="Stage.BracketName"/> and
    /// their names embed the cup so they stay unique within the division.
    /// </summary>
    private static void SeedCupPlayoffs(
        Division division,
        Stage groupStage,
        List<Team> teams,
        List<Venue> venues,
        PlayoffCupDefinition[] cups)
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

            if (seedIds.Count < 2)
            {
                continue;
            }

            List<Team> cupTeams = [.. seedIds.Select(id => teamsById[id])];
            List<(Guid HomeTeamId, Guid? VisitorTeamId)> semiPairs = PlayoffSeeder.SeedPairs(seedIds);

            Stage semiStage = new()
            {
                CreatedBy = CreatedBy,
                Name = $"{StageTemplate.SemiFinal.Name} {cup.BracketName}",
                Slug = SlugGenerator.GenerateSlug($"{StageTemplate.SemiFinal.Name} {cup.BracketName} {division.Name} {Guid.NewGuid()}"),
                StageType = StageType.SemiFinal,
                IsActive = true,
                IsElimination = true,
                BracketName = cup.BracketName,
                BestOf = cup.BestOf,
                StartDate = groupStage.EndDate.AddDays(StageTemplate.StandardGapDays),
                EndDate = groupStage.EndDate.AddDays(StageTemplate.StandardGapDays + StageTemplate.DurationDays),
                DivisionId = Guid.Empty,
                Division = division,
                Matches = [],
                Order = order++,
            };
            division.Stages.Add(semiStage);
            AddStageTeamMatches(semiStage, cupTeams);

            List<Team> winners = [];
            for (int i = 0; i < semiPairs.Count; i++)
            {
                (Guid homeId, Guid? visitorId) = semiPairs[i];
                Team home = teamsById[homeId];

                if (visitorId is null)
                {
                    // BYE: the top seed advances automatically (no match).
                    winners.Add(home);
                    continue;
                }

                Team visitor = teamsById[visitorId.Value];
                Match match = BuildFinishedMatch(
                    semiStage, home, visitor, 79 + (i * 3), 66 + (i * 2), MatchType.Playoff,
                    venues, semiStage.StartDate.AddDays(i), i, round: null);
                semiStage.Matches.Add(match);
                winners.Add(match.WinningTeam!);
            }

            if (winners.Count < 2)
            {
                continue;
            }

            Stage finalStage = new()
            {
                CreatedBy = CreatedBy,
                Name = $"{StageTemplate.Final.Name} {cup.BracketName}",
                Slug = SlugGenerator.GenerateSlug($"{StageTemplate.Final.Name} {cup.BracketName} {division.Name} {Guid.NewGuid()}"),
                StageType = StageType.Final,
                IsActive = true,
                IsElimination = true,
                BracketName = cup.BracketName,
                BestOf = cup.BestOf,
                StartDate = semiStage.EndDate.AddDays(StageTemplate.StandardGapDays),
                EndDate = semiStage.EndDate.AddDays(StageTemplate.StandardGapDays + StageTemplate.DurationDays),
                DivisionId = Guid.Empty,
                Division = division,
                Matches = [],
                Order = order++,
            };
            division.Stages.Add(finalStage);
            AddStageTeamMatches(finalStage, winners);

            Match finalMatch = BuildFinishedMatch(
                finalStage, winners[0], winners[1], 84, 71, MatchType.Playoff,
                venues, finalStage.StartDate, 0, round: null);
            finalStage.Matches.Add(finalMatch);
        }
    }

    /// <summary>
    /// Builds the cross-division cup (HU-110): one division with
    /// <see cref="Division.IsCrossDivisionCup"/> = true whose teams are ALL the
    /// tournament's teams, split into <paramref name="crossCup"/>.GroupCount
    /// finished round-robin groups ("Grupo 1".."Grupo N", jornadas on
    /// Wednesdays). The top <c>QualifiersPerGroup</c> of every group are pooled
    /// via <see cref="CrossCupGroupSeeder"/> into a single SemiFinal + Final
    /// bracket. Each team gets a StageTeamMatch in its cup group IN ADDITION to
    /// its regular zone — cross cups are exempt from one-team-one-zone.
    /// </summary>
    private static void SeedCrossDivisionCup(
        Tournament tournament,
        CrossCupDefinition crossCup,
        List<Team> allTeams,
        List<Venue> venues,
        DateTime anchorDate)
    {
        Division cupDivision = new()
        {
            CreatedBy = CreatedBy,
            Name = crossCup.DivisionName,
            Slug = SlugGenerator.GenerateSlug($"{crossCup.DivisionName} {Guid.NewGuid()}"),
            Tournament = tournament,
            Stages = [],
            IsCrossDivisionCup = true,
            QualifiersPerGroup = crossCup.QualifiersPerGroup,
        };
        tournament.Divisions.Add(cupDivision);

        int groupCount = crossCup.GroupCount;
        int perGroup = allTeams.Count / groupCount;

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
                Slug = SlugGenerator.GenerateSlug($"Grupo {g + 1} {cupDivision.Name} {Guid.NewGuid()}"),
                StageType = StageType.Group,
                IsActive = true,
                StartDate = anchorDate,
                EndDate = anchorDate.AddDays(7 * groupTeams.Count),
                DivisionId = Guid.Empty,
                Division = cupDivision,
                Matches = [],
                Order = g,
                RoundRobinLegs = 1,
            };
            cupDivision.Stages.Add(groupStage);
            AddStageTeamMatches(groupStage, groupTeams);

            SeedRoundRobinMatches(
                groupStage, groupTeams, venues, anchorDate, isCrossDivisionCup: true);

            groupStandings.Add(PositionCalculator.CalculatePositions(groupStage.Matches));
        }

        List<Guid> seedOrder = CrossCupGroupSeeder.ResolveSeedOrder(groupStandings, crossCup.QualifiersPerGroup);
        if (seedOrder.Count < 2)
        {
            return;
        }

        List<Team> pooledTeams = [.. seedOrder.Select(id => teamsById[id])];
        List<(Guid HomeTeamId, Guid? VisitorTeamId)> semiPairs = PlayoffSeeder.SeedPairs(seedOrder);

        DateTime bracketStart = anchorDate.AddDays(7 * (perGroup + 1));

        Stage semiStage = new()
        {
            CreatedBy = CreatedBy,
            Name = StageTemplate.SemiFinal.Name,
            Slug = SlugGenerator.GenerateSlug($"{StageTemplate.SemiFinal.Name} {cupDivision.Name} {Guid.NewGuid()}"),
            StageType = StageType.SemiFinal,
            IsActive = true,
            IsElimination = true,
            StartDate = bracketStart,
            EndDate = bracketStart.AddDays(StageTemplate.DurationDays),
            DivisionId = Guid.Empty,
            Division = cupDivision,
            Matches = [],
            Order = groupCount,
        };
        cupDivision.Stages.Add(semiStage);
        AddStageTeamMatches(semiStage, pooledTeams);

        List<Team> winners = [];
        for (int i = 0; i < semiPairs.Count; i++)
        {
            (Guid homeId, Guid? visitorId) = semiPairs[i];
            Team home = teamsById[homeId];

            if (visitorId is null)
            {
                winners.Add(home);
                continue;
            }

            Team visitor = teamsById[visitorId.Value];
            Match match = BuildFinishedMatch(
                semiStage, home, visitor, 80 + (i * 3), 67 + (i * 2), MatchType.Playoff,
                venues, semiStage.StartDate.AddDays(i), i, round: null);
            semiStage.Matches.Add(match);
            winners.Add(match.WinningTeam!);
        }

        if (winners.Count < 2)
        {
            return;
        }

        Stage finalStage = new()
        {
            CreatedBy = CreatedBy,
            Name = StageTemplate.Final.Name,
            Slug = SlugGenerator.GenerateSlug($"{StageTemplate.Final.Name} {cupDivision.Name} {Guid.NewGuid()}"),
            StageType = StageType.Final,
            IsActive = true,
            IsElimination = true,
            StartDate = semiStage.EndDate.AddDays(StageTemplate.StandardGapDays),
            EndDate = semiStage.EndDate.AddDays(StageTemplate.StandardGapDays + StageTemplate.DurationDays),
            DivisionId = Guid.Empty,
            Division = cupDivision,
            Matches = [],
            Order = groupCount + 1,
        };
        cupDivision.Stages.Add(finalStage);
        AddStageTeamMatches(finalStage, winners);

        Match finalMatch = BuildFinishedMatch(
            finalStage, winners[0], winners[1], 86, 73, MatchType.Playoff,
            venues, finalStage.StartDate, 0, round: null);
        finalStage.Matches.Add(finalMatch);
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
        Venue venue = venues[venueIndex % venues.Count];
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
    /// Adds a scorer plus Points/Assists PlayerStatistic rows for one team in a
    /// match (HU-72: the goleadores ranking reads PlayerStatistic). Skips a
    /// zero-score team so no phantom scorer is created.
    /// </summary>
    private static void AddScoring(Match match, Team team, int score, int scorerSeed)
    {
        if (score <= 0)
        {
            return;
        }

        Player scorer = team.Players.ElementAt(scorerSeed % team.Players.Count);

        match.Scorers.Add(new Scorer
        {
            CreatedBy = CreatedBy,
            PlayerId = Guid.Empty,
            Player = scorer,
            Points = score,
            MatchId = Guid.Empty,
            Match = match,
        });
        match.PlayerStatistics.Add(new PlayerStatistic
        {
            CreatedBy = CreatedBy,
            Value = score,
            PlayerId = Guid.Empty,
            Player = scorer,
            MatchId = Guid.Empty,
            Match = match,
            Type = StatisticType.Points,
        });
        match.PlayerStatistics.Add(new PlayerStatistic
        {
            CreatedBy = CreatedBy,
            Value = 1,
            PlayerId = Guid.Empty,
            Player = scorer,
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
    private static List<PlayerSanction> SeedSanctions(List<Stage> groupStages)
    {
        List<PlayerSanction> sanctions = [];

        List<Match> matches = [.. groupStages.SelectMany(s => s.Matches)];
        if (matches.Count == 0)
        {
            return sanctions;
        }

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

            Match match = matches[(i * 5) % matches.Count];
            Team losingTeam = match.WinningTeam == match.HomeTeam ? match.VisitorTeam! : match.HomeTeam!;

            PlayerSanction sanction = new()
            {
                CreatedBy = CreatedBy,
                Duration = duration,
                IssuedDate = active ? match.MatchDate : match.MatchDate.AddDays(-30),
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
            }

            if (appeal == SanctionAppealStatus.Pending)
            {
                sanction.AppealReason = "El jugador sostiene que la falta no existió.";
                sanction.AppealDate = match.MatchDate.AddDays(1);
            }
            else if (appeal == SanctionAppealStatus.Rejected)
            {
                sanction.AppealReason = "Se solicitó revisión de la jugada.";
                sanction.AppealDate = match.MatchDate.AddDays(-28);
                sanction.AppealResolution = "El tribunal ratificó la sanción.";
                sanction.AppealResolvedDate = match.MatchDate.AddDays(-25);
            }

            sanctions.Add(sanction);
        }

        return sanctions;
    }
}
