# Division Team Roster — Delta Spec

## Purpose

Define `DivisionTeamRegistration` as the new, authoritative, stage-independent record of which
teams are enrolled in which division — coexisting additively with `StageTeamMatch`'s narrower
"placed into this specific stage/slot" meaning — including the one-regular-zone-plus-optional-
cross-division-cup enrollment invariant, removal/unenrollment behavior, and the backfill
correctness rule for historical data. New capability; no prior spec.

## ADDED Requirements

### Requirement: Division Roster Is the Source of Truth for Division Enrollment

The system MUST persist a team's enrollment in a division as a `DivisionTeamRegistration` row
(`TeamId`, `DivisionId`), independent of any `Stage`/`StageTeamMatch` row. `DivisionTeamRegistration`,
not `StageTeamMatch`, MUST be the authoritative "which teams are enrolled in this division" fact
going forward.

#### Scenario: Enrollment exists without any stage placement

- GIVEN a team enrolled in a division via `DivisionTeamRegistration`
- WHEN no `StageTeamMatch` row exists yet for that team in that division
- THEN the team is still considered enrolled in the division

### Requirement: One Registration Per (TeamId, DivisionId) Pair

The system MUST enforce a unique constraint on `(TeamId, DivisionId)`. A team MUST NOT hold more
than one `DivisionTeamRegistration` row for the same division.

#### Scenario: Duplicate registration for the same division rejected

- GIVEN a team already registered to a division
- WHEN a second registration for the same team and the same division is attempted
- THEN the attempt is rejected
- AND only one registration row exists for that `(TeamId, DivisionId)` pair

### Requirement: One Regular-Zone Registration Plus an Optional Cross-Division-Cup Registration

Within a single tournament, a team MUST hold at most one `DivisionTeamRegistration` in a regular,
non-cross-division-cup division. A team MAY additionally hold exactly one `DivisionTeamRegistration`
in a division flagged `IsCrossDivisionCup` within the same tournament. A team MUST NOT hold
registrations in two regular divisions of the same tournament at once.

#### Scenario: Team registers to its home zone and a cross-division cup

- GIVEN a team registered to its regular, non-cross-cup division in a tournament
- WHEN it is also registered to a division flagged `IsCrossDivisionCup` in the same tournament
- THEN both registrations exist
- AND neither registration is rejected

#### Scenario: Second regular-division registration is rejected

- GIVEN a team already registered to a regular division of a tournament
- WHEN registration to a different regular, non-cross-cup division of the same tournament is
  attempted
- THEN the attempt is rejected

#### Scenario: Second cross-division-cup registration is rejected

- GIVEN a team already registered to a division flagged `IsCrossDivisionCup` in a tournament
- WHEN registration to a second `IsCrossDivisionCup` division of the same tournament is attempted
- THEN the attempt is rejected

### Requirement: Stage Placement Requires an Existing Roster Registration

Assigning a team to a stage (creating a `StageTeamMatch`) within a division MUST be rejected unless
a `DivisionTeamRegistration` already exists for that exact `(TeamId, DivisionId)` pair. Placement is
a subset relationship of enrollment, never the reverse.

#### Scenario: Placement without prior registration is rejected

- GIVEN a team with no `DivisionTeamRegistration` for a division
- WHEN an attempt is made to assign it directly to a stage of that division
- THEN the assignment is rejected
- AND no `StageTeamMatch` row is created

### Requirement: Removing a Team From the Roster Cascades to Its Stage Placements

Removing (unenrolling) a team's `DivisionTeamRegistration` MUST also remove any `StageTeamMatch`
row that still exists for that team within the division's stages, in the same operation. The
frontend MUST show a confirmation dialog before committing the removal whenever the team still
holds a stage placement, stating plainly that the team will be removed from its current group or
bracket slot as well — mirroring the existing "Eliminar equipo" and tournament-cancel cascade
confirmation pattern elsewhere in this app. Unenrolling a team that holds no stage placement
removes only the registration, no confirmation dialog required.

#### Scenario: Unenrolling a placed team removes both the placement and the registration

- GIVEN a team registered to a division and placed into one of its stages
- WHEN an admin confirms removal of that team's `DivisionTeamRegistration`
- THEN the team's `StageTeamMatch` row(s) for that division are deleted
- AND the `DivisionTeamRegistration` row is deleted
- AND the team is no longer considered enrolled in the division

#### Scenario: Unenrolling an unplaced team removes only the registration

- GIVEN a team registered to a division with no `StageTeamMatch` row in that division
- WHEN an admin removes that team's `DivisionTeamRegistration`
- THEN the registration row is deleted
- AND the team is no longer considered enrolled in the division

### Requirement: Cascade Delete on Team or Division Removal

`DivisionTeamRegistration` rows MUST be deleted automatically when their referenced `Team` or
`Division` is deleted (`OnDelete(DeleteBehavior.Cascade)` on both foreign keys).

#### Scenario: Deleting a division removes its registrations

- GIVEN a division with 2 enrolled teams
- WHEN the division is deleted
- THEN both `DivisionTeamRegistration` rows for that division are also removed

#### Scenario: Deleting a team removes its registrations

- GIVEN a team registered to 2 divisions
- WHEN the team is deleted
- THEN both of its `DivisionTeamRegistration` rows are also removed

### Requirement: Backfill Produces Exactly One Registration Per Distinct (TeamId, DivisionId) Pair

The migration that introduces `DivisionTeamRegistration` MUST backfill one row per distinct
`(TeamId, DivisionId)` pair derived from existing `StageTeamMatch → Stage.DivisionId` data,
deduplicating on the pair — never on `TeamId` alone.

#### Scenario: Team in two sub-groups of one division collapses to one registration

- GIVEN a team with `StageTeamMatch` rows in two different stages that both belong to the same
  division
- WHEN the backfill runs
- THEN exactly one `DivisionTeamRegistration` row exists for that team and division afterward

#### Scenario: Team in a group stage and a same-division bracket stage collapses to one registration

- GIVEN a team with a `StageTeamMatch` row in a division's Group stage and another `StageTeamMatch`
  row in a bracket stage of that same division
- WHEN the backfill runs
- THEN exactly one `DivisionTeamRegistration` row exists for that team and division afterward

#### Scenario: Cross-division-cup team backfills to two registrations

- GIVEN a team with `StageTeamMatch` rows in its regular division and in a separate
  `IsCrossDivisionCup` division
- WHEN the backfill runs
- THEN two `DivisionTeamRegistration` rows exist for that team, one per division
- AND neither registration is collapsed into the other

### Requirement: Backfill Is Safe to Re-Run

The backfill MUST be idempotent: re-running it against data it has already processed MUST NOT
create duplicate `DivisionTeamRegistration` rows.

#### Scenario: Re-running the backfill creates no duplicates

- GIVEN a database where the backfill has already run once
- WHEN the same backfill logic is executed again
- THEN the count of `DivisionTeamRegistration` rows for every `(TeamId, DivisionId)` pair remains
  exactly 1

## Non-Goals

- A status/lifecycle field on `DivisionTeamRegistration` (e.g. distinguishing "enrolled, not yet
  slotted" from "enrolled and playing"). Boolean presence of the row is sufficient for this change;
  that distinction is already expressed by whether a `StageTeamMatch` exists. Deferred, not
  precluded.
- Replacing or narrowing `StageTeamMatch`'s existing schema. This change is strictly additive.
- Cross-season promotion/relegation. `DivisionTeamRegistration` remains scoped to one tournament's
  divisions, mirroring `TeamTournamentRegistration`.
