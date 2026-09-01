using Application.DTOs.Tournament.Response;

using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Helper.Tournament;

/// <summary>
/// Single source of truth for whether a tournament can be COMPLETED once it is
/// started (HU-109). After registration closes, teams are assigned to divisions
/// and the tournament is started (transition to Ongoing, which generates the
/// fixture). This validator makes it impossible to start a tournament in a
/// state that could never be completed, by inspecting a fully-loaded tournament
/// graph and its enrolled-team registrations and returning one structured issue
/// per violation.
///
/// Domain vocabulary:
/// <list type="bullet">
/// <item>A "zone" is a regular <see cref="Division"/>
/// (<see cref="Division.IsCrossDivisionCup"/> false) with a
/// <see cref="StageType.Group"/> stage; teams are assigned to it through
/// <see cref="StageTeamMatch"/> rows.</item>
/// <item>The cross-division cup is a <see cref="Division"/> flagged
/// <see cref="Division.IsCrossDivisionCup"/> — a parallel competition that a
/// team plays IN ADDITION to its zone, so being in the cup never counts as the
/// team's zone.</item>
/// <item>Enrolled teams are the tournament's
/// <see cref="TeamTournamentRegistration"/> rows.</item>
/// </list>
/// </summary>
public static class TournamentCompletabilityValidator
{
    /// <summary>
    /// Minimum number of teams a zone (and each cross-cup group) must have
    /// assigned for the competition it feeds to be playable/completable.
    /// </summary>
    public const int MinTeamsPerZone = 2;

    /// <summary>
    /// Minimum number of players an enrolled team must have registered on its
    /// season roster (<see cref="PlayerTeamRegistration"/>) for the tournament
    /// to be completable — five is a basketball team's on-court minimum, so
    /// fewer players means the team could never field a lineup.
    /// </summary>
    public const int MinPlayersPerTeam = 5;

    /// <summary>
    /// Evaluates every completability rule against the loaded tournament graph
    /// and returns one issue per violation. An empty result means the
    /// tournament is completable and may be started.
    /// </summary>
    /// <param name="tournament">
    /// The tournament with its Divisions loaded, each division's Stages (and
    /// each stage's StageTeamMatches) and PlayoffMappings loaded.
    /// </param>
    /// <param name="enrolledRegistrations">
    /// The tournament's team registrations (enrolled teams), each ideally with
    /// its Team loaded so team names can be reported.
    /// </param>
    /// <param name="playerCountsByTeam">
    /// Each enrolled team's registered player count for this tournament
    /// (<see cref="PlayerTeamRegistration"/>), used by the TeamTooFewPlayers
    /// rule. Omitted (null) skips that rule entirely — callers that don't have
    /// this data loaded (e.g. tests focused on the zone/team rules) are
    /// unaffected.
    /// </param>
    public static IReadOnlyList<CompletabilityIssue> Validate(
        Domain.Entities.Models.Tournament tournament,
        IReadOnlyCollection<TeamTournamentRegistration> enrolledRegistrations,
        IReadOnlyDictionary<Guid, int>? playerCountsByTeam = null)
    {
        ArgumentNullException.ThrowIfNull(tournament);
        ArgumentNullException.ThrowIfNull(enrolledRegistrations);

        List<CompletabilityIssue> issues = [];

        List<Division> regularDivisions =
            [.. tournament.Divisions.Where(division => !division.IsCrossDivisionCup)];
        List<Division> crossCupDivisions =
            [.. tournament.Divisions.Where(division => division.IsCrossDivisionCup)];

        Dictionary<Guid, string> teamNames = BuildTeamNames(tournament, enrolledRegistrations);

        // Distinct teams assigned to each regular zone (across its Group stages).
        Dictionary<Guid, HashSet<Guid>> assignedTeamsByDivision = regularDivisions
            .ToDictionary(division => division.Id, division => GroupStageTeamIds(division));

        // Reverse map: which regular zones each team is assigned to.
        Dictionary<Guid, HashSet<Guid>> zonesByTeam = [];
        foreach ((Guid divisionId, HashSet<Guid> teamIds) in assignedTeamsByDivision)
        {
            foreach (Guid teamId in teamIds)
            {
                if (!zonesByTeam.TryGetValue(teamId, out HashSet<Guid>? zones))
                {
                    zonesByTeam[teamId] = zones = [];
                }

                zones.Add(divisionId);
            }
        }

        // Rule 1 — ZoneTooFewTeams: every regular zone needs >= MinTeamsPerZone.
        foreach (Division division in regularDivisions)
        {
            int assigned = assignedTeamsByDivision[division.Id].Count;
            if (assigned < MinTeamsPerZone)
            {
                issues.Add(new CompletabilityIssue
                {
                    Code = CompletabilityIssueCodes.ZoneTooFewTeams,
                    DivisionName = division.Name,
                    AssignedTeams = assigned,
                });
            }
        }

        // Rule 2 — TeamNotAssigned: every enrolled team must sit in some zone.
        // Being only in the cross cup does NOT satisfy this.
        HashSet<Guid> enrolledTeamIds = [.. enrolledRegistrations.Select(registration => registration.TeamId)];
        foreach (Guid teamId in enrolledTeamIds.OrderBy(id => ResolveTeamName(teamNames, id)))
        {
            if (!zonesByTeam.ContainsKey(teamId))
            {
                issues.Add(new CompletabilityIssue
                {
                    Code = CompletabilityIssueCodes.TeamNotAssigned,
                    TeamName = ResolveTeamName(teamNames, teamId),
                });
            }
        }

        // Rule 3 — TeamInMultipleZones: a team assigned to more than one zone.
        foreach ((Guid teamId, HashSet<Guid> zones) in zonesByTeam
            .Where(entry => entry.Value.Count > 1)
            .OrderBy(entry => ResolveTeamName(teamNames, entry.Key)))
        {
            issues.Add(new CompletabilityIssue
            {
                Code = CompletabilityIssueCodes.TeamInMultipleZones,
                TeamName = ResolveTeamName(teamNames, teamId),
            });
        }

        // Rule 4 — PlayoffRangeExceedsTeams: a mapping whose FromPosition is
        // beyond the zone's assigned team count seeds a cup from positions that
        // will never exist.
        foreach (Division division in regularDivisions)
        {
            int assigned = assignedTeamsByDivision[division.Id].Count;
            foreach (DivisionPlayoffMapping mapping in division.PlayoffMappings
                .Where(mapping => mapping.FromPosition > assigned)
                .OrderBy(mapping => mapping.FromPosition))
            {
                issues.Add(new CompletabilityIssue
                {
                    Code = CompletabilityIssueCodes.PlayoffRangeExceedsTeams,
                    DivisionName = division.Name,
                    FromPosition = mapping.FromPosition,
                    AssignedTeams = assigned,
                });
            }
        }

        // Rule 5 — CrossCupGroupTooFewTeams: each cross-cup Group stage needs
        // >= MinTeamsPerZone assigned teams.
        foreach (Division crossCup in crossCupDivisions)
        {
            foreach (Stage groupStage in crossCup.Stages.Where(stage => stage.StageType == StageType.Group))
            {
                int assigned = groupStage.StageTeamMatches.Select(match => match.TeamId).Distinct().Count();
                if (assigned < MinTeamsPerZone)
                {
                    issues.Add(new CompletabilityIssue
                    {
                        Code = CompletabilityIssueCodes.CrossCupGroupTooFewTeams,
                        DivisionName = crossCup.Name,
                        AssignedTeams = assigned,
                    });
                }
            }
        }

        // Rule 6 — TeamTooFewPlayers: every enrolled team needs >= MinPlayersPerTeam
        // registered players — basketball's on-court minimum — to ever field a
        // lineup. Skipped when the caller didn't load player-count data.
        if (playerCountsByTeam is not null)
        {
            foreach (Guid teamId in enrolledTeamIds.OrderBy(id => ResolveTeamName(teamNames, id)))
            {
                int playerCount = playerCountsByTeam.GetValueOrDefault(teamId);
                if (playerCount < MinPlayersPerTeam)
                {
                    issues.Add(new CompletabilityIssue
                    {
                        Code = CompletabilityIssueCodes.TeamTooFewPlayers,
                        TeamName = ResolveTeamName(teamNames, teamId),
                        PlayerCount = playerCount,
                    });
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// Distinct ids of the teams assigned to a division's Group stage(s) via
    /// <see cref="StageTeamMatch"/> rows.
    /// </summary>
    private static HashSet<Guid> GroupStageTeamIds(Division division)
    {
        return [.. division.Stages
            .Where(stage => stage.StageType == StageType.Group)
            .SelectMany(stage => stage.StageTeamMatches)
            .Select(match => match.TeamId)];
    }

    /// <summary>
    /// Builds a team-id → team-name lookup from every source that carries a
    /// Team navigation (the enrolled registrations and the stage assignments),
    /// so an issue can report a readable team name whichever rule fired.
    /// </summary>
    private static Dictionary<Guid, string> BuildTeamNames(
        Domain.Entities.Models.Tournament tournament,
        IReadOnlyCollection<TeamTournamentRegistration> enrolledRegistrations)
    {
        Dictionary<Guid, string> names = [];

        foreach (TeamTournamentRegistration registration in enrolledRegistrations
            .Where(registration => registration.Team is not null))
        {
            names[registration.TeamId] = registration.Team!.Name;
        }

        foreach (StageTeamMatch match in tournament.Divisions
            .SelectMany(division => division.Stages)
            .SelectMany(stage => stage.StageTeamMatches)
            .Where(match => match.Team is not null))
        {
            names[match.TeamId] = match.Team!.Name;
        }

        return names;
    }

    /// <summary>
    /// Resolves a team's display name, falling back to its id when no Team
    /// navigation was loaded for it.
    /// </summary>
    private static string ResolveTeamName(Dictionary<Guid, string> teamNames, Guid teamId)
    {
        return teamNames.TryGetValue(teamId, out string? name) ? name : teamId.ToString();
    }
}
