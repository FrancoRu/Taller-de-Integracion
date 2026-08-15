# Design: Frontend Zero Lint Warnings

## Technical Approach

Eliminate all 32 `npm run lint` warnings in `Club12-WebClient` by making dependency
arrays genuinely correct (never suppressing rules) and by splitting a mixed-export
module. Two implementation slices: (A) mechanical/safe, (B) two real-bug fixes with
regression tests. No spec-level behavior change; two latent bugs are corrected.

## Architecture Decisions

| Decision | Choice | Rejected | Rationale |
|----------|--------|----------|-----------|
| Fix `react-refresh/only-export-components` in `TableRowActions.tsx` | Extract `buildActionsColumn` (and its local `BuildActionsColumnOptions`) to new colocated module `src/views/core/components/buildActionsColumn.tsx`; keep `TableRowActions` default component + `TableRowAction` type in place | Add eslint-disable; move component instead | Codebase colocates view helpers under `views/core/components`; type-only named exports do not trigger the rule, so leaving `TableRowAction` there is clean. `.tsx` required because the builder returns JSX in `renderCell`. |
| 18 `useCallback` sites missing `handleUnknownError` (division 7 / team 6 / venue 5) | Wrap each context's own `handleUnknownError` in `useCallback([setError])`, then add it to each dependent callback's deps | Add raw unstable fn to deps; disable rule | `user.context`/`blogPost.context` ALREADY use `useCallback([setError])` — mirroring is the consistent, correct fix. Adding an unstable fn would defeat memoization. |
| tournament.context deps | `addTournament` +`setMessage`; `registerTeamsByTournamentId` +`setMessage`; `getAllTournamentsByFilter` +`tournaments` | Suppress | First two are harmless completeness fixes; the third fixes a real stale-closure. |
| showPosts refetch | `useMemo` `filterParams` on `[pagination.page, pagination.pageSize]`, THEN add `filterParams` + `getBlogPostsByFilters` to effect deps | Naively add inline `filterParams` to deps | Inline object is recreated each render → naive add = infinite refetch loop. Memoize first. |
| ErrorContext memoization | Out of scope | Memoize `setError`/`setMessage`/context value | Real smell (fresh literals each render) but broader; `ErrorProvider` re-renders only on its own `errors` state, and affected callbacks are user-event handlers, not effect deps — no loop. |

## Data Flow — tournament stale-closure (bug + fix)

`getAllTournamentsByFilter` → `fetchAndSetList({ currentState: tournaments, ... })`.
`fetchAndSetList` only calls `setState` when sorted new IDs differ from `currentState`
IDs. Deps are `[setTournaments, setError]`, so the callback is created once with
`tournaments = null` and NEVER recreated → `currentState` is permanently the stale
mount value. The dedup guard is fully defeated: every list fetch re-sets the whole
array, clobbering single-item upserts written by `getTournamentById`/`putTournamentById`
(via the `[tournament]` upsert effect). Adding `tournaments` to deps makes the closure
see live state, restoring correct dedup.

## Data Flow — showPosts loop (bug + fix)

`filterParams` inline object recreated each render. Today the effect lists only
`[pagination.page]`, so lint flags missing `filterParams`/`getBlogPostsByFilters`.
`author/title/keyword` are constant `''`; the only dynamic fields are page & pageSize —
so `useMemo(..., [pagination.page, pagination.pageSize])` is the exact correct dep array
(no search/sort state exists in this component). `getBlogPostsByFilters` is a stable
`useCallback` from the already-correct `blogPost.context`, so adding it is safe.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/views/core/components/buildActionsColumn.tsx` | Create | Extracted `buildActionsColumn` + `BuildActionsColumnOptions`; imports `TableRowActions` default + `TableRowAction` type from `./TableRowActions` |
| `src/views/core/components/TableRowActions.tsx` | Modify | Remove `buildActionsColumn` + `BuildActionsColumnOptions`; retain component, `TableRowAction`, `resolveRowValue` |
| 9 pages (`TeamsPage`, `VenuesPage`, `TournamentsPage`, `stagesPage`, `PlayersPage`, `UsersPage`, `matchesPage`, `divisionsPage`, `PlayerSanctionsPage`) | Modify | Repoint `buildActionsColumn` import to `@/views/core/components/buildActionsColumn`; leave any `TableRowAction` type import on `TableRowActions` |
| `division/team/venue context.tsx` | Modify | `handleUnknownError` → `useCallback([setError])`; add to deps of each dependent callback |
| `tournament/context/tournament.context.tsx` | Modify | +`setMessage` (2 callbacks), +`tournaments` (1 callback) |
| `views/blogPost/showPosts.tsx` | Modify | `useMemo` `filterParams`; effect deps `[filterParams, getBlogPostsByFilters]` |
| `division.d.ts`, `match.d.ts`, `tournament.d.ts`, `mui-data-grid.d.ts` | Modify | Remove unused type imports |
| `views/blogPost/showPosts.test.tsx` | Create | Loop-boundedness regression test |
| `modules/tournament/context/tournament.context.test.tsx` | Create | Stale-closure regression test |

## Testing Strategy

| Layer | What | Approach (existing Vitest + Testing Library harness, cf. `TeamsPage.test.tsx`) |
|-------|------|-------------------------------------------------------------------------------|
| Unit | showPosts no infinite refetch | `vi.mock('@/modules/blogPost/hook/blogPost.hook')`, `getBlogPostsByFilters` resolves a fixed page; render in `MemoryRouter`; `waitFor` initial fetch; flush microtasks/rerender; assert call count stays **1** (bounded) |
| Unit | tournament dedup after fix | `renderHook` wrapping `ErrorProvider`+`TournamentProvider` (mock `tournamentService`, mock `sweetalert2`); call `getAllTournamentsByFilter` twice with identical data in `act`; assert `tournaments` reference is **stable** (`Object.is`) — proves closure sees live state and guard dedups |
| Safety | 18 useCallback + 3 useEffect + userDetails | No new test: pure identity/dep completeness; guards (`tournament?.id ===`, React Query cache) already bound loops |

## Zero-behavior-change confirmation (safe sites)

- **18 useCallback**: memoizing `handleUnknownError` + adding to deps only changes callback
  identity, not runtime effect; these are user-event handlers, never effect deps → no refetch.
- **3 useEffect `getTournamentById`** (divisionPage:51, TournamentPage:76, TournamentEditPage:122):
  each has `if (tournament?.id === …) return` guard → loop-safe.
- **userDetails `getById`**: stable via React Query cache → loop-safe.

## Behavior CORRECTION (2 real-bug sites)

- **showPosts**: prevents a potential infinite refetch loop when the dep is completed correctly.
- **tournament.context:156**: restores the `fetchAndSetList` dedup guard so list fetches no
  longer clobber single-item upserts and no longer re-set state on unchanged results.

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary.

## Migration / Rollout

No migration required. Pure per-slice diff revert.

## Open Questions

None.
