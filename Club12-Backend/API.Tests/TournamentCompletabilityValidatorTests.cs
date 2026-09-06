using Application.DTOs.Tournament.Response;

using Domain.Entities.Models;
using Domain.Enums;

using System.Collections.Generic;
using System.Linq;

using CompletabilityValidator = Application.Utils.Helper.Tournament.TournamentCompletabilityValidator;

namespace API.Tests;

/// <summary>
/// HU-109: focused unit tests for the completability validator — the single
/// source of truth deciding whether a tournament can be started. Each test
/// builds an in-memory tournament graph (no database) and asserts exactly which
/// rule fires. A fully-valid tournament returns no issues (CanStart == true).
/// </summary>
public class TournamentCompletabilityValidatorTests
{
    [Fact]
    public void Validate_FullyValidTournament_ReturnsNoIssues()
    {
        Tournament tournament = NewTournament();
        Team t1 = MakeTeam("Alpha");
        Team t2 = MakeTeam("Bravo");
        Team t3 = MakeTeam("Charlie");
        Team t4 = MakeTeam("Delta");

        AddZone(tournament, "Zone A", [t1, t2], (1, 2, "Oro"));
        AddZone(tournament, "Zone B", [t3, t4], (1, 2, "Plata"));
        // Cross-division cup group draws from teams already in their zones.
        AddCrossCup(tournament, "Copa Club12", [t1, t3]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament, [Reg(tournament, t1), Reg(tournament, t2), Reg(tournament, t3), Reg(tournament, t4)]);

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_ZoneWithFewerThanTwoTeams_FiresZoneTooFewTeams()
    {
        Tournament tournament = NewTournament();
        Team t1 = MakeTeam("Solo");
        AddZone(tournament, "Lonely Zone", [t1]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament, [Reg(tournament, t1)]);

        CompletabilityIssue issue = Assert.Single(issues, i => i.Code == CompletabilityIssueCodes.ZoneTooFewTeams);
        Assert.Equal("Lonely Zone", issue.DivisionName);
        Assert.Equal(1, issue.AssignedTeams);
    }

    [Fact]
    public void Validate_EnrolledTeamNotAssignedToAnyZone_FiresTeamNotAssigned()
    {
        Tournament tournament = NewTournament();
        Team t1 = MakeTeam("Alpha");
        Team t2 = MakeTeam("Bravo");
        Team orphan = MakeTeam("Orphan");
        AddZone(tournament, "Zone A", [t1, t2]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament, [Reg(tournament, t1), Reg(tournament, t2), Reg(tournament, orphan)]);

        CompletabilityIssue issue = Assert.Single(issues, i => i.Code == CompletabilityIssueCodes.TeamNotAssigned);
        Assert.Equal("Orphan", issue.TeamName);
    }

    [Fact]
    public void Validate_EnrolledTeamOnlyInCrossCup_StillFiresTeamNotAssigned()
    {
        // Being in the cross-division cup does NOT count as the team's zone.
        Tournament tournament = NewTournament();
        Team t1 = MakeTeam("Alpha");
        Team t2 = MakeTeam("Bravo");
        Team cupOnly = MakeTeam("CupOnly");
        AddZone(tournament, "Zone A", [t1, t2]);
        AddCrossCup(tournament, "Copa Club12", [t1, cupOnly]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament, [Reg(tournament, t1), Reg(tournament, t2), Reg(tournament, cupOnly)]);

        CompletabilityIssue issue = Assert.Single(issues, i => i.Code == CompletabilityIssueCodes.TeamNotAssigned);
        Assert.Equal("CupOnly", issue.TeamName);
    }

    [Fact]
    public void Validate_TeamAssignedToTwoZones_FiresTeamInMultipleZones()
    {
        Tournament tournament = NewTournament();
        Team shared = MakeTeam("Shared");
        Team t2 = MakeTeam("Bravo");
        Team t3 = MakeTeam("Charlie");
        AddZone(tournament, "Zone A", [shared, t2]);
        AddZone(tournament, "Zone B", [shared, t3]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament, [Reg(tournament, shared), Reg(tournament, t2), Reg(tournament, t3)]);

        CompletabilityIssue issue = Assert.Single(issues, i => i.Code == CompletabilityIssueCodes.TeamInMultipleZones);
        Assert.Equal("Shared", issue.TeamName);
    }

    [Fact]
    public void Validate_PlayoffRangeBeyondAssignedTeams_FiresPlayoffRangeExceedsTeams()
    {
        Tournament tournament = NewTournament();
        Team t1 = MakeTeam("Alpha");
        Team t2 = MakeTeam("Bravo");
        // Zone has 2 teams but a mapping seeds a cup from position 3.
        AddZone(tournament, "Zone A", [t1, t2], (1, 2, "Oro"), (3, 4, "Plata"));

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament, [Reg(tournament, t1), Reg(tournament, t2)]);

        CompletabilityIssue issue = Assert.Single(issues, i => i.Code == CompletabilityIssueCodes.PlayoffRangeExceedsTeams);
        Assert.Equal("Zone A", issue.DivisionName);
        Assert.Equal(3, issue.FromPosition);
        Assert.Equal(2, issue.AssignedTeams);
    }

    [Fact]
    public void Validate_CrossCupGroupWithFewerThanTwoTeams_FiresCrossCupGroupTooFewTeams()
    {
        Tournament tournament = NewTournament();
        Team t1 = MakeTeam("Alpha");
        Team t2 = MakeTeam("Bravo");
        AddZone(tournament, "Zone A", [t1, t2]);
        // Cross-cup group with a single assigned team.
        AddCrossCup(tournament, "Copa Club12", [t1]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament, [Reg(tournament, t1), Reg(tournament, t2)]);

        CompletabilityIssue issue = Assert.Single(issues, i => i.Code == CompletabilityIssueCodes.CrossCupGroupTooFewTeams);
        Assert.Equal("Copa Club12", issue.DivisionName);
        Assert.Equal(1, issue.AssignedTeams);
    }

    [Fact]
    public void Validate_TeamWithFewerThanFourHabilitadoPlayers_FiresTeamTooFewPlayers()
    {
        Tournament tournament = NewTournament();
        Team t1 = MakeTeam("Alpha");
        Team t2 = MakeTeam("Bravo");
        AddZone(tournament, "Zone A", [t1, t2]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament,
            [Reg(tournament, t1), Reg(tournament, t2)],
            playerCountsByTeam: new Dictionary<Guid, int> { [t1.Id] = 4, [t2.Id] = 3 });

        CompletabilityIssue issue = Assert.Single(issues, i => i.Code == CompletabilityIssueCodes.TeamTooFewPlayers);
        Assert.Equal("Bravo", issue.TeamName);
        Assert.Equal(3, issue.PlayerCount);
    }

    [Fact]
    public void Validate_TeamMissingFromPlayerCounts_TreatedAsZeroPlayers_FiresTeamTooFewPlayers()
    {
        Tournament tournament = NewTournament();
        Team t1 = MakeTeam("Alpha");
        AddZone(tournament, "Zone A", [t1, MakeTeam("Bravo")]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament,
            [Reg(tournament, t1)],
            playerCountsByTeam: new Dictionary<Guid, int>());

        CompletabilityIssue issue = Assert.Single(issues, i => i.Code == CompletabilityIssueCodes.TeamTooFewPlayers);
        Assert.Equal("Alpha", issue.TeamName);
        Assert.Equal(0, issue.PlayerCount);
    }

    [Fact]
    public void Validate_PlayerCountsOmitted_SkipsTeamTooFewPlayersRule()
    {
        // Callers that don't load player-count data (e.g. other unit tests)
        // must not be affected by this rule.
        Tournament tournament = NewTournament();
        Team t1 = MakeTeam("Alpha");
        Team t2 = MakeTeam("Bravo");
        AddZone(tournament, "Zone A", [t1, t2]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament, [Reg(tournament, t1), Reg(tournament, t2)]);

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_EmptyTournament_ReturnsNoIssues()
    {
        // No divisions and no enrolled teams: nothing to complete, nothing blocks.
        Tournament tournament = NewTournament();

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(tournament, []);

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_SubGroupBelowMinimum_ReportsSubGroupTooFewTeams()
    {
        Tournament tournament = NewTournament();
        List<Team> groupA = [.. Enumerable.Range(0, 4).Select(i => MakeTeam($"A{i}"))];
        List<Team> groupB = [.. Enumerable.Range(0, 3).Select(i => MakeTeam($"B{i}"))];
        AddZoneWithSubGroups(tournament, "Zone A", [groupA, groupB]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament, [.. groupA.Concat(groupB).Select(team => Reg(tournament, team))]);

        CompletabilityIssue issue = Assert.Single(issues, i => i.Code == CompletabilityIssueCodes.SubGroupTooFewTeams);
        Assert.Equal("Zone A", issue.DivisionName);
        Assert.Equal(3, issue.AssignedTeams);
    }

    [Fact]
    public void Validate_SubGroupsBalancedAndAboveMinimum_NoIssue()
    {
        Tournament tournament = NewTournament();
        List<Team> groupA = [.. Enumerable.Range(0, 4).Select(i => MakeTeam($"A{i}"))];
        List<Team> groupB = [.. Enumerable.Range(0, 4).Select(i => MakeTeam($"B{i}"))];
        AddZoneWithSubGroups(tournament, "Zone A", [groupA, groupB]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament, [.. groupA.Concat(groupB).Select(team => Reg(tournament, team))]);

        Assert.DoesNotContain(issues, i => i.Code == CompletabilityIssueCodes.SubGroupTooFewTeams);
    }

    [Fact]
    public void Validate_HandEditedImbalanceAcrossSubGroups_ReportsIssue()
    {
        Tournament tournament = NewTournament();
        List<Team> groupA = [.. Enumerable.Range(0, 4).Select(i => MakeTeam($"A{i}"))];
        List<Team> groupB = [.. Enumerable.Range(0, 6).Select(i => MakeTeam($"B{i}"))];
        AddZoneWithSubGroups(tournament, "Zone A", [groupA, groupB]);

        IReadOnlyList<CompletabilityIssue> issues = CompletabilityValidator.Validate(
            tournament, [.. groupA.Concat(groupB).Select(team => Reg(tournament, team))]);

        CompletabilityIssue issue = Assert.Single(issues, i => i.Code == CompletabilityIssueCodes.SubGroupTooFewTeams);
        Assert.Equal("Zone A", issue.DivisionName);
    }

    // ---- in-memory graph builders ----

    private static Tournament NewTournament()
    {
        return new Tournament
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Description = string.Empty,
            Slug = "test",
            TeamRegistrationDeadline = default,
            StartDate = default,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };
    }

    private static Team MakeTeam(string name)
    {
        return new Team
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLowerInvariant(),
            ThreeLetterCode = "TMX",
            LogoUrl = string.Empty,
            ShirtColor = string.Empty,
            Players = [],
            CreatedBy = "test",
        };
    }

    private static void AddZone(
        Tournament tournament,
        string name,
        IReadOnlyList<Team> groupTeams,
        params (int From, int To, string Destination)[] mappings)
    {
        AddDivision(tournament, name, groupTeams, crossCup: false, mappings);
    }

    private static void AddCrossCup(Tournament tournament, string name, IReadOnlyList<Team> groupTeams)
    {
        AddDivision(tournament, name, groupTeams, crossCup: true, []);
    }

    /// <summary>
    /// Builds a regular division with one Group stage per sub-group, each seeded with its own teams.
    /// </summary>
    private static void AddZoneWithSubGroups(
        Tournament tournament, string name, List<List<Team>> subGroupTeams)
    {
        Division division = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLowerInvariant(),
            Tournament = tournament,
            TournamentId = tournament.Id,
            IsCrossDivisionCup = false,
            Stages = [],
            CreatedBy = "test",
            PlayoffMappings = [],
        };

        for (int i = 0; i < subGroupTeams.Count; i++)
        {
            Stage group = new()
            {
                Id = Guid.NewGuid(),
                Name = $"Grupo {(char) ('A' + i)}",
                Slug = $"grupo-{name.ToLowerInvariant()}-{i}",
                StageType = StageType.Group,
                IsActive = true,
                StartDate = default,
                EndDate = default,
                DivisionId = division.Id,
                Division = division,
                Matches = [],
                StageTeamMatches = [],
                CreatedBy = "test",
            };

            foreach (Team team in subGroupTeams[i])
            {
                group.StageTeamMatches.Add(new StageTeamMatch
                {
                    StageId = group.Id,
                    TeamId = team.Id,
                    Team = team,
                    CreatedBy = "test",
                });
            }

            division.Stages.Add(group);
        }

        tournament.Divisions.Add(division);
    }

    private static void AddDivision(
        Tournament tournament,
        string name,
        IReadOnlyList<Team> groupTeams,
        bool crossCup,
        (int From, int To, string Destination)[] mappings)
    {
        Division division = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLowerInvariant(),
            Tournament = tournament,
            TournamentId = tournament.Id,
            IsCrossDivisionCup = crossCup,
            Stages = [],
            CreatedBy = "test",
            PlayoffMappings = [.. mappings.Select(m => new DivisionPlayoffMapping
            {
                FromPosition = m.From,
                ToPosition = m.To,
                Destination = m.Destination,
                CreatedBy = "test",
            })],
        };

        Stage group = new()
        {
            Id = Guid.NewGuid(),
            Name = $"Group {name}",
            Slug = $"group-{name.ToLowerInvariant()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = default,
            EndDate = default,
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            StageTeamMatches = [],
            CreatedBy = "test",
        };

        foreach (Team team in groupTeams)
        {
            group.StageTeamMatches.Add(new StageTeamMatch
            {
                StageId = group.Id,
                TeamId = team.Id,
                Team = team,
                CreatedBy = "test",
            });
        }

        division.Stages.Add(group);
        tournament.Divisions.Add(division);
    }

    private static TeamTournamentRegistration Reg(Tournament tournament, Team team)
    {
        return new TeamTournamentRegistration
        {
            TeamId = team.Id,
            Team = team,
            TournamentId = tournament.Id,
            CreatedBy = "test",
        };
    }
}
