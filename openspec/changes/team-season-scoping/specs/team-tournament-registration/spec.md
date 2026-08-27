# Delta for Team Tournament Registration

## ADDED Requirements

### Requirement: Season-Scoped Registration Join

The system MUST persist team↔tournament participation as rows in a `TeamTournamentRegistration` join table (TeamId, TournamentId) with a unique (TeamId, TournamentId) index. A team MAY hold registrations in multiple tournaments at once; each MUST be preserved independently. Re-registering an existing pair MUST NOT duplicate the row.

#### Scenario: Team registered to two tournaments keeps both

- GIVEN a team registered to Tournament A
- WHEN it is also registered to Tournament B
- THEN it has an active registration row for both A and B

### Requirement: Listing and Pointer Resolve Via the Join

`GetAllTeamsAsync` filtered by `TournamentId` MUST resolve teams via the `TeamTournamentRegistration` join, not `Team.TournamentId` FK equality. `Team.TournamentId` MUST remain only a denormalized "current-season" pointer, never the source of truth for historical or multi-tournament participation.

#### Scenario: Multi-registered team appears in every tournament's listing

- GIVEN a team registered to Tournament A and Tournament B, with `Team.TournamentId` pointing to A
- WHEN teams are listed filtered by Tournament B
- THEN the team appears, regardless of the `Team.TournamentId` value

### Requirement: Migration Preserves Historical Participation

The migration MUST backfill `TeamTournamentRegistration` idempotently from: (1) team/tournament pairs recoverable via `StageTeamMatch → Stage → Division`, and (2) each team's current `Team.TournamentId` where not null. Re-running MUST NOT create duplicates.

#### Scenario: Historical participation is recovered idempotently

- GIVEN a team with `StageTeamMatch` rows under Tournament A, and `Team.TournamentId` pointing elsewhere
- WHEN the backfill runs, then runs again
- THEN one registration row for (team, Tournament A) exists after both runs

## MODIFIED Requirements

### Requirement: New Team Assignment

A team in `teamIds` with no existing registration for the target tournament MUST receive a new `TeamTournamentRegistration` row for it. `Team.TournamentId` MUST also update to the target tournament's ID.
(Previously: set the single reassignable `Team.TournamentId` FK; no join row existed.)

#### Scenario: Unregistered team is registered

- GIVEN a team with no registration for the target tournament
- WHEN `RegisterTeamsToTournamentAsync(tournament, teamIds)` includes that team's ID
- THEN a registration row for (team, tournament) exists
- AND `Team.TournamentId` becomes the target tournament's ID

### Requirement: Dropped Team Unassignment

A team registered to the target tournament but absent from `teamIds` MUST have only its registration for that tournament removed. This MUST NOT affect, and MUST NOT erase, its registrations or history in other tournaments.
(Previously: absence nulled the single `Team.TournamentId` FK, unconditionally clearing the team's only link.)

#### Scenario: Dropped team keeps its other tournament's registration

- GIVEN a team registered to the target tournament and to a different tournament
- WHEN called without that team's ID in `teamIds`
- THEN its registration for the target tournament is removed
- AND its registration for the other tournament remains intact

#### Scenario: Empty team list unassigns only the target tournament's members

- GIVEN two or more teams registered to the target tournament, one also registered elsewhere
- WHEN `RegisterTeamsToTournamentAsync(tournament, [])` is called
- THEN every team's registration for the target tournament is removed
- AND their registrations for other tournaments are NOT removed

### Requirement: Already-Registered Team Unchanged

A team already registered to the target tournament and also present in `teamIds` MUST remain registered, and its row MUST NOT be duplicated or modified.
(Previously: described via unmodified `Team.TournamentId` FK equality only.)

#### Scenario: Existing member stays registered

- GIVEN a team registered to the target tournament
- WHEN `RegisterTeamsToTournamentAsync(tournament, teamIds)` includes that team's ID
- THEN its registration for the target tournament remains unchanged, not duplicated

### Requirement: Multi-Tournament Registration

A team registered to a different tournament, whose ID is present in `teamIds`, MUST gain a registration to the target tournament in addition to, not instead of, its existing registration(s).
(Previously: "Cross-Tournament Reassignment" — the single `Team.TournamentId` FK moved to the target tournament, destroying the prior link.)

#### Scenario: Team gains a second registration without losing the first

- GIVEN a team registered to a different tournament
- WHEN `RegisterTeamsToTournamentAsync(tournament, teamIds)` includes that team's ID
- THEN it has an active registration for the target tournament
- AND its registration for the other tournament remains intact
