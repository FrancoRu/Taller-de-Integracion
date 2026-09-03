# Tasks: Seasons List Ordered by Year, Newest First

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~6 authored source + ~90 test |
| 400-line budget risk | None |
| Chained PRs recommended | No |
| Delivery strategy | single PR |

## Phase 1: Backend RED (strict TDD) — `API.Tests`

- [x] 1.1 RED `SeasonListOrderingTests.cs`: create seasons with years 2024, 2026, 2025 (in that order) tagged with a unique name token; `GetAllSeasonsAsync` returns them ordered 2026 → 2025 → 2024, each `Year <=` the previous (Newest year first).
- [x] 1.2 RED `SeasonListOrderingTests.cs`: a `Year = 2025` season and a `Year = null` season (shared token) — the 2025 one comes first (Null-year seasons sort last).
- [x] 1.3 RED `SeasonListOrderingTests.cs`: two `Year = 2026` seasons named "…B" and "…A" — the "…A" one comes first (Same year broken by name).

## Phase 2: Backend GREEN — `Application/Services/SeasonService.cs`

- [x] 2.1 In `GetAllSeasonsAsync`, order the fetched list: `OrderByDescending(s => s.Year.HasValue).ThenByDescending(s => s.Year).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)`. Document why in-memory (small unpaginated list; a `PaginatedFilterRequest` would truncate).
- [x] 2.2 Verify 1.1–1.3 green.

## Phase 3: Full regression

- [x] 3.1 `dotnet test Club12-Backend/Solution/Club12.sln` — 812 passed / 0 failed.
- [x] 3.2 `dotnet build Club12-Backend/Solution/Club12.sln` — Build succeeded, 0 warnings / 0 errors.
- [x] 3.3 Frontend untouched.

## Phase 4: Manual dev-DB verification

- [ ] 4.1 `/panel/temporadas`: the most recent season is in row 1; null-year seasons (if any) at the bottom.
