# Sanction List Ordering Specification

## Purpose

Define the default ordering of the paginated sanctions list served by
`GET /api/player-sanctions/find` (consumed by the admin `/panel/sanciones`
page and the public `/sanciones` page) when the caller supplies no explicit
sort parameter.

Scope is limited to behavior observable through
`PlayerSanctionService.GetPlayerSanctionsAsync` and the `find` endpoint with
xUnit + the SQLite in-memory harness. Client-side rendering and column-sort
interaction are out of scope.

## Requirements

### Requirement: Newest-First Default Ordering by Issue Date

The paginated sanctions list MUST default to ordering by
`PlayerSanction.IssuedDate` in descending order (most recently issued first)
when the request carries no explicit `orderBy`. The first page MUST therefore
contain the most recently issued sanctions, not the oldest.

A caller that supplies an explicit `orderBy` / `order` MUST still override
this default.

#### Scenario: DTO carries the newest-first default

- GIVEN a freshly constructed `GetPlayerSanctionsFilteredRequest` with no
  properties assigned
- WHEN its `OrderBy` and `Order` are read
- THEN `OrderBy` equals `"IssuedDate"`
- AND `Order` equals `SortOrder.Descending`

#### Scenario: Service returns sanctions newest-first

- GIVEN three sanctions issued on 2021-06-01, 2023-03-15 and 2022-09-20,
  inserted in that (non-chronological) order
- WHEN `GetPlayerSanctionsAsync` is called with a filter that selects only
  those three and no explicit sort
- THEN the returned items appear in the order 2023-03-15, 2022-09-20,
  2021-06-01
- AND each item's `IssuedDate` is less than or equal to the previous item's

#### Scenario: Explicit sort still wins

- GIVEN a `GetPlayerSanctionsFilteredRequest` with `OrderBy = "IssuedDate"`
  and `Order = SortOrder.Ascending`
- WHEN `GetPlayerSanctionsAsync` is called
- THEN the returned items appear oldest-first

### Requirement: Existing Filters Unaffected by the Ordering Default

Changing the default ordering MUST NOT change which sanctions match a
filter. The `Description` free-text search (matching the sanction reason or
the sanctioned player's name) MUST return the same set as before.

#### Scenario: Description search regression

- GIVEN a sanction whose reason contains a unique token
- WHEN `GetPlayerSanctionsAsync` is called with `Description` set to that
  token
- THEN the result contains that sanction
