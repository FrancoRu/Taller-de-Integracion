# Automated Stage Chain & Team Assignment — Delta Spec

Targets `openspec/specs/stage-generation/spec.md`.

## Purpose

Refine HU-121/122/123 (organizer-chosen, balanced sub-group count; manual reassignment; editing
the count before tournament start without orphaning the roster), make `AssignTeamsToStageAsync`
roster-aware, remove the dead, contradictory `CreateAutomatedStagesAsync` mechanism (HU-124), and
fence off the one combination this change does not support: sub-groups together with
position-range cup qualification (HU-125, explicitly out of scope).

## ADDED Requirements

### Requirement: Organizer-Configurable Sub-Group Count With Balanced Distribution

When building or editing a division's group-phase structure, the organizer MUST be able to specify
a sub-group count `G` (a positive integer) instead of a fixed group size. Given `T` teams enrolled
in the division's `DivisionTeamRegistration` roster, the system MUST distribute teams so each
sub-group receives either `floor(T/G)` or `ceil(T/G)` teams, and the difference between the largest
and smallest sub-group MUST never be 2 or more.

#### Scenario: 16 teams into 3 sub-groups balances 5/5/6

- GIVEN a division roster of 16 teams and an organizer-chosen sub-group count of 3
- WHEN the balanced distribution runs
- THEN the resulting sub-groups have sizes 5, 5, and 6
- AND no two sub-groups differ in size by 2 or more

#### Scenario: Evenly divisible roster balances exactly

- GIVEN a division roster of 16 teams and an organizer-chosen sub-group count of 4
- WHEN the balanced distribution runs
- THEN all 4 sub-groups have exactly 4 teams

### Requirement: Minimum Viable Sub-Group Size

The system MUST reject a sub-group count `G` where `T / G < 4` for the division's currently
relevant team count `T` (estimated in the wizard, actual once enrolled), with a clear error message
identifying that the count is too high for the team count.

#### Scenario: Too many sub-groups for the roster size is rejected

- GIVEN a division roster of 10 teams
- WHEN an organizer requests 3 sub-groups
- THEN the system rejects the request with a clear error message
- AND no stage structure is created or changed

#### Scenario: Exactly 4 teams per sub-group is accepted

- GIVEN a division roster of 12 teams
- WHEN an organizer requests 3 sub-groups
- THEN the system accepts the request, producing 3 sub-groups of 4 teams each

### Requirement: Two-Stage Validation — Non-Blocking Wizard Warning, Blocking Completability Guard

Sub-group balance and minimum-size validation MUST run at two points: as a non-blocking warning
during tournament-wizard configuration, before real enrollment numbers exist; and as a blocking
check inside `TournamentCompletabilityValidator` when registration closes or the tournament starts,
evaluated against the division's actual enrolled roster count.

#### Scenario: Wizard warns but does not block

- GIVEN the wizard's estimated team count would produce sub-groups smaller than 4
- WHEN the organizer reaches the review step
- THEN the wizard shows a non-blocking warning
- AND the organizer can still proceed past the wizard with that configuration

#### Scenario: Completability guard blocks at registration close

- GIVEN a division's actual enrolled roster would produce at least one sub-group smaller than 4
  teams for the configured sub-group count
- WHEN the organizer attempts to close registration or start the tournament
- THEN `TournamentCompletabilityValidator` blocks the transition with a clear error
- AND the tournament does not transition status

### Requirement: Manual Team-to-Subgroup Reassignment Always Available

Regardless of how a team was placed into a sub-group, the system MUST allow an admin/owner to
manually move an enrolled team from one sub-group to another within the same division, without
requiring a full re-distribution of every other team.

#### Scenario: Admin moves one team between sub-groups

- GIVEN a team placed in sub-group A of a division
- WHEN an admin manually reassigns it to sub-group B of the same division
- THEN the team's `StageTeamMatch` reflects sub-group B and no longer sub-group A
- AND every other team's sub-group placement is unchanged

### Requirement: One-Click Auto-Distribute Runs Balanced Random Distribution Over the Roster

The system MUST provide a one-click auto-distribute action that runs a random-balanced
distribution, per the balance rule above, over the `DivisionTeamRegistration` roster — not over
existing stage rows — and MUST leave manual per-sub-group adjustment available afterward as a
separate, later step.

#### Scenario: Auto-distribute reads the roster, not prior placements

- GIVEN a division roster of enrolled teams where some teams are not yet placed in any sub-group
- WHEN the auto-distribute action runs
- THEN every roster-enrolled team is placed into exactly one sub-group
- AND the distribution obeys the floor/ceil balance rule

### Requirement: Enrollment and Sub-Group Placement Are Independent Steps

A team MAY hold a `DivisionTeamRegistration` for a division without yet being placed into any
sub-group `StageTeamMatch`. This is a valid, non-error state.

#### Scenario: Enrolled but unplaced team is valid

- GIVEN a team enrolled in a division's roster
- WHEN no auto-distribute or manual placement has run yet for that team
- THEN the team's `DivisionTeamRegistration` exists
- AND no `StageTeamMatch` exists for it in that division
- AND this state is not reported as an error

### Requirement: Editing Sub-Group Count Before Tournament Start Rebuilds Only the Stage Layer

Changing a division's sub-group count after stages already exist, but before the tournament
starts, MUST leave every `DivisionTeamRegistration` for that division untouched, MUST delete and
recreate only the division's `Stage` and `StageTeamMatch` rows, and MUST re-run the
balanced-distribution rule over the unchanged roster afterward. No enrolled team may end the
operation without a `DivisionTeamRegistration`.

#### Scenario: Roster survives a group-count change

- GIVEN a division with 3 sub-groups and 16 enrolled teams already placed
- WHEN the organizer changes the sub-group count to 4
- THEN all 16 `DivisionTeamRegistration` rows for that division still exist afterward
- AND the teams are re-distributed into 4 balanced sub-groups
- AND no team is left without a `DivisionTeamRegistration`

#### Scenario: Old stage structure is fully replaced, not merged

- GIVEN a division with 3 sub-group `Stage` rows
- WHEN the organizer changes the sub-group count to 2
- THEN the original 3 `Stage` rows (and their `StageTeamMatch` rows) no longer exist
- AND exactly 2 new `Stage` rows exist, populated by the re-run distribution

### Requirement: Sub-Group Count Edit Bounded by the Existing Structural Lock

Editing sub-group count MUST be rejected once the tournament's status is `Ongoing`, `Finished`, or
`Canceled`, using the same `EnsureDivisionStructureEditableAsync` guard already applied to stage
creation, editing, and team assignment.

#### Scenario: Edit rejected after tournament starts

- GIVEN a tournament with status `Ongoing`
- WHEN an admin attempts to change one of its divisions' sub-group count
- THEN the request is rejected with the existing structure-locked error
- AND no stage or roster row is changed

### Requirement: Sub-Groups Combined With Position-Range Cups Are Rejected, Not Silently Miscalculated

A division configured with a sub-group count of 2 or more MUST NOT also carry a cup stage that
qualifies teams via `cupPositionRange`, because `cupPositionRange` reads a single combined
standings table and has no defined meaning across multiple independent sub-group tables.
Per-sub-group cup qualification is a separate, later change and is explicitly out of scope here.
The system MUST reject the combination outright in either configuration order — rather than let a
cup silently compute qualifiers from an incorrect combined table — because a playoff cup routinely
decides which teams reach paid or prized rounds, and a silently wrong standings computation there
is worse than blocking the organizer until the combination is properly supported.

#### Scenario: Enabling sub-groups is rejected when a position-range cup already exists

- GIVEN a division already has a cup stage configured with `cupPositionRange`
- WHEN an organizer attempts to set that division's sub-group count to 2 or more
- THEN the request is rejected with an error explaining that sub-groups are incompatible with
  position-range cups in this version
- AND the sub-group count is not changed

#### Scenario: Configuring a position-range cup is rejected when sub-groups already exist

- GIVEN a division already has a sub-group count of 2 or more
- WHEN an organizer attempts to configure a cup stage with `cupPositionRange` on that division
- THEN the request is rejected with the same error
- AND no cup stage is created

#### Scenario: Single sub-group is unaffected

- GIVEN a division with a sub-group count of 1, the current single-table behavior
- WHEN a cup stage with `cupPositionRange` is configured
- THEN the configuration succeeds exactly as it does today

## MODIFIED Requirements

### Requirement: Team Assignment to Stage

`AssignTeamsToStageAsync` MUST assign teams up to the stage's slot capacity (per `StageType`), MUST
reject manual assignment that would exceed available slots, MUST reject assignment to an
already-full stage, MUST cap automatic assignment at the number of available slots, and — new in
this change — MUST reject assigning any team that does not already hold a
`DivisionTeamRegistration` for the stage's division.
(Previously: assignment had no roster precondition; any tournament-registered team could be
assigned directly to a stage regardless of division enrollment.)

#### Scenario: Exact slot match still assigns all registered teams

- GIVEN a stage with slot capacity 4, 0 existing assignments, and 4 teams each holding a
  `DivisionTeamRegistration` for the stage's division
- WHEN the 4 team IDs are assigned manually
- THEN all 4 `StageTeamMatch` records are created

#### Scenario: Too many teams for available slots

- GIVEN a stage with 1 available slot
- WHEN 3 team IDs, all holding a `DivisionTeamRegistration` for the division, are assigned manually
- THEN it throws an exception and creates no records

#### Scenario: Duplicate team IDs are filtered

- GIVEN a manual request containing duplicate team IDs or IDs already assigned to the stage
- WHEN `AssignTeamsToStageAsync` is called
- THEN duplicates and already-assigned IDs are silently excluded from the created records

#### Scenario: Assignment rejected for a team with no roster registration

- GIVEN a stage with at least 1 available slot
- WHEN a team ID with no `DivisionTeamRegistration` for the stage's division is assigned, manually
  or automatically
- THEN the assignment is rejected for that team
- AND no `StageTeamMatch` record is created for it

#### Scenario: Auto mode only draws from registered, unassigned teams

- GIVEN a stage with N available slots and a division roster containing teams both already assigned
  and not yet assigned to any stage of that division
- WHEN `AssignTeamsToStageAsync` is called with `auto = true`
- THEN at most N teams are auto-assigned
- AND every auto-assigned team holds a `DivisionTeamRegistration` for the division and was not
  already linked to the stage

## REMOVED Requirements

### Requirement: Automated Stage Chain Generation

**Reason**: Dead code with zero UI callers, incompatible with HU-121's organizer-chosen sub-group
count (it hardcodes fixed 4-team groups and requires exactly 8/16/32/64 registered teams). Leaving
it alive under a name that "sounds like" HU-121's mechanism is a foot-gun for a future developer
wiring the wrong endpoint.

**Migration**: None required. No production data or existing caller depends on this endpoint;
deletion is a pure removal with no backward-compatibility shim.

#### Scenario: Endpoint no longer routes

- GIVEN the change is applied
- WHEN a client sends `POST /api/stages/generate/{id}`
- THEN the server responds `404 Not Found` because no controller action maps to that route

#### Scenario: Service method removed

- GIVEN the change is applied
- WHEN the backend solution is inspected
- THEN `StageService` no longer declares `CreateAutomatedStagesAsync`
- AND the solution builds successfully with no remaining caller of that method

#### Scenario: Frontend caller removed

- GIVEN the change is applied
- WHEN `Club12-WebClient/src/modules/stage/service/stage.service.ts` and `stage.context.tsx` are
  inspected
- THEN neither file exports nor calls a `generateStages` function
