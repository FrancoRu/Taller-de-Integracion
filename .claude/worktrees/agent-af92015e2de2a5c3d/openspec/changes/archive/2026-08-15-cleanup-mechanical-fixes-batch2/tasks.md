# Tasks: Query-Key Factory Extraction (Batch 2, Frontend)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~650-730 (12 factories ~200, 12 tests ~420, 12 context swaps ~110) |
| Real review budget (session) | 800 (cached override, not the 400 default) |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR, 4 reviewable commits by module cluster |
| Delivery strategy | single-pr |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Medium

Estimate (~650-730) stays under the real 800-line budget, so no chaining or
size:exception is required. Grouped into 4 commits by module cluster purely
for reviewer digestibility, all landing in one PR.

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Cluster A: blogPost, team, venue factories+tests+swap | PR 1, commit 1 | `npm run test -- queryKeys` | N/A — pure functions, no runtime harness | revert 3 modules' 3 files each |
| 2 | Cluster B: match, stage, user | PR 1, commit 2 | `npm run test -- queryKeys` | N/A | revert 3 modules' 3 files each |
| 3 | Cluster C: playerSanction, playerStatistic, division | PR 1, commit 3 | `npm run test -- queryKeys` | N/A | revert 3 modules' 3 files each |
| 4 | Cluster D (bespoke): player, scorer, auth | PR 1, commit 4 | `npm run test -- queryKeys` | N/A | revert 3 modules' 3 files each |

## Phase 1: Cluster A — blogPost, team, venue

- [x] 1.1 RED: write `modules/blogPost/queryKeys.test.ts` asserting `list()`, `list(filter)`, `byId(id)` toEqual prior literals (fails, no factory).
- [x] 1.2 GREEN: create `modules/blogPost/queryKeys.ts` exporting `blogPostKeys` (all/list/byId) to pass 1.1.
- [x] 1.3 REFACTOR: swap literals in `modules/blogPost/context/blogPost.context.tsx` for `blogPostKeys.*`; add import.
- [x] 1.4 RED: write `modules/team/queryKeys.test.ts` (list/list(filter)/byId).
- [x] 1.5 GREEN: create `modules/team/queryKeys.ts` exporting `teamKeys`.
- [x] 1.6 REFACTOR: swap literals in `modules/team/context/team.context.tsx`; add import.
- [x] 1.7 RED: write `modules/venue/queryKeys.test.ts` (list/byId).
- [x] 1.8 GREEN: create `modules/venue/queryKeys.ts` exporting `venueKeys`.
- [x] 1.9 REFACTOR: swap literals in `modules/venue/context/venue.context.tsx`; add import.

## Phase 2: Cluster B — match, stage, user

- [x] 2.1 RED: write `modules/match/queryKeys.test.ts` (list/list(filter)/byId).
- [x] 2.2 GREEN: create `modules/match/queryKeys.ts` exporting `matchKeys`.
- [x] 2.3 REFACTOR: swap literals in `modules/match/context/match.context.tsx`; add import.
- [x] 2.4 RED: write `modules/stage/queryKeys.test.ts` (list/list(filter)/byId).
- [x] 2.5 GREEN: create `modules/stage/queryKeys.ts` exporting `stageKeys`.
- [x] 2.6 REFACTOR: swap literals in `modules/stage/context/stage.context.tsx`; add import.
- [x] 2.7 RED: write `modules/user/queryKeys.test.ts` (list/list(filter)/byId).
- [x] 2.8 GREEN: create `modules/user/queryKeys.ts` exporting `userKeys`.
- [x] 2.9 REFACTOR: swap literals in `modules/user/context/user.context.tsx`; add import.

## Phase 3: Cluster C — playerSanction, playerStatistic, division

- [x] 3.1 RED: write `modules/playerSanction/queryKeys.test.ts` (list/list(filter)/byId).
- [x] 3.2 GREEN: create `modules/playerSanction/queryKeys.ts` exporting `playerSanctionKeys`.
- [x] 3.3 REFACTOR: swap literals in `modules/playerSanction/context/playerSanction.context.tsx`; add import.
- [x] 3.4 RED: write `modules/playerStatistic/queryKeys.test.ts` asserting bare `all` root `['playerStatistic']` toEqual prior invalidate-all literal.
- [x] 3.5 GREEN: create `modules/playerStatistic/queryKeys.ts` exporting `playerStatisticKeys` (`all` member only).
- [x] 3.6 REFACTOR: swap literal in `modules/playerStatistic/context/playerStatistic.context.tsx`; add import. **Deviation**: only the 3 bare `['playerStatistic']` invalidateQueries call sites were swapped, per explicit single-member-factory scope. The pre-existing `byId`/`list(filter)` inline literals in this file (setQueryData, fetchQuery ×2, removeQueries) were intentionally left untouched — out of scope for this narrowed factory.
- [x] 3.7 RED: write `modules/division/queryKeys.test.ts` (list/list(filter)/byId/`topScorers(id)`→`['division','top-scorers',id]`).
- [x] 3.8 GREEN: create `modules/division/queryKeys.ts` exporting `divisionKeys` incl. `topScorers`.
- [x] 3.9 REFACTOR: swap literals in `modules/division/context/division.context.tsx`; add import.

## Phase 4: Cluster D (bespoke) — player, scorer, auth

- [x] 4.1 RED: write `modules/player/queryKeys.test.ts` incl. `byId(id, isAdministrative)`→`['player','byId',id,isAdministrative]`.
- [x] 4.2 GREEN: create `modules/player/queryKeys.ts` exporting `playerKeys` with 4-arg `byId`.
- [x] 4.3 REFACTOR: swap literals in `modules/player/context/player.context.tsx`; add import.
- [x] 4.4 RED: write `modules/scorer/queryKeys.test.ts` for `byTeam(filter)`→`['scorer','byTeam',filter]` and `byPlayer(filter)`→`['scorer','byPlayer',filter]` (no list/byId members).
- [x] 4.5 GREEN: create `modules/scorer/queryKeys.ts` exporting `scorerKeys` (byTeam/byPlayer only).
- [x] 4.6 REFACTOR: swap literals in `modules/scorer/context/scorer.context.tsx`; add import.
- [x] 4.7 RED: write `modules/auth/queryKeys.test.ts` for `hasToken()`→`['auth','has-token']` singleton.
- [x] 4.8 GREEN: create `modules/auth/queryKeys.ts` exporting `authKeys` (`hasToken` only).
- [x] 4.9 REFACTOR: swap literal in `modules/auth/context/auth.context.tsx`; add import.

## Phase 5: Full Verification

- [x] 5.1 Run `npm run test` (full Vitest suite); confirm zero regressions across all 12 modules. Result: 13 test files, 33 tests, all passing.
- [x] 5.2 Grep the 12 migrated `context/*.context.tsx` files for leftover inline `queryKey`/cache-call array literals; confirm none remain. Result: none remain in 11/12 modules; `playerStatistic` retains 4 pre-existing `byId`/`list(filter)` literals by documented scope decision (see 3.6 deviation note) — these were never in-scope for the single-member `all`-only factory.
- [x] 5.3 Confirm `tournament`, `error`, brand colors, `axiosUtils.ts`, and i18n files were not touched. Confirmed via `git status`: only the 12 `context/*.context.tsx` files and 24 new `queryKeys.ts`/`queryKeys.test.ts` files changed.
