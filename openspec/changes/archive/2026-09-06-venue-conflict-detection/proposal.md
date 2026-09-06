# Proposal: Venue Conflict Detection at All Match Write Paths

## Intent

Venue double-booking protection already ships as `MatchService.HasVenueScheduleConflictAsync` (fixed 2-hour window, VenueId-scoped, no division/tournament filter — so it already catches cross-division and cross-tournament collisions) but is wired into only one of three endpoints that set a match's venue+time: `UpdateMatchDate`. A match can still be double-booked through `CreateMatch` and `SuspendMatch`, which apply no guard. This closes that gap so the same rule fires everywhere a venue+date is assigned, with the same Spanish 400 message admins already see on the edit form. Exploration open questions 1–3 are resolved by the user (400 status everywhere; null venue = no-op; add the missing tests here).

## Scope

### In Scope
- Apply the existing guard to `CreateMatchAsync` (via `CreateMatch`) using the request's `VenueId`.
- Apply the guard to `SuspendMatchAsync`, restructured to check-before-persist, using the match's own existing `VenueId` against the (possibly new) date; reject with `400` + `ErrorMessages.Match.VenueScheduleConflict`, consistent with `UpdateMatchDate`.
- New backend tests for `HasVenueScheduleConflictAsync` itself (exact 2-hour boundary, cross-division collision, null-venue no-op) plus the two newly-guarded paths.
- Frontend: surface the existing Spanish conflict message on create-match and suspend/reschedule forms, mirroring `matchPage.tsx`'s existing edit-venue error pattern.

### Out of Scope
- Bulk wizard fixture generation (never assigns `VenueId` — confirmed).
- Configurable match duration / window (no duration concept exists to hang it on).
- Field/court sub-resource under `Venue` (no evidenced pain; not this gap).
- Changing the shipped rule, its window, or the `UpdateMatchDate` path.

## Capabilities

### New Capabilities
- `venue-schedule-conflict`: the 2-hour same-venue double-booking rule and its uniform enforcement across create, reschedule (update), and suspend, including null-venue no-op and cross-division reach.

### Modified Capabilities
- None (no existing spec covers venue scheduling today).

## Approach

Exploration Approach 1: reuse `HasVenueScheduleConflictAsync` + `ErrorMessages.Match.VenueScheduleConflict` verbatim at the two missing call sites, mirroring the `UpdateMatchDate` controller guard (MatchController.cs:169-175). No new entities, DTOs, migration, or UI components. Only `SuspendMatch` needs a small check-before-persist restructure since it currently persists then returns with no re-check point; the exact conflict-signaling mechanism (controller pre-check vs. service exception) is a design detail.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Application/Services/MatchService.cs` | Modified | Guard in `CreateMatchAsync`; check-before-persist in `SuspendMatchAsync` |
| `API/Controllers/MatchController.cs` | Modified | Mirror `UpdateMatchDate` guard in `CreateMatch`, `SuspendMatch` |
| `Application/Utils/Constants/ErrorMessages.cs` | Reused | `VenueScheduleConflict` verbatim |
| `API.Tests/` | New | Conflict-logic + two-path coverage |
| `WebClient/src/views/match/matchPage.tsx` (+ create/suspend UIs) | Modified | Surface conflict message per existing pattern |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| `SuspendMatchAsync` restructure touches untested behavior | Med | Add tests first (strict TDD) before restructuring |
| Backend guard lands before frontend surfacing → opaque error | Low | Ship frontend error UI in same change |

## Rollback Plan

Revert the commit(s). The guard additions are isolated to two call sites and their tests; no schema or data changes, so rollback restores the prior (unguarded) behavior with no migration.

## Dependencies

- None. Touches backend and frontend; no external prerequisites.

## Success Criteria

- [ ] Creating or suspending/rescheduling a match into an occupied 2-hour venue slot returns `400` + the Spanish conflict message.
- [ ] Null-venue create/suspend proceeds unaffected.
- [ ] New tests cover the 2-hour boundary, cross-division collision, and both newly-guarded paths; full suite green.
- [ ] Create and suspend/reschedule forms display the same conflict message as the edit form.
