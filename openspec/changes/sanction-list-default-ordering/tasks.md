# Tasks: Sanctions List Defaults to Newest-First by Issue Date

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~10 authored source + ~120 test |
| 400-line budget risk | None |
| Chained PRs recommended | No |
| Delivery strategy | single PR |

## Phase 1: Backend RED (strict TDD) — `API.Tests`

- [x] 1.1 RED `PlayerSanctionOrderingTests.cs`: `new GetPlayerSanctionsFilteredRequest()` has `OrderBy == "IssuedDate"` and `Order == SortOrder.Descending` (DTO carries the newest-first default).
- [x] 1.2 RED `PlayerSanctionOrderingTests.cs`: seed three sanctions issued 2021-06-01 / 2023-03-15 / 2022-09-20 in that insertion order, tagged with one unique token; `GetPlayerSanctionsAsync` with `Description = token` and no explicit sort returns them ordered 2023 → 2022 → 2021, each `IssuedDate <=` the previous (Service returns sanctions newest-first).
- [x] 1.3 RED `PlayerSanctionOrderingTests.cs`: same seeded set with `OrderBy = "IssuedDate"`, `Order = SortOrder.Ascending` returns oldest-first (Explicit sort still wins).

## Phase 2: Backend GREEN — `Application/DTOs/PlayerSanction/Request/GetPlayerSanctionsFilteredRequest.cs`

- [x] 2.1 Add a parameterless constructor setting `OrderBy = nameof(PlayerSanction.IssuedDate)` and `Order = SortOrder.Descending`; document why (the list has no server-side column sort, so this is the effective order). Add the `Domain.Entities.Models` using.
- [x] 2.2 Verify 1.1–1.3 green.
- [x] 2.3 Regression: `dotnet test Club12-Backend/Solution/Club12.sln --filter PlayerSanction` — `PlayerSanctionSearchTests` (order-independent `Assert.Contains`) stays green.

## Phase 3: Full regression

- [x] 3.1 `dotnet test Club12-Backend/Solution/Club12.sln` green — 813 passed / 0 failed.
- [x] 3.2 `dotnet build Club12-Backend/Solution/Club12.sln` — Build succeeded, 0 warnings / 0 errors.
- [x] 3.3 Frontend untouched (zero files changed under `Club12-WebClient`); no `npm` run needed.

## Phase 4: Manual dev-DB verification (pending — owner login)

- [ ] 4.1 `/panel/sanciones`: row 1 is the most recently issued sanction; last page holds the oldest.
- [ ] 4.2 Public `/sanciones`: same newest-first order.
