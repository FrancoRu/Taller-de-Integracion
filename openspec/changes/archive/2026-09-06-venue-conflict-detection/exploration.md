# Exploration: venue-conflict-detection

## Correction to the initial gap report

The original survey's grep for `VenueConflict`/`double-book`/`overlapping` was a false negative.
Venue conflict detection **already exists** as `MatchService.HasVenueScheduleConflictAsync`
(`Club12-Backend/Application/Services/MatchService.cs:88`) with error constant
`ErrorMessages.Match.VenueScheduleConflict` (`Club12-Backend/Application/Utils/Constants/ErrorMessages.cs:358`)
— neither string contains "VenueConflict", so the earlier search missed it. This materially changes
the problem: it's not "build venue conflict detection from scratch," it's "the existing guard is wired
into only 1 of 3 places that can set a match's venue+time."

## Current State

- The check: fixed 2-hour window (`matchDate.AddHours(±2)`), queries `Match` by `VenueId` + window
  only — **no** StageId/DivisionId/TournamentId filter, so it already correctly covers cross-division
  and cross-tournament collisions.
- **Wired** into `MatchController.UpdateMatchDate` (PUT `/api/matches/{id}`) only — hard block,
  `400 BadRequest`, Spanish message already shipped and surfaced in `matchPage.tsx`'s edit-venue flow.
- **Missing** from `MatchController.CreateMatch` → `MatchService.CreateMatchAsync` —
  `CreateMatchRequest.VenueId` is a real `Guid?` field, so a manually created match can set a
  venue+date with zero guard.
- **Missing** from `MatchController.SuspendMatch` → `MatchService.SuspendMatchAsync(matchId, newMatchDate)`
  — moves `MatchDate` on an existing match without ever re-checking the (unchanged) venue against the
  new date.
- **Not a gap**: bulk wizard fixture generation. Verified end-to-end
  (`MatchService.CreateAutomatedMatchesAsync`, `BuildGroupStageMatchesAsync`,
  `CreateKnockoutStageMatchesAsync`, `CreateFinalStageMatchesAsync`, `BuildMatch`) — none ever assign
  `VenueId`; a grep for `VenueId` inside `MatchService.cs` returns only the one line inside the
  conflict-check itself. `submitWizard.ts` has zero venue references. Venues are only attached
  post-generation via the two manual endpoints above.
- `Venue` (`Club12-Backend/Domain/Entities/Models/Venue.cs`) is a single atomic bookable unit — `Name`,
  `Slug`, `Address`, `PhotoUrl`, `Latitude`, `Longitude` only, no field/court concept. `MatchDefaults.cs`
  has no duration concept either (only walkover scores), so "match duration" is implicitly the
  hardcoded 2-hour window.
- No test exercises the conflict logic itself. `Club12-Backend/API.Tests/MatchRescheduleMappingTests.cs`
  only proves teams survive a reschedule mapping — it is not conflict-detection coverage.

## Affected Areas

- `Club12-Backend/Application/Services/MatchService.cs` — add the guard call in `CreateMatchAsync`;
  restructure `SuspendMatchAsync` to check-before-persist (it currently updates then returns, with no
  re-check point)
- `Club12-Backend/API/Controllers/MatchController.cs` — mirror the existing `UpdateMatchDate` guard
  pattern (lines 170-175) in `CreateMatch` and `SuspendMatch`
- `Club12-Backend/Application/Utils/Constants/ErrorMessages.cs:358` — reuse `VenueScheduleConflict`
  message verbatim, no new copy needed
- `Club12-Backend/API.Tests/` — no existing coverage of `HasVenueScheduleConflictAsync`'s actual
  behavior; new tests needed regardless of chosen approach
- `Club12-WebClient/src/views/match/matchPage.tsx` — already handles this error on the edit path;
  create-match and suspend/reschedule UIs have no equivalent error surfacing yet

## Approaches

1. **Extend the existing guard verbatim to `CreateMatch` and `SuspendMatch`** — reuse
   `HasVenueScheduleConflictAsync` + `ErrorMessages.Match.VenueScheduleConflict` exactly as shipped
   today.
   - Pros: No new concepts/config; matches the codebase's own already-made hard-block decision;
     smallest diff; consistent UX everywhere.
   - Cons: `SuspendMatchAsync` needs a small check-before-persist refactor.
   - Effort: Low.

2. **Warn-with-override on the two missing paths**, diverging from the existing hard-block pattern.
   - Pros: Covers the hypothetical "two courts share one venue record" case without a data-model
     change.
   - Cons: Directly inconsistent with the already-shipped hard block on `UpdateMatchDate` for the
     identical rule; needs new response DTOs and confirmation-dialog UI on two more forms; the
     multi-court case is already solvable today via two separate `Venue` rows, so the added complexity
     isn't clearly justified by evidence of real pain.
   - Effort: Medium.

3. **Add a first-class field/court sub-resource under `Venue`** before wiring the missing checks.
   - Pros: More correct long-term model for multi-court venues.
   - Cons: New entity, FK, migration, CRUD UI — orthogonal to the actual reported gap (create/suspend
     not checking) and not evidenced by any current complaint, only a hypothetical.
   - Effort: High.

## Recommendation

Approach 1. The "hard block vs. warn" question is already answered by the codebase — this rule is live
as a hard 400 block with a Spanish error message. The real problem is inconsistent wiring, not an
undecided design. Recommend: reuse the guard in `CreateMatch` and in `SuspendMatchAsync` (checked
against the match's own existing `VenueId`, since `SuspendMatchRequest` carries no venue), add missing
tests for the conflict logic itself (boundary at exactly 2 hours, cross-division, etc.), keep the
2-hour window as a fixed constant (no duration-config infrastructure exists anywhere to hang a
configurable value on), and leave `Venue` atomic — do not bundle a court sub-resource into this change.

## Open Questions (need a user decision before sdd-propose)

1. Should `SuspendMatch`'s conflict response stay `400 BadRequest` (matches existing precedent) or move
   to `409 Conflict` (used elsewhere for venue delete-integrity)? Recommend `400` for consistency with
   the one shipped precedent.
2. Confirm intended behavior: a match created with `VenueId = null` has no conflict concern until a
   venue is later assigned (mirrors today's `UpdateMatchDate` no-op-when-null behavior).
3. Confirm this change should also add the currently-missing test coverage for
   `HasVenueScheduleConflictAsync` itself, rather than treating that as separate pre-existing debt.

## Risks

- `SuspendMatchAsync` has no covering tests today per the call-graph analysis — restructuring it to
  check-before-persist touches unverified behavior.
- Frontend: create-match and suspend/reschedule forms have no venue-conflict error UI yet; adding the
  backend guard alone will surface as an opaque generic error there until the frontend catches up.

## Ready for Proposal

Yes — scope is bounded (2 backend call sites + tests + minor frontend error surfacing) and the
hard-block-vs-warn question is resolved by existing precedent. Recommend confirming Open Questions 1-3
with the user before `sdd-propose`.
