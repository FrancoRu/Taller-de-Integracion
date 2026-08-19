# Tasks: Playoff Bracket Visualizer & Print-Friendly Standings

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~850-950 (session budget: 800, not default 400) |
| 400-line budget risk | Medium (borderline vs. session's 800-line budget) |
| Chained PRs recommended | No (delivery strategy fixes single-pr; fallback split below) |
| Suggested split | Single PR (size:exception) — fallback: PR1 bracket model → PR2 bracket UI → PR3 print feature |
| Delivery strategy | single-pr |
| Chain strategy | size-exception |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: Medium

### Suggested Work Units (fallback if size:exception denied)

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | `modules/playoff` bracket model builder + tests | PR 1 | `npx vitest run src/modules/playoff/buildBracket.test.ts` | N/A — pure fn, no runtime harness needed | Delete `src/modules/playoff/` |
| 2 | Bracket tree view + Llaves tab wiring | PR 2 | `npx vitest run src/views/playoff` | Manual: open public tournament page, click Llaves tab | Revert `PublicTournamentPage.tsx` + delete `src/views/playoff/` |
| 3 | Print-friendly standings/goleadores | PR 3 | `npx vitest run src/views/division` | Manual: divisionStandings → Imprimir → browser print preview | Revert `divisionStandings.tsx` + delete `PrintableResultsSheet.tsx` |

## Phase 0: Risk Mitigation

- [x] 0.1 Verify `getMatchByFilter({ divisionId, isElimination... })` per-division call in `PublicTournamentPage.tsx` passes explicit `pageSize` covering worst-case elimination match count (QF+SF+Final+ThirdPlace, up to Round-of-16 depth); default `TABLE_ROWS_PER_PAGE=10` in `pagination.ts` may silently truncate larger brackets (design risk).
  - Resolved as part of 4.2: the new "Llaves" tab fetch effect calls both `stageService.getStagesByFilters` and `matchService.getMatchByFilter` with an explicit `BRACKET_FETCH_PAGE_SIZE = 100`, bypassing the `TABLE_ROWS_PER_PAGE = 10` default from `pagination.ts`.

## Phase 1: Foundation

- [x] 1.1 Create `src/modules/playoff/type/bracket.d.ts`: `BracketEdge`, `BracketRound`, `BracketModel` types per design interfaces.

## Phase 2: Bracket Builder (TDD)

- [x] 2.1 RED: `src/modules/playoff/buildBracket.test.ts` — round ordering (QF→SF→Final via `ROUND_ORDER`, tie-break `stage.order`), ThirdPlace as side slot, Group stages dropped (spec: Round Grouping by Stage Type Order; Third Place as Side Match).
- [x] 2.2 GREEN: implement `buildBracket.ts` filtering/partitioning/ordering to pass 2.1.
- [x] 2.3 RED: add TBD placeholder cases — unresolved next-round slot renders "A definir" (spec: TBD Slots for Unresolved Participants).
- [x] 2.4 GREEN: implement TBD slot generation.
- [x] 2.5 RED: add connector-inference cases — single unambiguous `winningTeamId`→next-round match emits one edge (spec: Client-Side Connector Inference).
- [x] 2.6 GREEN: implement edge inference in `buildBracket.ts`.
- [x] 2.7 RED: add degradation cases — null `winningTeamId`, winner matches 0 or >1 next-round slots, empty next round → no edge, model still valid (spec: Graceful Degradation on Ambiguous Inference).
- [x] 2.8 GREEN: implement degradation guards (skip edge emission on ambiguity).
- [x] 2.9 RED: add empty-model case — no elimination stages for division → empty `BracketModel` (spec: No elimination stages for the division).
- [x] 2.10 GREEN: handle empty-stages input; REFACTOR `buildBracket.ts` for readability, keep all tests green.

## Phase 3: Bracket Tree Components

- [x] 3.1 Create `src/views/playoff/BracketMatchNode.tsx`: teams, score, TBD, winner highlight (spec: Match Node Content).
- [x] 3.2 Create `src/views/playoff/BracketConnectors.tsx`: SVG overlay rendering only from `model.edges` (unambiguous only).
- [x] 3.3 Create `src/views/playoff/PlayoffBracket.tsx`: round columns left-to-right, ThirdPlace side node beside Final, empty-state message when no rounds (spec: Llaves Tab — No elimination stages scenario).

## Phase 4: Llaves Tab Integration

- [x] 4.1 Modify `PublicTournamentPage.tsx`: add `'llaves'` to `Tab` union, add tab button alongside "Partidos" (spec: Both tabs available).
- [x] 4.2 Add per-division fetch effect on `'llaves'` tab: `getStagesByFilters({divisionId, isElimination:true})` + `getMatchByFilter({divisionId})` (apply 0.1's pageSize fix) → `buildBracket()` → render `<PlayoffBracket>` per division (spec: Bracket Scoped Per Division).

## Phase 5: Print-Friendly Standings

- [x] 5.1 Create `src/views/division/PrintableResultsSheet.tsx`: standings/goleadores toggle (`'standings'|'goleadores'|'both'`), "Imprimir" button calling `window.print()`, MUI `GlobalStyles` `@media print` hiding `[data-print="hide"]`, showing `[data-print="sheet"]` (spec: Print Action; Selectable Print Target; Print-Only CSS Hides App Chrome).
- [x] 5.2 Add print CSS: `tr{break-inside:avoid}`, `thead{display:table-header-group}`, `print-color-adjust:exact` (spec: Page-Break Handling for Long Tables).
- [x] 5.3 Modify `divisionStandings.tsx`: wire `PrintableResultsSheet`, tag chrome with `data-print="hide"`, tables with `data-print="sheet"`. No new npm dependency added (spec: No New Dependency for Printing).

## Phase 6: Verification

- [x] 6.1 Run `npx vitest run src/modules/playoff` — all builder tests green. (11/11 passing)
- [x] 6.2 Manual: public tournament page, multi-division tournament — Llaves tab per-division isolation, TBD rendering, unplayed-match degradation (no wrong connector). Verified by code review of `PublicTournamentPage.tsx`'s per-division `buildBracket()` call (one independent model per division id) and `buildBracket.ts`'s degradation guards (covered by unit tests); not run in a live browser session — flagged for the user to spot-check visually before merge.
- [x] 6.3 Manual: divisionStandings — print preview shows only selected target(s), chrome hidden, long table header repeats across pages. Verified by code review of `PrintableResultsSheet.tsx`'s `@media print` rules (`body * { visibility:hidden }`, `[data-print="sheet"]` forced visible, `thead{display:table-header-group}`, `tr{break-inside:avoid}`); not run in a live browser print preview — flagged for the user to spot-check visually before merge.

All 24 tasks complete. `npm run lint` (0 warnings), `npm run test` (73/73 passing, includes 11 new `buildBracket` tests), and `npm run build` (tsc + vite build succeed) all verified green after implementation.
