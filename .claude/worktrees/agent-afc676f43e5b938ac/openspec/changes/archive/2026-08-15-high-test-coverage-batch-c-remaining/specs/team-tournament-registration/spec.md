# Team Tournament Registration Specification

## Purpose

Characterizes `TeamService.RegisterTeamsToTournamentAsync`, which reconciles a tournament's team roster against a submitted list of team IDs (add/unassign/keep/reassign). Test-only characterization — no production code changes.

## Requirements

### Requirement: New Team Assignment

A team whose ID appears in `teamIds` but is not currently assigned to any tournament MUST be assigned to the target tournament (`TournamentId` set to the tournament's ID).

#### Scenario: Unassigned team is registered

- GIVEN a team with `TournamentId == null`
- WHEN `RegisterTeamsToTournamentAsync(tournament, teamIds)` is called with that team's ID in `teamIds`
- THEN the team's `TournamentId` becomes the target tournament's ID

### Requirement: Dropped Team Unassignment

A team currently assigned to the target tournament whose ID is absent from `teamIds` MUST be unassigned (`TournamentId` set to `null`).

#### Scenario: Team removed from the roster is unassigned

- GIVEN a team with `TournamentId` equal to the target tournament's ID
- WHEN `RegisterTeamsToTournamentAsync(tournament, teamIds)` is called without that team's ID in `teamIds`
- THEN the team's `TournamentId` becomes `null`

#### Scenario: Empty team list unassigns every current member

- GIVEN two or more teams currently assigned to the target tournament
- WHEN `RegisterTeamsToTournamentAsync(tournament, [])` is called
- THEN every previously assigned team's `TournamentId` becomes `null`

### Requirement: Already-Registered Team Unchanged

A team already assigned to the target tournament whose ID is also present in `teamIds` MUST remain assigned to that same tournament, unmodified.

#### Scenario: Existing member stays registered

- GIVEN a team with `TournamentId` equal to the target tournament's ID
- WHEN `RegisterTeamsToTournamentAsync(tournament, teamIds)` is called with that team's ID included
- THEN the team's `TournamentId` remains the target tournament's ID

### Requirement: Cross-Tournament Reassignment

A team currently assigned to a different tournament whose ID is present in `teamIds` MUST be reassigned to the target tournament.

#### Scenario: Team moves from one tournament to another

- GIVEN a team with `TournamentId` equal to a different tournament's ID
- WHEN `RegisterTeamsToTournamentAsync(tournament, teamIds)` is called with that team's ID included
- THEN the team's `TournamentId` becomes the target tournament's ID

### Requirement: Unrelated Teams Untouched

A team neither currently assigned to the target tournament nor present in `teamIds` MUST NOT be modified.

#### Scenario: Uninvolved team is left alone

- GIVEN a team with `TournamentId` set to a different tournament and NOT present in `teamIds`
- WHEN `RegisterTeamsToTournamentAsync(tournament, teamIds)` is called
- THEN the team's `TournamentId` remains unchanged
