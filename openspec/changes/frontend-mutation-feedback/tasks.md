# Tasks: Post-Mutation UI Feedback

All frontend (`Club12-WebClient`). Delivered on branch
`fix/venue-photo-edit-double-modal-and-refresh`.

## Phase 1 — Venue photo edit (commit `68704d0`)

- [x] 1.1 `useVenue.putVenuePhotoById`: after upload, `GET` the venue via the
  service (bypassing the `getVenueById` in-memory short-circuit), update
  state, return `IVenueResponse | void`.
- [x] 1.2 `IVenueContextProps.putVenuePhotoById` return type updated.
- [x] 1.3 `useVenue.putVenueById`: remove the success `setMessage` calls
  (200 + 204 branches); drop `setMessage` from that callback's deps.
- [x] 1.4 `venuePage.handleEditSubmit`: use the venue returned by the
  mutations (`setVenue(refreshed)`); drop the trailing `fetchVenue()`.
- [x] 1.5 Tests: `venue.context.test.tsx` (new) — no toast after
  `putVenueById`; `putVenuePhotoById` refetches even when cached and returns
  the fresh venue. `VenuePage.test.tsx` — updated "shows the updated venue";
  new "shows the new photo after upload without a reload".

## Phase 2 — Duplicate context toasts audit (commit `4312f89`)

- [x] 2.1 Grep every `*.context.tsx` for a success `setMessage(...)`;
  cross-reference the calling view for a `notify*` on the same path.
- [x] 2.2 Remove the success `setMessage` from: `venue.deleteVenueById`,
  `blogPost.addBlogPost`, `division.putDivisionById` (200 + 204),
  `division.deleteDivisionsById`, `season.putSeasonById` (200 + 204),
  `season.deleteSeasonById`, `tournament.createFullTournament`,
  `tournament.addFullDivision`, `tournament.registerTeamsByTournamentId`.
- [x] 2.3 Fix each affected `useCallback` dependency array.
- [x] 2.4 `blogPost.context`: `useError`/`setMessage` now unused — remove
  the import and the destructure.
- [x] 2.5 Confirm the kept flows (venue/season create, `putTournamentById`,
  enroll/unenroll, `suspendMatch`) still notify.
- [x] 2.6 Tests: extend `venue.context.test.tsx` (`deleteVenueById`) and
  `tournament.context.test.tsx` (`createFullTournament`, `addFullDivision`,
  `registerTeamsByTournamentId`); new `division.context.test.tsx`,
  `season.context.test.tsx`, `blogPost.context.test.tsx` — all asserting
  `Swal.fire` is not called after a successful mutation.

## Phase 3 — Backup catalog freshness (commit `dfed36c`)

- [x] 3.1 `useBackups`: add private `refreshCatalog()` (`GET` + `setBackups`,
  errors swallowed).
- [x] 3.2 `fetchBackups` wraps `refreshCatalog()` with the `loading` flag.
- [x] 3.3 `createBackup` and `restoreBackup` call `refreshCatalog()` after a
  successful service call instead of the optimistic prepend.
- [x] 3.4 `deleteBackup` unchanged (optimistic `filter` is exact).
- [x] 3.5 Tests: rewrite the `createBackup` and `restoreBackup` cases in
  `backup.hook.test.ts` to assert `getBackups` is called and the list equals
  the refetch result.

## Phase 4 — Unify blocking overlays (commit `5aab20e`)

- [x] 4.1 `requestActivity.ts`: add the LIFO message stack —
  `setBlockingMessage`, `clearBlockingMessage`, `getBlockingMessage`,
  `runWithBlockingMessage`. Keep the `Listener` signature; `notify()` on
  message changes.
- [x] 4.2 `GlobalLoadingOverlay.tsx`: second `useSyncExternalStore` for the
  message; `open = count > 0 || message !== null`; pass `message`.
- [x] 4.3 `DataAdministrationPage.tsx`: delete `activeOperation` + the
  `<Dialog>`; 4 handlers use `runWithBlockingMessage`; keep `isWiping`;
  drop the stale z-index/notify-ordering comment.
- [x] 4.4 `TournamentPage.tsx`: delete the local `<BlockingOverlay>` +
  import; wrap the revert in `runWithBlockingMessage`.
- [x] 4.5 `TournamentDivisionAssignment.tsx`: delete the local
  `<BlockingOverlay>` + import + the now-unused `starting` state; use
  `setBlockingMessage`/`clearBlockingMessage` in `handleStart`.
- [x] 4.6 Tests: extend `requestActivity.test.ts` (message stack: newest
  wins, out-of-order clear, notify, `runWithBlockingMessage` clears on
  throw) and `GlobalLoadingOverlay.test.tsx` (shows the message + spinner
  with no request in flight).

## Phase 5 — Verify

- [x] 5.1 `npx tsc --noEmit` — clean on all changed files (pre-existing
  `LeafletMap.tsx` errors are a missing local `pnpm install`, unrelated).
- [x] 5.2 `npx eslint <changed files>` — clean.
- [x] 5.3 Targeted vitest runs green (venue, backup, tournament, panel,
  core/utils, core/components context + hook + overlay suites).
- [x] 5.4 Full `npx vitest run` — see `verify-report.md` for the one
  pre-existing environmental flake.
