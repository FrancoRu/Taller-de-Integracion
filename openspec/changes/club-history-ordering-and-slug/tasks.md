# Tasks: Club History — Seasons Newest-First and a Slug URL

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~40 authored source + ~120 test, across backend + frontend |
| 400-line budget risk | None |
| Chained PRs recommended | No — ships with `season-list-year-ordering` in one PR |
| Delivery strategy | single PR |

## Phase 1: Backend RED (strict TDD) — `API.Tests/ClubTests.cs`

- [x] 1.1 Extended `SeedTournamentAsync` with an optional `DateTime? startDate`.
- [x] 1.2 RED `GetClubHistory_OrdersSeasonsByStartDateDescending`: one team registered in a 2025-03-01 and a 2027-03-01 tournament; `Seasons` must come back 2027 → 2025 with matching `StartDate`. (Failed to compile — `StartDate` absent — then value-failed as expected.)

## Phase 2: Backend GREEN

- [x] 2.1 `ClubHistoryResponse.cs`: `ClubSeasonResponse.StartDate` added.
- [x] 2.2 `ClubService.GetClubHistoryAsync`: `Dictionary<Guid, Tournament>` lookup; `StartDate = tournament?.StartDate ?? DateTime.MinValue`; `OrderByDescending(season => season.StartDate)` per team.
- [x] 2.3 6 `ClubTests` green; `dotnet build` 0 warnings.

## Phase 3: Frontend RED — `ClubHistoryPage.test.tsx`

- [x] 3.1 `startDate` added to the `CLUB` fixture seasons (2026 team then 2027 team).
- [x] 3.2 RED: first `<tbody>` row is the 2027 season.
- [x] 3.3 RED: `/panel/clubes/<CLUB_ID>` ends at `/panel/clubes/colon` via a `LocationProbe`; `/panel/clubes/colon` triggers no navigation.

## Phase 4: Frontend GREEN — `ClubHistoryPage.tsx` + `club.d.ts`

- [x] 4.1 `club.d.ts`: `IClubSeasonResponse.startDate: string`.
- [x] 4.2 `ClubHistoryPage`: `ClubSeasonRow.startDate`; `''` for the placeholder; `unsorted.sort((a, b) => b.startDate.localeCompare(a.startDate))`.
- [x] 4.3 `ClubHistoryPage`: `useEffect` — after `club` loads, `navigate(APP_ROUTES.panelClub.build(club.slug), { replace: true })` when `idOrSlug !== club.slug`.
- [x] 4.4 6 `ClubHistoryPage` tests green.

## Phase 5: Full regression

- [x] 5.1 `dotnet test` — 813 passed / 0 failed.
- [x] 5.2 `dotnet build` — 0 warnings / 0 errors.
- [x] 5.3 `npx tsc --noEmit` exit 0; `npm run lint` exit 0; ClubHistoryPage suite green. Full `vitest run` had 1 unrelated `VenuesPage` flake under parallel load — passes in isolation (2/2), no venue files touched.

## Phase 6: Manual dev-DB verification (pending — owner login)

- [ ] 6.1 Team page → "Ver historial del club": URL shows the slug; table lists the most recent season first.
