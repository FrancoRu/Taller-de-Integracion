# Proposal: Club History — Seasons Newest-First and a Slug URL

**Touches**: Backend (`ClubSeasonResponse` + `ClubService`) and frontend
(`ClubHistoryPage`, `club.d.ts`). No schema, no migration.

## Intent

The "Ver historial del club" button on the team page (`TeamPage.tsx`) opens
`ClubHistoryPage` with two problems:

1. **Unordered history table.** The table lists one row per (team, season)
   pair in the order the API returns them. `ClubService.GetClubHistoryAsync`
   builds each team's `Seasons` list straight from the
   `TeamTournamentRegistration` rows with no ordering, and `ClubSeasonResponse`
   carries only `TournamentId` + `TournamentName` — there is no date to sort
   by. The organizer expects the most recent season first.

2. **UUID in the URL.** The button navigates with `team.clubId` (a GUID),
   so the address bar shows `/panel/clubes/<uuid>`. `ClubHistoryPage` and
   `GET /api/clubs/{idOrSlug}` already accept a slug, and the loaded club
   response already carries `slug` — the page just never switches to it.

## Scope

### In Scope

- `ClubSeasonResponse` gains `StartDate` (from `Tournament.StartDate`, a
  required field).
- `ClubService.GetClubHistoryAsync` resolves each season's `StartDate` and
  orders every team's `Seasons` by it, descending.
- `ClubHistoryPage` builds every (team, season) row with its `startDate`,
  sorts the full row list descending, and — once the club is loaded — if the
  route param was not the club slug, replaces the URL with
  `/panel/clubes/<slug>`.
- `IClubSeasonResponse` gains `startDate`.
- Backend + frontend tests.

### Out of Scope (Non-Goals)

- Adding `ClubSlug` to `TeamResponse` / including `Team.Club` in the team
  fetch. The URL is canonicalised on the history page instead, which needs no
  backend change and no new include on a hot path. The button keeps
  navigating with the id it already has.
- Displaying the date in the table (the "Temporada" column keeps showing the
  tournament name; `StartDate` is only a sort key).
- Ordering the `Teams` list itself independently of their seasons — the row
  list is flattened and sorted as one.
- Any change to `/panel/temporadas` — that is the separate
  `season-list-year-ordering` change (shipped in the same PR).

## Capabilities

### New Capabilities

- `club-history-view`: the order of the club history table and the canonical
  URL of `ClubHistoryPage`.

### Modified Capabilities

- None.

## Approach

Add the one missing sort key (`Tournament.StartDate`) to the club-history
projection and order by it on both ends. Fix the URL entirely on the client:
`ClubHistoryPage` already fetches the club (slug included), so a
`navigate(..., { replace: true })` to the slug URL after load is the smallest
change that removes the UUID, with no backend surface touched.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Application/DTOs/Club/Response/ClubHistoryResponse.cs` | Modified | `ClubSeasonResponse.StartDate` added |
| `Application/Services/ClubService.cs` (`GetClubHistoryAsync`) | Modified | Resolve `StartDate`; order `Seasons` desc |
| `Club12-WebClient/src/modules/club/type/club.d.ts` | Modified | `IClubSeasonResponse.startDate` |
| `Club12-WebClient/src/views/club/ClubHistoryPage.tsx` | Modified | Sort rows desc; canonicalise URL to slug |
| `API.Tests/ClubTests.cs` | Modified | Season ordering + `StartDate` present |
| `Club12-WebClient/src/views/club/ClubHistoryPage.test.tsx` | Modified | Row order + GUID→slug redirect |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| `ClubHistoryResponse` is AutoMapper-validated | Low | It is hand-built in the service, not mapped; `AutoMapperProfilesTests` unaffected |
| A team row with no season has no `StartDate` | Expected | Placeholder rows sort last (empty string / `default` date) |
| Redirect loop if slug lookup returns a different slug | Very low | Guard is `param !== club.slug`; the club response slug is canonical and stable |

## Rollback Plan

Revert the commit. `StartDate` is additive on the DTO; no data or schema
state. The table returns to unordered and the URL to the UUID.

## Success Criteria

- [ ] `GetClubHistoryAsync` returns each team's `Seasons` ordered by
      `StartDate` descending, and every entry carries a non-default
      `StartDate` when its tournament exists.
- [ ] `ClubHistoryPage` shows the most recent season in the first row.
- [ ] Opening `/panel/clubes/<uuid>` replaces the URL with
      `/panel/clubes/<slug>` after the club loads.
- [ ] Backend + frontend suites green; `dotnet build` 0 warnings.
