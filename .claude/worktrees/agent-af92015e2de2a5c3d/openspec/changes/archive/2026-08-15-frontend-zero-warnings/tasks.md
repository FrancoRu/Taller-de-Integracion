# Tasks: Frontend Zero Lint Warnings

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~260-320 (additions + deletions) |
| 800-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | single PR |
| Delivery strategy | single-pr |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

Budget is 800 lines (session config). Breakdown: new `buildActionsColumn.tsx` (~50) + `TableRowActions.tsx` edit (~50) + 9 importer repoints (~18) + 4 `.d.ts` removals (~4) + 3 context files, memo + 18 deps (~30) + tournament.context 3 deps (~3) + 4 guarded effect deps (~4) + showPosts memo+deps (~10) + 2 new test files (~80-120). Total ~260-320, well under budget — no chaining/exception needed.

### Suggested Work Units

| Unit | Goal | PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----|----------------------|-----------------|-------------------|
| 1 | RED tests for 2 real-bug sites | same PR, 1st commit | `npm run test -- tournament.context showPosts` | Vitest+RTL, `ErrorProvider`+mocked sweetalert2, per `TeamsPage.test.tsx` | Revert 2 test files only |
| 2 | Mechanical/safe fixes (extraction, imports, 18 deps, 4 safe effect deps) | same PR | `npm run lint && npm run test` | `npm run build` | Revert per-file, independent of Unit 3 |
| 3 | Real-bug fixes (tournament `tournaments` dep, showPosts memo) turn Unit 1 GREEN | same PR | `npm run test -- tournament.context showPosts && npm run lint` | Same harness as Unit 1 | Revert 2 files; Unit 1 tests go RED again |

## Phase 1: RED — Regression Tests First

- [x] 1.1 Create `Club12-WebClient/src/modules/tournament/context/tournament.context.test.tsx`: `TournamentProvider`+`ErrorProvider` (mock `tournamentService`, `sweetalert2`); call `getAllTournamentsByFilter` twice via `act`; assert `tournaments` ref stable (`Object.is`)
- [x] 1.2 Run `npm run test -- tournament.context`, confirm it FAILS (stale closure defeats dedup) — empirically confirmed RED (`AssertionError: expected [...] to be [...] // Object.is equality`) before any production-code change
- [x] 1.3 Create `Club12-WebClient/src/views/blogPost/showPosts.test.tsx`: mock `getBlogPostsByFilters`, render in `MemoryRouter`, `waitFor` fetch, force re-render, assert call count bounded (1) — see Deviation note below re: exact assertion shape
- [x] 1.4 Run `npm run test -- showPosts`, confirm it FAILS against current baseline — empirically confirmed RED (timeout waiting for 2nd call) before any production-code change

## Phase 2: Extract buildActionsColumn (mechanical)

- [x] 2.1 Create `Club12-WebClient/src/views/core/components/buildActionsColumn.tsx`: move `buildActionsColumn`+`BuildActionsColumnOptions`; import `TableRowActions` default + `TableRowAction` type from `./TableRowActions`
- [x] 2.2 Edit `TableRowActions.tsx`: remove `buildActionsColumn`+`BuildActionsColumnOptions`; keep component, `TableRowAction`, `resolveRowValue`
- [x] 2.3 Repoint import in `TeamsPage.tsx`, `VenuesPage.tsx`, `TournamentsPage.tsx`, `stagesPage.tsx`, `PlayersPage.tsx`, `UsersPage.tsx`, `matchesPage.tsx`, `divisionsPage.tsx`, `PlayerSanctionsPage.tsx` to `@/views/core/components/buildActionsColumn`
- [x] 2.4 Run `npm run build`, confirm all 9 pages compile unchanged — build succeeded

## Phase 3: Unused Type Imports (mechanical)

- [x] 3.1 Remove unused import: `Club12-WebClient/src/modules/division/type/division.d.ts`
- [x] 3.2 Remove unused import: `Club12-WebClient/src/modules/match/type/match.d.ts`
- [x] 3.3 Remove unused import: `Club12-WebClient/src/modules/tournament/type/tournament.d.ts`
- [x] 3.4 Remove unused import: `Club12-WebClient/src/mui-data-grid.d.ts`

## Phase 4: handleUnknownError Memoization (mechanical, 18 deps)

- [x] 4.1 `division.context.tsx`: wrap `handleUnknownError` in `useCallback([setError])`; add to 7 dependent deps
- [x] 4.2 `team.context.tsx`: same pattern; add to 6 dependent deps
- [x] 4.3 `venue.context.tsx`: same pattern; add to 5 dependent deps
- [x] 4.4 Run `npm run test -- division team venue`, confirm green — 26/26 passed

## Phase 5: Guarded Dependency Additions (mechanical, safe)

- [x] 5.1 `tournament.context.tsx`: add `setMessage` to `addTournament` (~L69) and `registerTeamsByTournamentId` (~L194)
- [x] 5.2 Add `getTournamentById` to effect deps: `divisionPage.tsx` (~L51), `TournamentPage.tsx` (~L76), `TournamentEditPage.tsx` (~L122) — existing `tournament?.id` guards
- [x] 5.3 Add `getById` to effect deps: `userDetails.tsx` (~L65) — React Query cache stable
- [x] 5.4 Run full `npm run test`, confirm all 59 existing tests green — 59 passed, 2 new RED tests still failing as expected

## Phase 6: GREEN — tournament.context Stale-Closure Fix

- [x] 6.1 Add `tournaments` to `getAllTournamentsByFilter` deps (~L156), restoring `fetchAndSetList` dedup
- [x] 6.2 Run `npm run test -- tournament.context`, confirm 1.1 test now PASSES — GREEN

## Phase 7: GREEN — showPosts Infinite-Refetch Fix

- [x] 7.1 `showPosts.tsx`: wrap `filterParams` in `useMemo([pagination.page, pagination.pageSize])`
- [x] 7.2 Add `filterParams`+`getBlogPostsByFilters` to effect deps (~L57)
- [x] 7.3 Run `npm run test -- showPosts`, confirm 1.3 test now PASSES — GREEN

## Phase 8: Full Verification

- [x] 8.1 `npm run lint`: confirm 0 errors, 0 warnings — exit code 0, zero output
- [x] 8.2 `npm run test`: confirm 61/61 pass (59 existing + 2 new) — 22 test files, 61 tests passed
- [x] 8.3 `npm run build`: confirm success, no consumer-code changes beyond 9 import repoints — `tsc && vite build` succeeded; `npx tsc --noEmit` also clean

## Deviation Note — showPosts RED test assertion

Task 1.3 as originally worded ("assert call count bounded (1)" via plain re-render)
does NOT genuinely fail against the current unmodified `showPosts.tsx`: with only
`[pagination.page]` as the effect dependency, unrelated re-renders never retrigger
the effect today (verified empirically), so a bare "stays at 1 across re-renders"
assertion passes even on the buggy file — it would not be a real RED test.

The actual defect empirically present in the current code is different and more
precise: the effect's dependency array omits `pagination.pageSize`, so if the
server ever echoes back a different `pageSize` than requested, the component
never refetches with the corrected value (stale filterParams closure, permanently
bounded at 1 call even though pagination state changed). The implemented test
captures this: it asserts the first call uses the client-requested `pageSize`
(10), then asserts a *second* call occurs once the pagination state's `pageSize`
changes from the server response, carrying the new value (25). This test
genuinely times out (RED) against the unmodified file and genuinely passes
(GREEN) after the `useMemo`/deps fix in Phase 7 — matching the underlying spec
scenario ("Fetch re-runs only when pagination changes") more precisely than the
original "just re-render" framing.
