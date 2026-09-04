# Tasks: Blog / Novedades List Defaults to Newest-First by Creation Date

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~10 authored source + ~110 test |
| 400-line budget risk | None |
| Chained PRs recommended | No |
| Delivery strategy | single PR |

## Phase 1: Backend RED (strict TDD) — `API.Tests/BlogPostOrderingTests.cs`

- [x] 1.1 RED: `new GetBlogPostsFilteredRequest()` — expected `Descending`, got `Ascending`.
- [x] 1.2 RED: three posts dated 2026-01-10 / 03-01 / 02-05 inserted out of order → expected [03-01, 02-05, 01-10], got insertion order.
- [x] 1.3 Explicit ascending override — passed at RED (works regardless).
- [x] 1.4 Draft-hidden regression — passed at RED.

## Phase 2: Backend GREEN — `GetBlogPostsFilteredRequest.cs`

- [x] 2.1 Parameterless constructor: `OrderBy = "DateCreated"`, `Order = SortOrder.Descending`, documented.
- [x] 2.2 4/4 new tests green.
- [x] 2.3 15 `BlogPost` tests green (draft-visibility / slug / view-counter unaffected).

## Phase 3: Full regression

- [x] 3.1 `dotnet test` — 839 passed / 0 failed.
- [x] 3.2 `dotnet build` — Build succeeded, 0 warnings / 0 errors.
- [x] 3.3 Frontend untouched.

## Phase 4: Manual dev-DB verification (pending — owner)

- [ ] 4.1 `/` (logged out) "Últimas noticias": the most recent post is first.
- [ ] 4.2 `/blog`: newest-first; last page holds the oldest.
