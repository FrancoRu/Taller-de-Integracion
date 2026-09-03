# Proposal: Sanctions List Defaults to Newest-First by Issue Date

**Touches**: Backend only (`Club12-Backend`, one Application DTO + tests). No frontend, no API surface, no schema, no migration.

## Intent

The admin sanctions table (`/panel/sanciones`) and the public sanctions page
(`/sanciones`) both read `GET /api/player-sanctions/find` with
`paginationMode="server"` and send **no sort parameter**. Neither page wires
the MUI DataGrid column sort to the server. So the row order is decided
entirely by the backend default.

`GetPlayerSanctionsFilteredRequest` inherits `PaginatedFilterRequest`'s
defaults — `OrderBy = "DateCreated"`, `Order = Ascending` — so the list comes
back **oldest first**. The newest sanctions land on the last page; the first
10 rows are the 10 oldest. The organizer expects the 10 most recent sanctions
first.

## Scope

### In Scope

- `GetPlayerSanctionsFilteredRequest` gets a constructor that sets the
  default ordering to `OrderBy = IssuedDate`, `Order = Descending`.
- Backend tests: the DTO default is `IssuedDate` / `Descending`; the service
  returns sanctions ordered by `IssuedDate` descending when no sort is
  supplied; the existing `Description` free-text search still matches
  (regression).

### Out of Scope (Non-Goals)

- Any frontend change. Both pages already rely on the backend order and need
  no edit.
- Wiring server-side column sort to the DataGrid (`onSortModelChange`).
- A stable secondary tiebreaker for sanctions sharing the same `IssuedDate`.
  `QueryableExtensions.SortBy` applies a single `OrderBy`/`OrderByDescending`
  with no `ThenBy`; teaching it a tiebreaker would touch every paginated
  list in the app and is deliberately excluded. Same-`IssuedDate` rows may
  still reorder between pages — an accepted, pre-existing limitation.
- Changing `PaginatedFilterRequest`'s own defaults (would affect every other
  filtered list).

## Capabilities

### New Capabilities

- `sanction-list-ordering`: the default ordering of the paginated sanctions
  list served by `GET /api/player-sanctions/find` when the caller supplies no
  explicit sort.

### Modified Capabilities

- None.

## Approach

Override the two inherited ordering defaults in the sanctions filter DTO
only. `[FromQuery]` model binding keeps a property's initialised value when
the query string omits that parameter, so a caller that never sends
`orderBy` now gets `IssuedDate` descending, while a caller that does send one
still wins. The DTO feeds exactly one endpoint (`find`, consumed by the admin
and public sanctions pages), so the blast radius matches the intent.

`PlayerSanction.IssuedDate` is a plain required `DateTime` column with no
column rename, so `SortBy`'s reflection-by-name resolves it directly.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Application/DTOs/PlayerSanction/Request/GetPlayerSanctionsFilteredRequest.cs` | Modified | Constructor sets `OrderBy = nameof(PlayerSanction.IssuedDate)`, `Order = SortOrder.Descending` |
| `API.Tests/PlayerSanctionOrderingTests.cs` | New | DTO default + service ordering coverage |

`gitnexus impact` on `GetPlayerSanctionsAsync` reports MEDIUM (7 upstream =
controller + 2 test files); the method itself is untouched. The only
production consumer of the DTO is `PlayerSanctionController.GetFilteredPlayersPrivateAsync`.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| An existing test asserts the old oldest-first order | Low | Verified: `PlayerSanctionSearchTests` uses order-independent `Assert.Contains`; no other test asserts sanction order |
| Same-`IssuedDate` rows shuffle across pages | Med | Documented non-goal; `IssuedDate` is a full `DateTime`, collisions are rare outside seed data |
| A future caller expects the inherited `DateCreated` default | Low | The inherited default was never intentional for this list; documented in the DTO |

## Rollback Plan

Revert the single DTO commit. No data, schema, or persisted state involved.
The list returns to oldest-first; no flow breaks.

## Success Criteria

- [ ] `new GetPlayerSanctionsFilteredRequest()` has `OrderBy == "IssuedDate"` and `Order == SortOrder.Descending`.
- [ ] `GetPlayerSanctionsAsync` with no sort supplied returns items in strictly non-increasing `IssuedDate` order.
- [ ] `Description` free-text search regression stays green.
- [ ] Backend suite green; `dotnet build` 0 warnings.
- [ ] Manual check on the dev DB: `/panel/sanciones` shows the most recent sanction in row 1.
