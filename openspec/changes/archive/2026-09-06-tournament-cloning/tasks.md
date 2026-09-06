# Tasks: Tournament Cloning (wizard-prefill)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 350-450 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Backend structure DTO + service + endpoint | PR 1 | `dotnet test --filter TournamentStructure` | `dotnet test Club12-Backend/API.Tests` | Revert new DTO/service method/endpoint; no existing code touched |
| 2 | Frontend `cloneWizard.ts` + wizard prefill + "Clonar torneo" UI | PR 2 | `npm run test -- cloneWizard` | `npm run test` in `Club12-WebClient` | Revert `cloneWizard.ts`, `location.state` widening, and `TournamentPage.tsx` action; wizard/submit path untouched |

If the two units land as one PR, re-check line count before requesting `size:exception`.

## Phase 1: Backend Foundation — Structure DTO

- [x] 1.1 Create `Club12-Backend/Application/DTOs/Tournament/Response/TournamentStructureResponse.cs` with `TournamentStructureResponse`, `DivisionStructureResponse`, `StageStructureResponse` per design's contract, reusing existing `PlayoffMappingResponse`.
- [x] 1.2 Add AutoMapper profile entries: `Tournament→TournamentStructureResponse`, `Division→DivisionStructureResponse`, `Stage→StageStructureResponse`.

## Phase 2: Backend Service — `GetTournamentStructureAsync` (RED/GREEN)

- [x] 2.1 RED — add failing tests in `Club12-Backend/API.Tests/` asserting `GetTournamentStructureAsync` assembles the full tree for: (a) a simple single-zone tournament, (b) a division with M non-cup Group-type sub-stages, (c) a cross-division cup, (d) a playoffs-only (groupless) division; assert zero instance data (no rosters/matches) leaks into the DTO.
- [x] 2.2 GREEN — implement `GetTournamentStructureAsync(Guid)` on `ITournamentService`/`TournamentService`. Verified: `IGenericRepository`'s `includes: IEnumerable<Expression<Func<TEntity,object>>>` API only supports single-level `.Include()`, not `.ThenInclude()` — nested `Tournament.Divisions.Stages` cannot be expressed directly. Reuse the existing `EvaluateCompletabilityAsync` pattern instead: load the Tournament via `GetTournamentByIdAsync`, then separately load Divisions via `unitOfWork.DivisionRepository.FindAsync(d => d.TournamentId == id, includes: [d => d.Stages, d => d.PlayoffMappings], asSplitQuery: true)` and assemble the tree in the service. No `IGenericRepository` extension needed.
- [x] 2.3 GREEN — map the assembled graph to `TournamentStructureResponse` via AutoMapper; confirm all Phase 2.1 tests pass.

## Phase 3: Backend Endpoint

- [x] 3.1 RED — add failing `TournamentController` tests: `GET {idOrSlug}/structure` returns 200 with the full tree for an existing tournament (by id and by slug), and 404 for a missing one.
- [x] 3.2 GREEN — add the `GET {idOrSlug}/structure` action to `Club12-Backend/API/Controllers/TournamentController.cs`, resolving id-or-slug the same way existing tournament-detail endpoints do.
- [x] 3.3 Run `dotnet build` and full `dotnet test` — confirm clean, including Phase 2/3 new tests.

## Phase 4: Frontend `cloneWizard.ts` — Reverse Mapper (RED/GREEN, centerpiece)

- [x] 4.1 Add `ITournamentStructureResponse` types and `getStructure(idOrSlug)` to `Club12-WebClient/src/modules/tournament/service/tournament.service.ts` and `hook/tournament.hook.ts`.
- [x] 4.2 RED — write golden round-trip tests in a new `cloneWizard.test.ts` (sibling of `submitWizard.test.ts`): `submitWizard(structureToWizardState(structureResponse, category))` reconstructs an equivalent creation payload for (a) a simple single-zone tournament, (b) a multi-sub-group zone, (c) a cross-division cup, (d) a playoffs-only (groupless) division.
- [x] 4.3 RED — write mismatch-detection tests: sub-groups with inconsistent `RoundRobinLegs` → `review[]` notice + falls back to range minimum (not silently guessed); a `PlayoffMapping` whose `Destination` matches no `BracketName` → `review[]` notice for that cup; confirm other zones still pre-fill correctly.
- [x] 4.4 GREEN — implement `Club12-WebClient/src/views/tournament/wizard/cloneWizard.ts` exporting `structureToWizardState(dto, category)` per design's reverse-mapping table (D1): exact qualifiers from `PlayoffMapping` span/`groupCount × QualifiersPerGroup`, cross-check against recomputed `qualifiersToStageTypes`, fallback+notice on mismatch. Name prefill defaults to `"{source name} (copia)"` — a small, editable, easy-to-override default, not a hard requirement.
- [x] 4.5 GREEN — confirm all Phase 4.2/4.3 tests pass; `tsc --noEmit` clean for the new file.

## Phase 5: Wizard Prefill Wiring

- [x] 5.1 Widen `TournamentWizardPage.tsx`'s `location.state` type to `{ seasonId?: string; clonePrefill?: WizardState; cloneReview?: string[] }`; confirm the existing `{ seasonId }`-only launch path (from `AdminSeasonDetailPage.tsx`) still works unchanged.
- [x] 5.2 When `clonePrefill` is present, initialize wizard state from it instead of `createInitialWizardState()`; render `cloneReview` (when non-empty) as a persistent banner above the stepper.
- [x] 5.3 Extend `TournamentWizardPage` tests: prefilled state renders the cloned zones/cups; banner appears only when `cloneReview` is non-empty; blank `startDate`/`teamRegistrationDeadline` still block submit via existing validation (unchanged shared validators, already covered elsewhere).

## Phase 6: "Clonar torneo" UI Entry Point

- [x] 6.1 Add a "Clonar torneo" action to `Club12-WebClient/src/views/tournament/TournamentPage.tsx`, gated by the existing `canEditTournament` permission check.
- [x] 6.2 Add a category-choice dialog (explicit `Category` select, defaulting to the source's category as a convenience, always editable) triggered by the action.
- [x] 6.3 Wire the handler: on confirm, fetch `getStructure(idOrSlug)`, run `structureToWizardState(dto, chosenCategory)`, then `navigate(APP_ROUTES.panelTournamentWizard, { state: { clonePrefill, cloneReview } })`.
- [x] 6.4 Extend `TournamentPage.test.tsx`: clicking "Clonar torneo" opens the category dialog; confirming navigates to the wizard with the expected `clonePrefill`/`cloneReview` state; action is hidden/disabled when `canEditTournament` is false.

## Phase 7: Full Regression + End-to-End Verification

- [x] 7.1 Backend: `dotnet build` and `dotnet test` clean across the full solution.
- [x] 7.2 Frontend: `npm run test`, `tsc --noEmit`, `npm run lint`, `npm run build` all clean.
- [x] 7.3 End-to-end: `API.Tests/TournamentCloningEndToEndTests.cs` seeds a real `SampleTournamentBuilder` tournament (2 regular cup divisions + a cross-division cup), reads it through the real `GET {id}/structure` endpoint, reverse-maps it per the D1 rules (mirroring `cloneWizard.ts`), edits it (drops one zone), submits with a chosen category different from the source's through the real unchanged `POST /full`, and verifies in a fresh DB scope: dates/category are the chosen ones (never the source's), the dropped zone is absent, the retained regular zone's group/cup stages and PlayoffMapping span match, the cross-cup's groups/qualifiersPerGroup/bracket match, and every created division has zero `DivisionTeamRegistrations` and zero matches. (No browser-level E2E harness exists in this repo — Vitest/xUnit only — so this is the most complete cross-endpoint proof achievable in-process; the frontend reverse-mapper itself is proven separately by `cloneWizard.test.ts`'s golden round-trips against `submitWizard`.)
