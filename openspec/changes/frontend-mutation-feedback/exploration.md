# Exploration: Post-Mutation UI Feedback

## Trigger

Reported from live use of the admin panel, in this order:

1. "Editar cancha": uploading a new photo shows **two** confirmation modals
   with the same text, and the image only updates after a manual reload.
2. "Which other flows have the double modal?"
3. "Does the Administración de datos table reload after generate / restore?"
4. "While the operation blocks the screen, two spinners appear — one white
   (higher z-index), one orange. Which is which?"

## Findings

### 1. Double success modal

`venuePage.handleEditSubmit` calls `putVenueById` then `notifySuccess({ title:
'Cancha actualizada' })`. Inside `VenueProvider.putVenueById`, both the 200
and 204 branches also call `setMessage(res.status, ['La información de la
cancha fue actualizada correctamente'])`. `setMessage` → `Swal.fire` (an
auto-dismiss toast). So: one toast from the context + one modal from the
page.

Grepping every provider for `setMessage(` on a success path and cross-
referencing the calling view for a `notify*` call produced nine affected
flows across `blogPost`, `division`, `season`, `tournament`, `venue`.
`MatchProvider` already had the same class of bug removed
(`match.context.test.tsx` — "MatchProvider — no duplicate success toast");
its comment ("The context used to ALSO fire a generic … toast, so the user
saw two different messages back to back for one action") is the precedent.

Two of the nine (`createFullTournament`, `addFullDivision`) fired
`setMessage(res.status, [])` — an **empty** toast, i.e. a blank `Swal`.

### 2. Stale data after mutation

**Venue photo.** `putVenuePhotoById` (context) did
`await queryClient.invalidateQueries(...)`, but nothing consumes those keys
via `useQuery` — `venuePage` calls `getVenueById`, which returns the first
match from the in-memory `venues` array without hitting the network. The
backend (`SupabaseHelper.GenerateNameFile` = `Guid.NewGuid()`) gives every
upload a new URL, so the stored `photoUrl` genuinely changes — the frontend
just never learns the new value until a reload clears `venues`.

**Backups catalog.** `useBackups` optimistically did
`setBackups(prev => [response.data, ...prev])` for create and restore.
- Create: `BackupOperationsService.CreateBackupAsync` →
  `CreateBackupCoreAsync(applyRetention: true)`. Once `RetentionCount` is
  exceeded the oldest catalogued backup is deleted server-side; the
  optimistic prepend still shows it.
- Restore: `PgDumpBackupService` dumps `--clean --if-exists` over schemas
  `public` + `Club12`; `PsqlDatabaseRestoreService` replays it with
  `psql -f`. `BackupRecords` lives in `Club12`
  (`20260820005357_AddBackupRecordTable`), so a restore drops and recreates
  it — the catalog reverts to the restored snapshot's rows. The endpoint
  returns the pre-restore **safety backup** record, whose row was written to
  the pre-restore DB and is gone after the replay. Prepending it shows a
  backup that does not exist.
`DataAdministrationPage` never re-calls `fetchBackups` after these
operations, so the table is wrong until a manual reload.

### 3. Two blocking spinners

- **White, on top:** `BlockingOverlay` rendered by `GlobalLoadingOverlay`
  (mounted once in `App.tsx`). A MUI `<Backdrop>` with
  `<CircularProgress color="inherit" />` inheriting `color: '#fff'` and
  `zIndex: theme.zIndex.modal + 1` (= 1301). Fires automatically for every
  non-GET request: `axiosUtils.sendRequest` → `beginRequest()`
  (`isMutation = method !== 'GET'`). No text.
- **Orange, below:** `DataAdministrationPage`'s own `<Dialog>` (z-index
  1300) driven by an `activeOperation` string. `<CircularProgress />` with
  no `color` prop → `primary` = the theme's orange. This one carries the
  operation text.

`TournamentPage` and `TournamentDivisionAssignment` have the same duplication
(their local `<BlockingOverlay>` is also white, so it is less noticeable —
two white spinners overlap almost exactly).

The `DataAdministrationPage` `<Dialog>` predates `GlobalLoadingOverlay`; its
own comment still explains a z-index workaround for SweetAlert that the
global overlay's design (message layer, Swal lifted to z-index 2000 by
`confirmDialog.liftAboveMuiModals`) makes unnecessary.

## Options Considered — blocking overlay

| Option | Blast radius | Result |
| --- | --- | --- |
| Bump `DataAdministrationPage`'s `<Dialog>` z-index above the global overlay | 1 line, 1 file | Orange + text covers the white one; the white one still exists behind it |
| **Unify: delete the local overlays, add a message channel to `GlobalLoadingOverlay`** | ~6 files | One spinner, one code path, consistent across pages |

The requester chose the unification.
