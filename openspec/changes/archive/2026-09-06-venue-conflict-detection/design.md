# Design: Venue Conflict Detection at All Match Write Paths

## Technical Approach

Reuse `MatchService.HasVenueScheduleConflictAsync` (`MatchService.cs:88`) verbatim and wire it into the two
currently-unguarded write paths — `CreateMatch` and `SuspendMatch` — using the **exact controller-level
pre-check** already shipped in `MatchController.UpdateMatchDate` (`MatchController.cs:169-175`): guard gated on
`VenueId.HasValue`, on conflict `return BadRequest(ErrorMessages.Match.VenueScheduleConflict)`. No new
conflict logic, no config, no entities, no migration. Implements the `venue-schedule-conflict` capability
(create-reject, null no-op, non-colliding-succeeds, suspend/reschedule-reject, cross-division/tournament reach,
exclusive 2h boundary).

## Architecture Decisions

| # | Decision | Choice | Rejected alternative | Rationale |
|---|----------|--------|----------------------|-----------|
| D1 | Conflict signaling | Controller-level pre-check returning `BadRequest(...)` at both sites | Service-thrown `InvalidOperationException` (the `EnsureXxxAsync` house style in `StageService`) | **Verified**: `GlobalExceptionHandler.MapException` maps `InvalidOperationException` → **409 Conflict + generic title** (`GlobalExceptionHandler.cs:80`). The fixed requirement is **400 + the exact `VenueScheduleConflict` message**, so the guard-throw pattern produces the wrong status/copy. Mirroring `UpdateMatchDate` is the only path that yields 400 + exact message, and keeps a **single** convention across all three write paths. |
| D2 | `SuspendMatch` check-before-persist location | In the **controller**: load match → resolve effective date → pre-check → call the unchanged `SuspendMatchAsync`. `SuspendMatchAsync` body and signature stay as-is. | Restructuring inside `SuspendMatchAsync` to check-then-persist | Check-before-persist is satisfied by controller ordering (guard runs before the service mutates/persists). Avoids a service signature change, avoids the 409 problem from D1, avoids a discriminated-result return type, and preserves the method's existing test (`MatchServiceGenerationTests`). The one-extra load (controller `GetMatchByIdAsync` + service reload) is an acceptable cost for a low-risk change. |
| D3 | Suspend guard inputs | Effective date = `suspendRequest.MatchDate ?? existingMatch.MatchDate`; venue = `existingMatch.VenueId`; exclude self via `existingMatch.Id`. | Check only when a new date is supplied | `SuspendMatchRequest` carries no venue, so the match's own `VenueId` is used; a null new date keeps the existing date, and self-exclusion prevents a match colliding with itself. |
| D4 | Guard placement / validation ordering | `CreateMatch`: after `mapper.Map<Match>`, before `CreateMatchAsync`. `SuspendMatch`: after load + null check, before `SuspendMatchAsync`. | Add a started/finished pre-check to Suspend | `CreateMatch` has no prior validation, so ordering is trivial; `CreateMatchAsync`'s internal ordering is untouched (guard is controller-side). No started/finished check is added to Suspend — none exists today and it is out of scope. |
| D5 | 2-hour boundary | Keep strict operators verbatim: `MatchDate > windowStart && MatchDate < windowEnd` (`MatchService.cs:96-97`). | — | **Confirmed**: boundary is **exclusive** — a match exactly 2h before/after is **not** a conflict; strictly under 2h apart **is**. Spec boundary scenario must assert exactly-2h = allowed. |
| D6 | Null venue | Both guards gated on `VenueId.HasValue` (no-op when null), identical to `UpdateMatchDate`. | — | Matches the shipped rule and the confirmed null-venue no-op requirement. |

## Data Flow

    CreateMatch (POST)   → map → [VenueId? & HasVenueScheduleConflictAsync] → 400  ┐
    SuspendMatch (PUT)   → load → [VenueId? & HasVenueScheduleConflictAsync] → 400 ├→ else → service persist → 200/201
    UpdateMatchDate (PUT, unchanged) ─────────────────────────────────────────────┘

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Club12-Backend/API/Controllers/MatchController.cs` | Modify | `CreateMatch`: guard between `Map` and `CreateMatchAsync`. `SuspendMatch`: load via `GetMatchByIdAsync`, null-check, resolve effective date, guard, then call `SuspendMatchAsync`. Both mirror `UpdateMatchDate:169-175`. |
| `Club12-Backend/API.Tests/` | Create | Tests for `HasVenueScheduleConflictAsync` (exact-2h exclusive boundary, cross-division/tournament, null-venue no-op) plus create-reject / suspend-reject / non-colliding-succeeds path coverage. |

No changes to `MatchService`, `IMatchService`, `ErrorMessages`, or the DB. **No signature changes anywhere.**

## Interfaces / Contracts

Unchanged. `HasVenueScheduleConflictAsync(Guid venueId, DateTime matchDate, Guid excludeMatchId)` reused as-is.

## Frontend Error Surfacing

Both write flows **already funnel errors through the shared mechanism** the edit flow uses:
`createMatch`/`addMatch` and `suspendMatch` in `match.context.tsx` each `catch → handleUnknownError(error)`
(`useUnknownErrorHandler` → `setError` → global error context), which renders the backend 400 body — the same
path `putMatchByMatchId` uses for the edit-venue conflict (`matchPage.tsx:167-177`). Design mandate: **no new
error-surfacing convention** — the create-match form mirrors `handleSaveEdit`'s shape (short-circuit on falsy
return, no bespoke catch); `StageMatchesByRound.handleConfirmSuspend` already checks the falsy return. Once the
backend guard lands, the Spanish message surfaces automatically. Tasks must confirm the create-match form routes
through the `createMatch` context method (not a bespoke axios call) so it inherits the surfacing.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `HasVenueScheduleConflictAsync`: exactly-2h-apart allowed, just-under-2h blocked, cross-division/tournament match, null/other-venue ignored, self excluded | Direct service test with in-memory/mocked `IMatchRepository` |
| Integration | `CreateMatch` and `SuspendMatch` return 400 + `VenueScheduleConflict` on collision; null-venue and non-colliding return 201/200 | Controller test mirroring existing `MatchController` test style |

Strict TDD: RED tests first. No `#pragma warning disable`/suppressions.

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary.

## Migration / Rollout

No migration required. Revert the commit to restore prior behavior.

## Open Questions

- [ ] None — status (400), null no-op, boundary semantics, and test scope are all resolved by fixed inputs and verified code.
