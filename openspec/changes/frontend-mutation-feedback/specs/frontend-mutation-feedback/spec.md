# Frontend Mutation Feedback Specification

## Purpose

Define how `Club12-WebClient` presents the outcome of a mutation (create,
update, delete, restore, long domain operation): exactly one success
confirmation, a view that reflects the real post-mutation server state, and
a single app-wide blocking overlay that can carry a per-operation message.

Scope is limited to behavior observable in Vitest + Testing Library with
mocked services / a mocked `Swal`. Visual styling (spinner color, z-index
constants) is not specified beyond "one overlay, on top of every MUI modal
layer".

## Requirements

### Requirement: Single Success Confirmation Per Mutation

A successful mutation MUST surface at most one success dialog/toast to the
user. When a view calls `notifySuccess` (or another `notify*` from
`confirmDialog`) after a mutation resolves, the context provider backing that
mutation MUST NOT also call `setMessage(...)` on the success path.

A provider method MAY keep its own success `setMessage(...)` only when no
calling view shows its own confirmation for that flow.

#### Scenario: Context stays silent when the page confirms

- GIVEN `VenueProvider.deleteVenueById` is called and the service resolves
  successfully
- WHEN the deletion completes
- THEN `Swal.fire` is NOT called from within the provider
- AND the calling view (`VenuesPage`) is the sole source of the
  "¡Eliminada!" confirmation

#### Scenario: Empty toast is never fired

- GIVEN `TournamentProvider.createFullTournament` or `addFullDivision`
  resolves successfully
- WHEN the mutation completes
- THEN the provider does NOT call `setMessage(res.status, [])` (an
  empty-title `Swal`)

#### Scenario: Sole-feedback toast is preserved

- GIVEN `MatchProvider.suspendMatch` resolves successfully and
  `StageMatchesByRound` shows no `notifySuccess` for it
- WHEN the mutation completes
- THEN the provider still fires exactly one toast
  ("Partido reprogramado correctamente")

### Requirement: View Reflects Real Post-Mutation Server State

When a mutation's effect on server state cannot be reconstructed from the
request plus the response body, the data layer MUST re-read the affected
resource from the server rather than optimistically mutating its in-memory
copy. The view MUST show the re-read state without requiring a full page
reload.

#### Scenario: Venue photo appears without a reload

- GIVEN a venue is displayed with photo URL `A`
- WHEN the admin uploads a new photo and the save completes
- THEN `useVenue.putVenuePhotoById` issues a `GET` for the venue through the
  service (not through the in-memory-cached `getVenueById` path)
- AND it returns the venue carrying the new photo URL `B`
- AND `venuePage` renders `<img src="B">` immediately, with no reload

#### Scenario: Backups table reflects retention pruning after a generate

- GIVEN the backups list is at the retention limit
- WHEN the admin generates a new backup and it completes
- THEN `useBackups.createBackup` calls `backupService.getBackups()` and
  replaces the list with the response
- AND the pruned oldest backup is no longer shown

#### Scenario: Backups table reflects the restored snapshot after a restore

- GIVEN the admin restores from a catalogued backup and it completes
- WHEN `useBackups.restoreBackup` resolves
- THEN it calls `backupService.getBackups()` and replaces the list with the
  response
- AND the list is NOT `[<returned safety-backup row>, ...<pre-restore list>]`

#### Scenario: Delete stays optimistic

- GIVEN the admin deletes one catalogued backup and it completes
- WHEN `useBackups.deleteBackup` resolves
- THEN the list is updated by removing that one row (`filter`), with no
  additional `GET`

### Requirement: Single Blocking Overlay With Optional Contextual Message

The web client MUST render exactly one full-screen blocking overlay. It MUST
be visible whenever a mutating request is in flight (any non-GET through
`axiosUtils`) OR a contextual blocking message is set. A screen MUST NOT
render its own second blocking overlay for a long operation; it MUST instead
publish a message to the shared store.

#### Scenario: Overlay shows for a mutating request with no message

- GIVEN no contextual message is set
- WHEN `beginRequest()` is called (a POST/PUT/DELETE starts)
- THEN `GlobalLoadingOverlay` shows its spinner
- AND it hides once the matching `endRequest()` brings the count to zero

#### Scenario: Overlay shows a contextual message with no request in flight

- GIVEN the request count is zero
- WHEN `setBlockingMessage("Restaurando la base de datos…")` is called
- THEN `GlobalLoadingOverlay` shows the spinner AND that text
- AND it hides when the message is cleared with its id

#### Scenario: Newest message wins, out-of-order clear is tolerated

- GIVEN `setBlockingMessage("A")` then `setBlockingMessage("B")`
- WHEN `getBlockingMessage()` is read
- THEN it returns `"B"`
- AND clearing `"A"` first still leaves `getBlockingMessage()` as `"B"`
- AND clearing `"B"` then returns `null`

#### Scenario: runWithBlockingMessage always clears

- GIVEN `runWithBlockingMessage(msg, fn)` is invoked
- WHEN `fn` throws
- THEN the rejection propagates to the caller
- AND `getBlockingMessage()` returns to `null` (the message is cleared in a
  `finally`)

### Requirement: Blocking Message Store Does Not Break Existing Subscribers

Adding the message channel to `requestActivity` MUST NOT change the
`subscribeToRequestActivity` listener contract. Listeners MUST continue to
receive the running request count, and MUST be notified on message changes
as well.

#### Scenario: Count notifications unchanged

- GIVEN a listener subscribed via `subscribeToRequestActivity`
- WHEN `beginRequest()` is called twice then `endRequest()` twice
- THEN the listener is invoked with `1, 2, 1, 0` in order

#### Scenario: Message changes notify

- GIVEN a subscribed listener
- WHEN a message is set and then cleared
- THEN the listener is invoked once per change
