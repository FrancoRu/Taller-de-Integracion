# Venue Schedule Conflict — Delta Spec

## Purpose

Defines the same-venue double-booking rule (fixed 2-hour window, scoped only by `VenueId`, no
division/tournament filter) and its uniform enforcement across every match write path that can
set a venue + date: create, update/reschedule, and suspend. New capability; formalizes the
already-shipped `UpdateMatchDate` behavior and extends the same rule to `CreateMatch` and
`SuspendMatch`.

## ADDED Requirements

### Requirement: Creating a Match Rejects a Colliding Venue+Time

The system MUST reject creating a match whose `VenueId` and `MatchDate` fall strictly within a
2-hour window of another match already scheduled at the same venue, responding `400 BadRequest`
with `ErrorMessages.Match.VenueScheduleConflict`. A match with no `VenueId` MUST NOT be rejected
for a schedule conflict, regardless of `MatchDate`.

#### Scenario: Create collides with an existing match at the same venue

- GIVEN a match already exists at Venue A scheduled for 2026-09-06 15:00
- WHEN a new match is created at Venue A scheduled for 2026-09-06 16:00
- THEN the create is rejected with 400 and the venue schedule conflict message

#### Scenario: Create with no venue never conflicts

- GIVEN a match already exists at Venue A scheduled for 2026-09-06 15:00
- WHEN a new match is created with `VenueId` null for the same date/time
- THEN the create succeeds

#### Scenario: Create at exactly the 2-hour boundary succeeds

- GIVEN a match already exists at Venue A scheduled for 2026-09-06 15:00
- WHEN a new match is created at Venue A scheduled for 2026-09-06 17:00 (exactly 2 hours later)
- THEN the create succeeds, since the window bounds are exclusive

#### Scenario: Create at the same time at a different venue succeeds

- GIVEN a match already exists at Venue A scheduled for 2026-09-06 15:00
- WHEN a new match is created at Venue B scheduled for 2026-09-06 15:00
- THEN the create succeeds

#### Scenario: Create collides across a different division and tournament sharing the venue

- GIVEN a match exists at Venue A scheduled for 2026-09-06 15:00, belonging to Division X of
  Tournament 1
- WHEN a new match is created at Venue A scheduled for 2026-09-06 16:00, belonging to Division Y
  of Tournament 2
- THEN the create is rejected with 400 and the venue schedule conflict message

### Requirement: Suspending/Rescheduling a Match Rejects a Colliding New Date at Its Own Venue

The system MUST reject rescheduling a suspended match to a new `MatchDate` that falls strictly
within 2 hours of another match at the same venue as the match being rescheduled, responding
`400 BadRequest` with `ErrorMessages.Match.VenueScheduleConflict`, checked before the new date is
persisted. A match with no `VenueId` MUST NOT be rejected for a schedule conflict.

#### Scenario: Suspend to a colliding date at the match's own venue

- GIVEN Match M is scheduled at Venue A for 2026-09-06 10:00, and Match N is scheduled at
  Venue A for 2026-09-06 15:00
- WHEN Match N is suspended/rescheduled to 2026-09-06 11:00
- THEN the reschedule is rejected with 400 and the venue schedule conflict message
- AND Match N's `MatchDate` remains unchanged

#### Scenario: Suspend to a non-colliding date succeeds

- GIVEN Match M is scheduled at Venue A for 2026-09-06 10:00, and Match N is scheduled at
  Venue A for 2026-09-06 15:00
- WHEN Match N is suspended/rescheduled to 2026-09-07 15:00
- THEN the reschedule succeeds and Match N's `MatchDate` is updated

#### Scenario: Suspending a match with no venue never conflicts

- GIVEN Match N has no `VenueId`
- WHEN Match N is suspended/rescheduled to any date
- THEN the reschedule succeeds

### Requirement: Enforcement Is Uniform Across Every Venue+Date Write Path

The venue schedule conflict rule, its 2-hour window, its `400 BadRequest` status, and its
`ErrorMessages.Match.VenueScheduleConflict` message MUST be identical across `CreateMatch`,
`UpdateMatchDate`, and `SuspendMatch`. The rule MUST scope only by `VenueId`, independent of
division or tournament, so it also blocks cross-division and cross-tournament collisions at the
same physical venue.

#### Scenario: Same message and status on all three paths

- GIVEN a colliding venue+time submitted separately on create, on update, and on suspend
- WHEN each request is submitted
- THEN each is rejected with the same `400` status and the same
  `ErrorMessages.Match.VenueScheduleConflict` message

## Non-Goals

- Bulk wizard fixture generation — it never assigns `VenueId` and is unaffected by this rule.
- Configurable/variable match duration — the 2-hour window remains a fixed constant.
- A field/court sub-resource under `Venue` to model multi-court venues.
