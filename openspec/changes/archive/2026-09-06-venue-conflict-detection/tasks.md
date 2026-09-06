# Tasks: Venue Conflict Detection at All Match Write Paths

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~150-220 (1 modified controller file, 2 new test files) |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

This is a small, low-risk, single-PR change per design.md: one modified file
(`MatchController.cs`), two new test files, no service/DTO/entity/migration
changes, no chaining needed.

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Guard `CreateMatch` + `SuspendMatch` with full test coverage | PR 1 (only PR) | `dotnet test --filter FullyQualifiedName~VenueScheduleConflict` | `dotnet test Club12-Backend/API.Tests` | Revert the two guard hunks in `MatchController.cs`; delete both new test files |

## Phase 1: Coverage — Characterize Existing `HasVenueScheduleConflictAsync`

No production change; proves the already-shipped logic (design D5) before reuse.

- [x] 1.1 New `Club12-Backend/API.Tests/MatchServiceVenueScheduleConflictTests.cs`: just-under-2h, same venue → `true`.
- [x] 1.2 Exactly-2h apart, same venue → `false` (exclusive boundary, D5).
- [x] 1.3 Same time, different venue → `false`.
- [x] 1.4 Same venue/window, different Division + Tournament → `true` (cross-division/tournament reach).
- [x] 1.5 `excludeMatchId` excludes the match from colliding with itself → `false`.
- [x] 1.6 Run `dotnet test --filter MatchServiceVenueScheduleConflictTests` — all green immediately (no prod change).

## Phase 2: RED — Controller Guard Tests (must fail against current code)

New `Club12-Backend/API.Tests/MatchControllerVenueConflictTests.cs`, `IClassFixture<CustomWebApplicationFactory>`, mirroring `DivisionRosterControllerTests.cs`'s seed/authenticated-client shape.

- [x] 2.1 `POST /api/matches` colliding venue+time → expect `400` + `ErrorMessages.Match.VenueScheduleConflict`.
- [x] 2.2 `POST /api/matches` with `VenueId = null`, same colliding date → expect `201`.
- [x] 2.3 `POST /api/matches` exactly 2h after existing match, same venue → expect `201`.
- [x] 2.4 `POST /api/matches` same time, different venue → expect `201`.
- [x] 2.5 `PUT /api/matches/{id}/suspend` new date colliding at the match's own venue → expect `400` + message, and re-fetch proves `MatchDate` unchanged.
- [x] 2.6 `PUT /api/matches/{id}/suspend` non-colliding new date → expect `200`, `MatchDate` updated.
- [x] 2.7 `PUT /api/matches/{id}/suspend` on a match with `VenueId = null` → expect `200` regardless of date.
- [x] 2.8 Run `dotnet test --filter MatchControllerVenueConflictTests` — 2.1 and 2.5 must FAIL (RED); others pass since no guard blocks them yet.

## Phase 3: GREEN — Wire the Guards

- [x] 3.1 `MatchController.CreateMatch`: after `mapper.Map<Match>(matchRequest)`, before `CreateMatchAsync`, add `if (mappedMatch.VenueId.HasValue && await matchService.HasVenueScheduleConflictAsync(mappedMatch.VenueId.Value, mappedMatch.MatchDate, Guid.Empty)) return BadRequest(ErrorMessages.Match.VenueScheduleConflict);` (unpersisted match has no id to self-exclude; `Guid.Empty` never matches a stored row).
- [x] 3.2 `MatchController.SuspendMatch`: load `existingMatch` via `matchService.GetMatchByIdAsync(id)`; 404 via `NotFoundProblem` if null; resolve `DateTime effectiveDate = suspendRequest.MatchDate ?? existingMatch.MatchDate`; guard on `existingMatch.VenueId.HasValue && HasVenueScheduleConflictAsync(existingMatch.VenueId.Value, effectiveDate, existingMatch.Id)` → `BadRequest(...)`; else call the unchanged `matchService.SuspendMatchAsync(id, suspendRequest.MatchDate)`.
- [x] 3.3 Add `[ProducesResponseType(StatusCodes.Status400BadRequest)]` to `SuspendMatch` (new possible response).
- [x] 3.4 Run `dotnet test --filter FullyQualifiedName~VenueScheduleConflict` — all of Phase 1 and Phase 2 green.

## Phase 4: Frontend Verification (per design.md risk)

- [x] 4.1 Trace the create-match UI's actual submit call: confirm it invokes `match.context.tsx`'s `addMatch`/`createMatch` (which already `catch → handleUnknownError`), not a bespoke axios call. No create-match view currently calls `addMatch` per repo search — locate the real entry point (admin panel form) and verify its call path before concluding zero frontend work is needed.
  **Finding**: grepped every file under `Club12-WebClient/src` for `addMatch(`/`createMatch(` call sites (not the definition). The only matches are the declaration in `match.d.ts`, the implementation in `match.context.tsx`, and its `useMutation` wiring — zero view/component anywhere calls `addMatch`. There is currently no UI entry point for manually creating a match (fixtures are generated via the automated `generate` endpoint, not `CreateMatch`). Confirmed, not assumed.
- [x] 4.2 If a bespoke call path is found, wire it through the context method instead (small change); otherwise no frontend change needed — record that finding.
  **Finding**: no bespoke call path exists because no call path exists at all yet. No frontend change is needed for `CreateMatch`'s new guard — there is nothing to wire, since no shipped view invokes it. `addMatch`/`createMatch` already funnels through `catch → handleUnknownError` for whenever a create-match UI is eventually built.
- [x] 4.3 Confirm `StageMatchesByRound.handleConfirmSuspend`'s existing falsy-return short-circuit surfaces the new suspend conflict message (no code change expected, verification only).
  **Finding**: verified in `match.context.tsx` — `suspendMatch` catches any error via `handleUnknownError(error)` and falls through returning `undefined` (no explicit return in the catch block). `StageMatchesByRound.handleConfirmSuspend` (`StageMatchesByRound.tsx:101-116`) already does `const result = await suspendMatch(...); if (result) { closeSuspend(); await reload(); }` — a falsy `result` on the new 400 leaves the dialog open with the global error message already surfaced by `handleUnknownError`. No code change needed; verified by reading, not assumed.

## Phase 5: Full Regression

- [x] 5.1 `dotnet build` clean, no warnings introduced. **Result**: `Build succeeded. 0 Warning(s) 0 Error(s)`.
- [x] 5.2 `dotnet test` (full suite) green. **Result**: `Passed! - Failed: 0, Passed: 921, Skipped: 0, Total: 921` (909 pre-existing + 12 new).
- [x] 5.3 If Phase 4 required a frontend edit: `npm run test`, `tsc --noEmit`, `npm run lint`, `npm run build` all clean in `Club12-WebClient`. **Not applicable** — Phase 4 concluded no frontend edit is needed (see 4.1/4.2 findings), so this step is skipped by design, not omitted by oversight.
