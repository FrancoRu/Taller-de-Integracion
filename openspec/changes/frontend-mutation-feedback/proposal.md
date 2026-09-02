# Proposal: Consistent Post-Mutation UI Feedback (Toasts, Catalog Freshness, Blocking Overlay)

**Touches**: Frontend only (`Club12-WebClient`). No backend, no API, no schema.

## Intent

Three unrelated-looking bugs reported from the admin panel share one root
cause: **after a mutation, the UI does not converge on a single, truthful
representation of the new state.**

1. **Double confirmation modal.** Editing a venue photo (and eight other
   flows) showed two dialogs with the same message: the calling page's
   `notifySuccess`, plus a second toast the context provider fired on its own
   from `setMessage(...)`. `MatchProvider` already had this exact bug fixed
   (see `2026-08-15-*` work / `match.context.test.tsx` — "no duplicate
   success toast"); the other providers were never brought in line.

2. **Stale data after a mutation whose server effect diverges from the
   optimistic guess.**
   - The venue photo only appeared after a full page reload: the photo
     endpoint returns no body and every upload lands at a fresh unique URL,
     but `getVenueById` short-circuits on the in-memory `venues` list, which
     still holds the pre-upload URL.
   - The "Administración de datos" backups table did not reflect a generate
     (server-side retention pruning removes the oldest) or a restore (a
     full-schema dump replay reverts the whole `BackupRecords` table to the
     restored snapshot's state, and the returned pre-restore safety-backup
     row no longer exists).

3. **Two stacked blocking spinners.** `DataAdministrationPage`,
   `TournamentPage` and `TournamentDivisionAssignment` each render their own
   full-screen blocking overlay while a long operation runs — on top of the
   app-wide `GlobalLoadingOverlay`, which already fires for every mutating
   request via the `axiosUtils` choke point. The user saw a white spinner
   (global, `zIndex: theme.zIndex.modal + 1`) covering an orange one
   (`DataAdministrationPage`'s own `<Dialog>` + `<CircularProgress>`).

## Scope

### In Scope

- **No self-notification on success in providers.** `blogPost`, `division`,
  `season`, `tournament` and `venue` contexts stop calling `setMessage(...)`
  on a successful mutation for the flows whose calling page already shows a
  `notifySuccess`. The page owns the single confirmation.
- **Refetch, not optimistic mutation, where server state diverges.**
  - `useVenue.putVenuePhotoById` re-fetches the venue from the service
    (bypassing the `getVenueById` in-memory short-circuit) and returns it;
    `venuePage` uses the returned venue.
  - `useBackups.createBackup` and `useBackups.restoreBackup` call a shared
    `refreshCatalog()` (`GET /backups` + `setBackups`) instead of prepending.
- **One blocking overlay.** `requestActivity` gains a LIFO stack of
  contextual messages (`setBlockingMessage` / `clearBlockingMessage` /
  `getBlockingMessage`, newest wins) and a `runWithBlockingMessage(msg, fn)`
  helper. `GlobalLoadingOverlay` renders the message. The three pages delete
  their local `<BlockingOverlay>` / `<Dialog>` and set a contextual message.

### Out of Scope (Non-Goals)

- Any backend change. The backup/restore backend semantics
  (`BackupOperationsService`, `PgDumpBackupService`,
  `PsqlDatabaseRestoreService`) are unchanged and correct.
- The `useBackups.deleteBackup` optimistic `filter` — a delete removes
  exactly one row with no server-side side effect, so it stays optimistic.
- Provider success toasts that are the **only** feedback for a flow (venue
  create, season create, `putTournamentById`, `enrollTeam`/`unenrollTeam`,
  `suspendMatch`) — those keep their `setMessage`.
- The date-picker locale request (`dd/mm/yyyy`) discussed in the same
  session — a native `<input type="date">` renders per browser locale and
  cannot be forced from the page; declined by the requester.
- The production 502 incident from the `CleanupOrphanedTournaments`
  migration — a backend fix tracked on its own branch / PR (`fix/cleanup-
  orphaned-tournaments-teams-fk`).

## Capabilities

### New Capabilities

- `frontend-mutation-feedback`: how the web client presents the result of a
  mutation — exactly one success confirmation (owned by the view), a list/
  detail view that reflects the real post-mutation server state, and a
  single app-wide blocking overlay with an optional per-operation message.

### Modified Capabilities

- None. (`frontend-http-error-pipeline` and `frontend-query-key-factory`
  are adjacent but their requirements are untouched.)

## Approach

Pull the success-notification responsibility down to one owner per flow (the
view), and make the data layer re-read the server whenever its optimistic
guess cannot be trusted. Collapse three ad-hoc blocking overlays into the
existing global one, extended with a message channel driven from the view.

## Rollback Plan

Pure frontend, no migration, no persisted state. Revert the four commits on
the branch. Each is independently revertible:

- `fix(web): drop duplicate context success toasts across five providers`
- `fix(web): stop the venue photo edit showing two modals and not refreshing`
- `fix(web): refetch the backup catalog after create and restore`
- `refactor(web): unify blocking overlays behind GlobalLoadingOverlay`

The worst regression a bad revert reintroduces is a redundant toast or a
stale list until reload — no data loss, no broken flow.
