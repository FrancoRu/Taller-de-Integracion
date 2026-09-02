# Design: Post-Mutation UI Feedback

## 1. Success notification ownership

**Decision:** the **view** owns the single success confirmation. A context
provider never fires its own toast on a successful mutation *unless it is the
only feedback that flow has*.

Rationale: every admin page already calls `notifySuccess({ title, text })`
from `confirmDialog.ts` after a mutation resolves truthy. A provider that
also calls `setMessage(...)` produces a second, redundant `Swal`. The dedup
guard in `ErrorProvider.setMessage` (a 2500 ms same-message window) does not
help — `notifySuccess` is a different `Swal.fire` path with different text.

Kept (context toast is the only feedback — the page has no `notifySuccess`):

| Provider / method | Why kept |
| --- | --- |
| `venue.addVenue`, `season.addSeason` | create flows; page shows nothing |
| `tournament.putTournamentById` | edit pages use only `notifyError`/`notifyWarning` |
| `tournament.enrollTeam` / `unenrollTeam` | `TournamentEnrolledTeams` defers to the toast by comment |
| `match.suspendMatch` | already documented as its only feedback |

Removed:

| Provider / method | Calling view |
| --- | --- |
| `venue.deleteVenueById` | `VenuesPage` — "¡Eliminada!" |
| `blogPost.addBlogPost` | `addBlogPostForm` (also drops the lone English string) |
| `division.putDivisionById` (200 + 204) | `divisionEditPage` |
| `division.deleteDivisionsById` | `divisionsPage` |
| `season.putSeasonById` (200 + 204) | `AdminSeasonDetailPage` |
| `season.deleteSeasonById` | `SeasonsPage` |
| `tournament.createFullTournament` | `TournamentWizardPage` (was an empty toast) |
| `tournament.addFullDivision` | `divisionCreatePage` (was an empty toast) |
| `tournament.registerTeamsByTournamentId` | `TeamRegisterPage` |

`useError`/`setMessage` becomes unused in `blogPost.context` and is removed
from its imports; it stays in the other four (still used by kept flows).

## 2. Refetch instead of optimistic mutation

**Decision:** when the server effect of a mutation cannot be reconstructed
from the request + the response body, the data layer re-reads the server.

### 2a. Venue photo

`useVenue.putVenuePhotoById` now:

```
await venueService.putVenuePhotoById(id, image);
const res = await venueService.getVenueById(id);   // service, not getVenueById()
setVenue(res.data); setVenues(upsert); setQueryData; invalidate list;
return res.data;                                    // now IVenueResponse | void
```

It calls the **service** directly, not the context's `getVenueById`, because
that method short-circuits on the stale in-memory `venues` list.
`venuePage.handleEditSubmit` uses the returned venue
(`setVenue(refreshed)`) rather than `fetchVenue()`, which would hit the same
short-circuit.

### 2b. Backups catalog

```
const refreshCatalog = async () => {
  try { setBackups((await backupService.getBackups()).data); } catch { /* keep */ }
};
```

`createBackup` and `restoreBackup` call `refreshCatalog()` after a successful
service call instead of `setBackups(prev => [response.data, ...prev])`.
`fetchBackups` is refactored to wrap `refreshCatalog()` with the `loading`
flag (same observable behavior). `deleteBackup` is unchanged — its
optimistic `filter` is exact.

Because `createBackup`/`restoreBackup` now `await` the refetch internally,
the caller's blocking message (see §3) stays up until the fresh list has
loaded — the user never sees a stale table between "done" and "reloaded".

## 3. One blocking overlay

### Store: `modules/core/utils/requestActivity.ts`

Adds a **LIFO stack** of `{ id, text }` alongside the existing request
counter:

| Export | Behavior |
| --- | --- |
| `setBlockingMessage(text): number` | push, return id, notify listeners |
| `clearBlockingMessage(id): void` | remove by id (tolerates out-of-order), notify |
| `getBlockingMessage(): string \| null` | newest entry's text, or `null` |
| `runWithBlockingMessage(msg, fn)` | `setBlockingMessage` → `await fn()` → `clearBlockingMessage` in `finally` |

The `Listener` signature is unchanged (`(activeCount: number) => void`), so
existing subscribers and tests keep working; `notify()` is also called on
message changes (the count argument is carried along, unused by the message
consumer).

**Why a stack, not a single string:** overlapping operations (rare here, but
possible) must not have an earlier `clear` wipe a later message, and a later
`clear` must not resurrect an earlier one. Newest-wins with id-keyed removal
is the minimal correct semantics.

### Component: `views/core/components/GlobalLoadingOverlay.tsx`

```
const activeCount = useSyncExternalStore(sub, getActiveRequestCount, getActiveRequestCount);
const message     = useSyncExternalStore(sub, getBlockingMessage,    getBlockingMessage);
return <BlockingOverlay open={activeCount > 0 || message !== null} message={message ?? undefined} />;
```

Two `useSyncExternalStore` calls returning **primitives** — never a
`{ count, message }` object, which would fail referential-equality and loop.
`BlockingOverlay` already accepts `message`; no change there. It now has
exactly one consumer.

### Pages

| Page | Before | After |
| --- | --- | --- |
| `DataAdministrationPage` | `activeOperation` state + a `<Dialog>` + `<CircularProgress>` (~40 lines) | 4 handlers wrap work in `runWithBlockingMessage`; `isWiping` kept for the button |
| `TournamentPage` | local `<BlockingOverlay open={reverting} message=…>` | `runWithBlockingMessage` around the revert; `reverting` kept for the button |
| `TournamentDivisionAssignment` | local `<BlockingOverlay open={starting} message=…>` | `setBlockingMessage`/`clearBlockingMessage` in `handleStart`'s `try/finally` (it has early returns); `starting` state deleted (unused — the button uses `busy`) |

`DataAdministrationPage`'s "close the overlay before `notify*`" comment and
workaround are removed: `runWithBlockingMessage` clears the message before
the `await` returns, and SweetAlert is already lifted above the overlay.

## Sequence — venue photo edit (after)

```
user picks file, clicks Guardar
  venuePage.handleEditSubmit
    putVenueById(id, payload)            -> PUT  (beginRequest/endRequest)
      context: setVenue/setVenues, NO setMessage
    putVenuePhotoById(id, file)
      POST /venues/{id}/photo            -> (beginRequest/endRequest)
      GET  /venues/{id}                  -> fresh photoUrl
      context: setVenue(fresh), return fresh
    setVenue(refreshed)                  -> <img src> = new URL, no reload
    setEditDialogOpen(false)
    notifySuccess("Cancha actualizada")  -> ONE modal
```

## Sequence — restore backup (after)

```
BackupsTable row -> onRestore
  DataAdministrationPage.handleRestoreBackup
    runWithBlockingMessage("Restaurando la base de datos…", () =>
        useBackups.restoreBackup(id))
      setBlockingMessage(...)            -> overlay shows spinner + text
      POST /backups/{id}/restore         -> safety backup taken, schema replayed
      refreshCatalog(): GET /backups     -> the real post-restore catalog
      setBackups(fresh)
      clearBlockingMessage(...)          -> overlay closes
    notifySuccess("Base de datos restaurada")
```
