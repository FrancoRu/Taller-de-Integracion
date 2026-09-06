# Playoff Draw & Seeding — Delta Spec

## Purpose

Define seeding for playoffs-only divisions (no group phase): roster-based enrollment, random draw
with a server-side preview-then-commit flow, manual seeding, bye handling reused unchanged from
`PlayoffSeeder`, a bracket-scoped re-draw lock, audit logging via `AuditAction.PlayoffDraw`, and the
public "Sorteo realizado el [fecha]" transparency element sourced from `Stage.DrawnAt`. New
capability; no prior spec.

## ADDED Requirements

### Requirement: Playoffs-Only Division Enrollment Via the Roster

A division configured with no `Group` stage MUST accept team enrollment through
`DivisionTeamRegistration` alone, with no requirement that any group-phase stage exist.

#### Scenario: Team enrolls into a playoffs-only division

- GIVEN a division configured with no Group stage
- WHEN an admin enrolls a team into that division
- THEN a `DivisionTeamRegistration` is created for that team and division
- AND the enrollment does not require any `Stage` to exist yet

#### Scenario: Assignment view offers enrolled teams with no group stage present

- GIVEN a playoffs-only division with enrolled teams
- WHEN the division's team-assignment view is opened
- THEN the roster's enrolled teams are shown as assignable to the bracket

### Requirement: Random Draw Produces an Ordered Team List Fed to the Existing Bracket Seeder

A random draw ("sorteo aleatorio") MUST produce a shuffled ordering of the division's enrolled
roster and hand that ordered list to the existing `PlayoffSeeder.SeedPairs` and
`FillStageWithSeedsAsync` machinery unchanged.

#### Scenario: Random draw builds a valid bracket

- GIVEN a playoffs-only division with 6 enrolled teams
- WHEN a random draw is committed
- THEN `SeedPairs` receives a 6-element ordered list drawn from the roster
- AND every enrolled team appears exactly once in the resulting bracket

### Requirement: Manual Seeding Produces an Admin-Specified Ordered List

Manual seeding MUST let an admin specify the exact order/slot per enrolled team, producing the same
ordered-list shape consumed by `PlayoffSeeder.SeedPairs`, without a random shuffle.

#### Scenario: Admin manually orders the bracket

- GIVEN a playoffs-only division with enrolled teams
- WHEN an admin submits an explicit team order for seeding
- THEN the bracket is built from that exact order
- AND no random shuffling is applied

### Requirement: Bye Handling Is Reused Unchanged From PlayoffSeeder

Non-power-of-2 enrolled team counts MUST be handled by `PlayoffSeeder.SeedPairs`'s existing
padding-to-next-power-of-two-with-null behavior, and a bye's implicit winner MUST be advanced by the
existing `TryAdvanceStageWinnerAsync` mechanism. No new bye-handling logic is introduced by this
change.

#### Scenario: Non-power-of-2 count produces byes

- GIVEN a playoffs-only division with 6 enrolled teams
- WHEN the bracket is seeded
- THEN 2 of the top seeds receive byes into the next round
- AND each bye's team is automatically advanced without requiring a match to be played

### Requirement: Server-Side Draw Preview Guarantees Preview Equals Commit

A stateless preview endpoint MUST compute and return a candidate pairing together with a draw
token, without persisting any change. Committing the draw MUST accept that same token, and the
committed bracket MUST be identical to the previewed pairing.

#### Scenario: Previewed bracket matches committed bracket

- GIVEN a preview-draw request returns a pairing and a draw token
- WHEN that same token is submitted to the commit endpoint
- THEN the committed bracket's team-to-slot assignments are identical to the previewed pairing

#### Scenario: Preview does not persist state

- GIVEN a preview-draw request has been made and not committed
- WHEN the division's stages are inspected afterward
- THEN no `StageTeamMatch` row and no `Stage.DrawnAt` value have changed

#### Scenario: Committing without a valid preceding preview token is rejected

- GIVEN no preview has been made, or the supplied token does not correspond to a live preview
- WHEN the commit endpoint is called with that token
- THEN the commit is rejected rather than silently producing an un-previewed bracket

### Requirement: Bracket-Scoped Re-Draw Lock

A (re-)draw of a bracket MUST be permitted only while the target `Stage` and `BracketName`
combination has zero played matches. A match counts as played if it `IsFinished`, or has a recorded
score, or has a recorded actual start/played date. This lock is evaluated per bracket
(`Stage` + `BracketName`) and independently of tournament status.

#### Scenario: Re-draw allowed before any match is played

- GIVEN a bracket stage with all matches unplayed
- WHEN an admin requests a re-draw
- THEN the re-draw is permitted
- AND it replaces the existing seeding

#### Scenario: Re-draw blocked after the first match is played

- GIVEN a bracket stage where at least one match `IsFinished`, or has a recorded score, or has a
  recorded played date
- WHEN an admin requests a re-draw of that same `Stage` + `BracketName`
- THEN the request is rejected
- AND the existing seeding is unchanged

#### Scenario: Parallel brackets lock independently

- GIVEN a division with two parallel brackets under different `BracketName` values, one with a
  played match and one with none
- WHEN a re-draw is requested for the bracket with no played matches
- THEN it is permitted regardless of the other bracket's locked state

#### Scenario: Lock applies regardless of tournament status

- GIVEN a tournament with status `Ongoing` and a bracket stage with no played matches
- WHEN an admin requests a draw for that bracket
- THEN the draw is permitted, because the re-draw lock is bracket-scoped, not tournament-status-scoped

### Requirement: Every Draw Writes a PlayoffDraw Audit Entry

Every draw, initial or re-draw, MUST log through `IAuditService.LogAsync` using a new
`AuditAction.PlayoffDraw` value, with `TargetType` set to `"Stage"`, `TargetId` set to the bracket
stage's ID, and `Detail` set to a human-readable line describing the draw. A logging failure MUST
NOT block or roll back the draw itself.

#### Scenario: Random draw logs an audit entry

- GIVEN a committed random draw of 8 teams
- WHEN the audit log is queried afterward
- THEN one entry exists with `AuditAction.PlayoffDraw`, `TargetType = "Stage"`, `TargetId` matching
  the bracket stage, and a `Detail` describing an 8-team random draw

#### Scenario: Manual seeding also logs an audit entry

- GIVEN a committed manual seeding
- WHEN the audit log is queried afterward
- THEN one entry exists with `AuditAction.PlayoffDraw` and a `Detail` describing the manual order

#### Scenario: Audit logging failure does not block the draw

- GIVEN the audit-logging dependency fails when called
- WHEN a draw is committed
- THEN the draw still completes successfully
- AND the failure is not surfaced to the caller as a draw failure

### Requirement: Public Bracket View Shows the Draw Date From Stage.DrawnAt

Committing a draw MUST set a new nullable `Stage.DrawnAt` field on the bracket stage to the commit
timestamp. The public bracket view MUST display "Sorteo realizado el [fecha]" using
`IStageResponse.DrawnAt` when it is set, without reading the audit trail, which is
Admin/Owner-only.

#### Scenario: Public view shows the draw date after commit

- GIVEN a bracket stage whose draw was committed at a known timestamp
- WHEN an unauthenticated visitor views the public bracket page
- THEN it shows "Sorteo realizado el [fecha]" using that timestamp
- AND no authentication is required to see it

#### Scenario: No draw yet shows no draw-date label

- GIVEN a bracket stage that has not been drawn (`DrawnAt` is null)
- WHEN the public bracket page is viewed
- THEN no "Sorteo realizado" label is shown for that stage

#### Scenario: Re-draw updates the displayed date

- GIVEN a bracket stage previously drawn, then re-drawn before any match was played
- WHEN the public bracket page is viewed after the re-draw
- THEN the displayed date reflects the most recent commit, not the original one

## Non-Goals

- Per-sub-group cup qualification (HU-125) — a division with sub-groups combined with
  position-range cups is explicitly rejected by the `stage-generation` delta, not solved here.
- Client-side-only draw preview. The preview MUST be server-side per the preview-equals-commit
  guarantee above.
- Any change to how already-seeded, group-standings-based brackets (`SeedKnockoutStageAsync`) work;
  this capability only adds the new roster/random/manual seeding path for groupless divisions.
