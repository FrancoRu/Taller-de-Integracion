using Application.DTOs.Tournament.Response;

using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

using SubGroupDistributionHelper = Application.Utils.Helper.SubGroupDistribution.SubGroupDistribution;

namespace Application.Utils.Helper.Tournament;

/// <summary>
/// Single source of truth for whether a tournament can be completed once it is started.
/// </summary>
public static class TournamentCompletabilityValidator
{
    /// <summary>
    /// Minimum number of teams a zone, and each cross-cup group, must have assigned for the competition it feeds to be playable or completable.
    /// </summary>
    public const int MinTeamsPerZone = 2;

    /// <summary>
    /// Minimum number of HABILITADO players an enrolled team must have for the tournament to be completable.
    /// </summary>
    public const int MinPlayersPerTeam = 4;

    /// <summary>
    /// Evaluates every completability rule against the loaded tournament graph and returns one issue per violation.
    /// </summary>
    /// <param name="tournament">
    /// The tournament with its Divisions loaded, each division's Stages, and
    /// each stage's StageTeamMatches and PlayoffMappings loaded.
    /// </param>
    /// <param name="enrolledRegistrations">
    /// The tournament's team registrations, the enrolled teams, each ideally with
    /// its Team loaded so team names can be reported.
    /// </param>
    /// <param name="playerCountsByTeam">
    /// Each enrolled team's HABILITADO player count for this tournament, per
    /// PlayerTeamRegistration.IsHabilitado — NOT its raw
    /// registered-roster size, since a registered-but-not-yet-approved player
    /// could never legally play anyway. Used by the TeamTooFewPlayers rule.
    /// Omitted, null, skips that rule entirely — callers that don't have this
    /// data loaded are unaffected.
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

        Dictionary<Guid, HashSet<Guid>> assignedTeamsByDivision = regularDivisions
            .ToDictionary(division => division.Id, division => GroupStageTeamIds(division));

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

        // A team assigned only to the cross cup does not satisfy zone assignment.
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

        // A mapping whose FromPosition exceeds the zone's assigned team count seeds a cup from positions that will never exist.
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

        // A rebuild always deals a balanced split, so this mainly catches a hand-edited imbalance or a group left below the minimum after a manual move.
        foreach (Division division in regularDivisions)
        {
            List<Stage> subGroupStages = [.. division.Stages.Where(stage => stage.StageType == StageType.Group)];
            if (subGroupStages.Count <= 1)
            {
                continue;
            }

            List<int> subGroupSizes = [.. subGroupStages
                .Select(stage => stage.StageTeamMatches.Select(match => match.TeamId).Distinct().Count())];

            int smallestSubGroup = subGroupSizes.Min();
            int largestSubGroup = subGroupSizes.Max();

            if (smallestSubGroup < SubGroupDistributionHelper.MinTeamsPerSubGroup || largestSubGroup - smallestSubGroup >= 2)
            {
                issues.Add(new CompletabilityIssue
                {
                    Code = CompletabilityIssueCodes.SubGroupTooFewTeams,
                    DivisionName = division.Name,
                    AssignedTeams = smallestSubGroup,
                });
            }
        }

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

        // Every enrolled team needs at least MinPlayersPerTeam HABILITADO players, basketball's minimum to field a legal lineup without a walkover, skipped when the caller did not load player-count data.
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
    /// Distinct ids of the teams assigned to a division's Group stages via StageTeamMatch rows.
    /// </summary>
    private static HashSet<Guid> GroupStageTeamIds(Division division)
    {
        return [.. division.Stages
            .Where(stage => stage.StageType == StageType.Group)
            .SelectMany(stage => stage.StageTeamMatches)
            .Select(match => match.TeamId)];
    }

    /// <summary>
    /// Builds a team-id to team-name lookup from every source that carries a Team navigation, so an issue can report a readable team name whichever rule fired.
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
    /// Resolves a team's display name, falling back to its id when no Team navigation was loaded for it.
    /// </summary>
    private static string ResolveTeamName(Dictionary<Guid, string> teamNames, Guid teamId)
    {
        return teamNames.TryGetValue(teamId, out string? name) ? name : teamId.ToString();
    }
}
