# Proposal: Frontend Zero Lint Warnings

## Intent

`npm run lint` in Club12-WebClient reports 0 errors but 32 warnings. The backend just reached genuine 0/0/0; the frontend should match so warnings stay meaningful signal, not background noise. Fixes must make dependency arrays genuinely correct — not suppress the rule.

## Scope

### In Scope
- 5 easy warnings: remove unused type imports (`division.d.ts`, `match.d.ts`, `tournament.d.ts`, `mui-data-grid.d.ts`) and resolve `react-refresh/only-export-components` in `TableRowActions.tsx` by extracting `buildActionsColumn` to its own module (updates 9 importers).
- 18 `useCallback` sites (division/team/venue contexts) missing `handleUnknownError`: memoize `handleUnknownError` with `useCallback(fn, [setError])` in those 3 contexts (matching the pattern user/blogPost contexts already use), then add it to the dep arrays.
- 3 tournament-context sites: add `setMessage` (addTournament:69, registerTeamsByTournamentId:194) and `tournaments` (getAllTournamentsByFilter:156 — real stale-closure feeding `fetchAndSetList`'s id-diff; matches `getTournamentById` which already lists `tournaments`).
- 5 `useEffect` sites: add `getTournamentById` (divisionPage:51, TournamentPage:76, TournamentEditPage:122 — safe: each has an `if (tournament?.id === …) return` guard that stops any loop), add `getById` (userDetails:65), and fix `showPosts:57` by `useMemo`-ing `filterParams` on `[pagination.page, pagination.pageSize]` then adding `filterParams` + `getBlogPostsByFilters`.

### Out of Scope
- Memoizing `ErrorContext` value / `setError`/`setMessage` (a real underlying smell, but broader; deps here are safe in practice because `ErrorProvider` rarely re-renders during affected flows).
- Any behavior change, unrelated refactor, or context API restructure.

## Capabilities

### New Capabilities
- None

### Modified Capabilities
- None (pure lint/correctness cleanup; no spec-level behavior change).

## Approach

Two logical slices in one PR. Slice A (mechanical, no loop risk): unused imports, `buildActionsColumn` extraction, `handleUnknownError` memoization + 18 deps, 3 tournament deps. Slice B (careful review): 5 `useEffect` deps, where `showPosts` needs the `filterParams` `useMemo` to avoid an infinite refetch loop. Estimated ~75 changed lines, well under the 800 budget.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/**/type/*.d.ts`, `src/mui-data-grid.d.ts` | Modified | Remove unused imports |
| `src/views/core/components/TableRowActions.tsx` + new module | Modified/New | Extract `buildActionsColumn`; update 9 importers |
| `src/modules/{division,team,venue}/context/*.context.tsx` | Modified | Memoize `handleUnknownError`, add 18 deps |
| `src/modules/tournament/context/tournament.context.tsx` | Modified | Add `setMessage` ×2, `tournaments` ×1 |
| `src/views/{division,tournament,user,blogPost}/*.tsx` | Modified | 5 effect dep fixes; `showPosts` filterParams useMemo |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Infinite refetch loop in `showPosts` | Med if naive | `useMemo` `filterParams` before adding dep |
| Effect loop from unstable `getTournamentById` | Low | Existing `tournament?.id` guards break loops |
| Lost `useCallback` memoization | Low | Memoize `handleUnknownError`, not raw add |

## Rollback Plan

Pure diff revert per slice; no data/schema/API impact. `git revert` the commit(s).

## Dependencies

- None. Run alone on `develop`.

## Success Criteria

- [ ] `npm run lint` reports 0 errors, 0 warnings.
- [ ] No new render loops; affected pages load once and behave unchanged.
- [ ] `npm run build` passes.
</content>
</invoke>
