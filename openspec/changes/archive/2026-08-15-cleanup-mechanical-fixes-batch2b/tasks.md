# Tasks: Cleanup — Mechanical / Behavior-Preserving Fixes (Batch 2b, Frontend)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 230-280 |
| 800-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | single PR |
| Delivery strategy | single-pr |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low (real session budget is 800; ~230-280 lines fits comfortably, no size:exception needed)

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Route constant + consumers | PR 1 (single PR) | `vitest run routes.test.ts axiosUtils` | N/A — pure value/unit test, no server | `routes.ts`, `axiosUtils.ts`, `App.tsx` |
| 2 | Color tokens + swap-over | PR 1 (single PR) | `vitest run theme.color-tokens.test.ts` | N/A — value-equality only, no jsdom render | All `#FD6B00`/`#d33` call sites |
| 3 | I18n dead-file deletion | PR 1 (single PR) | `npm run build && npm run test` | N/A — deletion only | 2 deleted files |

## Phase 1: Route Constant (Foundation)

- [x] 1.1 RED: test `routes.tokenInvalido === '/token-invalido'` in `Club12-WebClient/src/modules/core/constants/routes.test.ts`
- [x] 1.2 GREEN: add `tokenInvalido: '/token-invalido'` to `routes.ts`
- [x] 1.3 Update `axiosUtils.ts:11` to `const INVALID_TOKEN_PATH = routes.tokenInvalido`
- [x] 1.4 Update `App.tsx:67` to read `routes.tokenInvalido`
- [x] 1.5 Test: 401-with-auth-header still calls `window.location.assign('/token-invalido')`

## Phase 2: Color Tokens in theme.ts

- [x] 2.1 RED: `theme.color-tokens.test.ts` asserting `theme.palette.primary.main === '#FD6B00'`, `CANCEL_BUTTON_COLOR === '#d33'`, `theme.palette.error.main === '#d32f2f'` unchanged
- [x] 2.2 GREEN: add `export const CANCEL_BUTTON_COLOR = '#d33';` to `theme.ts` (palette untouched)

## Phase 3: Color Site Swap-Over (bulk, sub-grouped)

- [x] 3.1 venue/division/match group — `VenuesPage.tsx`, `divisionsPage.tsx`, `matchesPage.tsx`, `MatchStatisticsTab.tsx`: import `theme` (+ `CANCEL_BUTTON_COLOR` where used), swap `#FD6B00`→`theme.palette.primary.main`, `#d33`→`CANCEL_BUTTON_COLOR`
- [x] 3.2 tournament group — `TournamentEditPage.tsx`, `TournamentPage.tsx`, `TournamentsPage.tsx`: same swap
- [x] 3.3 stage group — `stagesPage.tsx`, `stageCreatePage.tsx`, `stageEditPage.tsx`: same swap
- [x] 3.4 team group — `TeamPage.tsx`, `TeamRegisterPage.tsx`, `TeamsPage.tsx`: same swap
- [x] 3.5 playerSanction group — `playerSanctionCreatePage.tsx`, `playerSanctionDeletePage.tsx`, `playerSanctionEditPage.tsx`, `PlayerSanctionPage.tsx`: same swap
- [x] 3.6 player/panel group — `PlayerPage.tsx`, `PlayersPage.tsx`, `UsersPage.tsx`: same swap
- [x] 3.7 core/components group — `ErrorPageActions.tsx`, `ErrorPageLayout.tsx`: remove local `ORANGE` const, use `theme.palette.primary.main`
- [x] 3.8 Test/grep gate: zero remaining `#FD6B00`/`#d33` literals in `src/views/**` (outside `theme.ts`), zero `'primary.main'`-shorthand mistakes introduced by this change

## Phase 4: Dead I18n File Removal

- [x] 4.1 Verify: grep `languajes/spanish` and `languajes/english` import specifiers across `Club12-WebClient/src` — confirm zero matches
- [x] 4.2 Delete `src/modules/core/languajes/spanish.ts` and `english.ts`

## Phase 5: Final Verification

- [x] 5.1 Full grep gate: zero `#FD6B00`, zero `#d33`, zero hardcoded `/token-invalido` outside `routes.ts`
- [x] 5.2 `npm run build`
- [x] 5.3 Typecheck (`tsc --noEmit` or project script)
- [x] 5.4 Lint (pre-existing 32-warning baseline, unchanged by this change — verified via git-stash diff; `--max-warnings 0` fails on pre-existing warnings unrelated to this scope)
- [x] 5.5 `npm run test` (full Vitest suite green, including new equivalence tests) — 16 files / 40 tests passed
