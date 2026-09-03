# Proposal: Seasons List Ordered by Year, Newest First

**Touches**: Backend only (`Club12-Backend`, `SeasonService` + tests). No frontend, no API surface, no schema, no migration.

## Intent

`/panel/temporadas` (and the public `/temporadas`) show the seasons in an
unpredictable order. Both call `GET /api/seasons`, which returns a plain,
non-paginated array. `SeasonService.GetAllSeasonsAsync` runs
`FindAsync(season => true, includes: [...])` with **no ordering** — and
because no `PaginatedFilterRequest` is passed, `GenericRepository.FindAsync`
never applies `SortBy` either. Postgres then returns rows in physical/heap
order, so a newly created season can appear anywhere in the list.

The organizer works with the current season, so the list should lead with the
most recent one.

## Scope

### In Scope

- `SeasonService.GetAllSeasonsAsync` orders its result by `Season.Year`
  **descending** (most recent year first), seasons with no year last, then by
  `Name` as a stable tiebreaker.
- Backend tests for the ordering, including the null-year placement.

### Out of Scope (Non-Goals)

- Any frontend change. `SeasonsPage` and `PublicSeasonsPage` render the array
  in the order the endpoint returns it (client-side pagination, no default
  sort model), so the backend order is the effective order.
- Server-side pagination / a filtered seasons endpoint. The list stays a
  plain array.
- Ordering the tournaments *inside* a season (`AdminSeasonDetailPage`,
  `PublicSeasonPage`) — separate concern, separate endpoint.
- Adding a secondary user-facing sort control to the DataGrid.

## Capabilities

### New Capabilities

- `season-list-ordering`: the order of the season list returned by
  `GET /api/seasons`.

### Modified Capabilities

- None.

## Approach

Sort in `SeasonService.GetAllSeasonsAsync` after the fetch. The list is small
(a handful of seasons per league) and unpaginated, so an in-memory
`OrderByDescending(...).ThenBy(...)` is the simplest correct fix — no EF/
repository signature change, no `PaginatedFilterRequest` (which would also
truncate the list to the default page size).

`Season.Year` is `int?`; a null year sorts last, and `Name` breaks ties so
two seasons of the same year keep a deterministic order across requests.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Application/Services/SeasonService.cs` (`GetAllSeasonsAsync`) | Modified | Order by `Year` desc, nulls last, then `Name` |
| `API.Tests/SeasonListOrderingTests.cs` | New | Year-desc ordering + null-year placement |

Only `GET /api/seasons` (`SeasonController.GetAllSeasons`) consumes
`GetAllSeasonsAsync`. No existing test asserts season list order.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| A test relies on the old arbitrary order | Low | Verified: the only test hitting `GetAllSeasons` asserts 200 only |
| Two seasons share a year and a name → order still non-deterministic | Very low | Names are effectively unique; `EntityBase` id could be a final tiebreak if it ever matters |
| Callers expected insertion order | Low | Insertion order was never guaranteed (no `OrderBy`, heap order) |

## Rollback Plan

Revert the single `SeasonService` commit. No data, schema, or migration
involved.

## Success Criteria

- [ ] `GetAllSeasonsAsync` returns seasons with a year in strictly
      non-increasing `Year` order.
- [ ] Seasons with a null `Year` come after every season that has one.
- [ ] `/panel/temporadas` shows the most recent season in row 1.
- [ ] Backend suite green; `dotnet build` 0 warnings.
