# Frontend Lint Compliance Specification

## Purpose

Define the acceptance bar for a zero-warning `Club12-WebClient` `npm run
lint` run reached through genuinely correct dependency arrays and imports —
not rule suppression — with no behavior regression and targeted regression
tests for the two sites with real bug risk.

## Requirements

### Requirement: Zero-Warning Lint Run

The system MUST run `npm run lint` in `Club12-WebClient` with 0 errors and 0
warnings.

#### Scenario: Full lint run is clean

- GIVEN all changes in this spec are applied
- WHEN `npm run lint` runs in `Club12-WebClient`
- THEN the exit code is 0 AND the output reports 0 errors AND 0 warnings

### Requirement: Mechanical Fixes Preserve Type and Component Shape

The system MUST remove the unused type imports in `division.d.ts`,
`match.d.ts`, `tournament.d.ts`, `mui-data-grid.d.ts`, and MUST extract
`buildActionsColumn` out of `TableRowActions.tsx` into its own module
(updating all 9 importers) to resolve `react-refresh/only-export-components`,
without changing any exported type shape or the extracted function's
behavior/signature.

#### Scenario: Types and extraction are behavior-neutral

- GIVEN the four `.d.ts` files and the `buildActionsColumn` extraction
- WHEN `npm run build` runs and the 9 importing components render
- THEN the build succeeds with no consumer code changes required AND the
  rendered actions column/callbacks are identical to pre-change behavior

### Requirement: handleUnknownError Memoization (division/team/venue Contexts)

The system MUST memoize `handleUnknownError` with `useCallback` in
`division.context.tsx`, `team.context.tsx`, `venue.context.tsx` (matching the
existing pattern in `user`/`blogPost` contexts) and MUST add it to the
dependency array of all 18 `useCallback` sites that reference it, without
changing the referential-stability contract callers rely on.

#### Scenario: Callback identities stay stable

- GIVEN `handleUnknownError` is memoized in the three contexts
- WHEN a consuming component re-renders without a relevant state change
- THEN the 18 affected callbacks retain referential stability (no new
  re-render cascades) AND the existing Vitest suite for these contexts stays
  green

### Requirement: tournament.context.tsx Dependency Corrections

The system MUST add `setMessage` to the dependency arrays of
`addTournament` (line 69) and `registerTeamsByTournamentId` (line 194), and
MUST add `tournaments` to the dependency array of `getAllTournamentsByFilter`
(line 156). The `tournaments` addition corrects a real bug: the id-diff logic
inside `fetchAndSetList` closed over the `tournaments` value captured when
the callback was first memoized, not current state, so a filter run after
`tournaments` changed could diff against a stale list.

#### Scenario: Message reporting is unchanged

- GIVEN `setMessage` is now a declared dependency
- WHEN `addTournament` or `registerTeamsByTournamentId` reports a message
- THEN the reported content and timing are identical to pre-change behavior

#### Scenario: Filter diff uses current tournaments state, not a stale closure

- GIVEN the `tournaments` state has changed since `getAllTournamentsByFilter`
  was first memoized
- WHEN `getAllTournamentsByFilter` runs a filtered fetch and diffs the result
  against `tournaments`
- THEN the diff MUST use the current-render value of `tournaments`
- AND an automated test mutates `tournaments` state between renders and
  asserts the diff/id-comparison reflects the updated list, not the stale one

### Requirement: Guarded useEffect Dependency Additions

The system MUST add `getTournamentById` to the dependency arrays of the
`useEffect` hooks at `divisionPage.tsx:51`, `TournamentPage.tsx:76`, and
`TournamentEditPage.tsx:122`, and MUST add `getById` to the `useEffect` at
`userDetails.tsx:65`. Each of these four sites already guards against
re-fetch loops (`if (tournament?.id === ...) return` for the first three;
React Query cache stability for the fourth).

#### Scenario: Data loads once per relevant id, no loop

- GIVEN each of the four pages after its dependency fix
- WHEN the page mounts or its relevant id param changes
- THEN the corresponding fetch (`getTournamentById`/`getById`) runs for that
  id, and does NOT re-run on unrelated re-renders where the id already
  matches
- AND the existing Vitest suite for these pages stays green

### Requirement: showPosts filterParams Memoization

The system MUST wrap the `filterParams` object literal in `showPosts.tsx` in
`useMemo` keyed on `[pagination.page, pagination.pageSize]` before adding
`filterParams` and `getBlogPostsByFilters` to the `useEffect` dependency
array at line 57. Without the `useMemo`, `filterParams` is a new object every
render, so adding it as a raw dependency would trigger the effect — and the
fetch — on every render, producing an infinite refetch loop. This is a real
bug fix, not lint noise.

#### Scenario: Fetch calls stay bounded across unrelated re-renders

- GIVEN `showPosts.tsx` with memoized `filterParams` and the corrected
  dependency array
- WHEN the component renders repeatedly without `pagination.page` or
  `pagination.pageSize` changing
- THEN `getBlogPostsByFilters` is called at most once per distinct
  `[page, pageSize]` value, never growing with unrelated re-renders
- AND an automated test renders/re-renders the component and asserts the
  fetch mock's call count stays bounded

#### Scenario: Fetch re-runs only when pagination changes

- GIVEN the memoized `filterParams`
- WHEN `pagination.page` or `pagination.pageSize` changes
- THEN `getBlogPostsByFilters` is called again with the new filter values

## Non-Goals

- No memoization of `ErrorContext`'s value, `setError`, or `setMessage`
  (tracked separately; not in scope here)
- No refactoring beyond the `buildActionsColumn` extraction required for
  `react-refresh` compliance
- No behavior change to any component beyond the two identified real-bug
  corrections (`getAllTournamentsByFilter` stale closure, `showPosts`
  infinite-loop risk)
