# Club History View Specification

## Purpose

Define the order of the club history table and the canonical URL of
`ClubHistoryPage` (reached from the "Ver historial del club" button on the
team page).

Scope: `ClubService.GetClubHistoryAsync` under the xUnit + SQLite harness,
and `ClubHistoryPage` under Vitest + Testing Library.

## Requirements

### Requirement: Club History Seasons Ordered Newest-First

`GetClubHistoryAsync` MUST populate each `ClubSeasonResponse` with the
`StartDate` of its tournament and MUST return every team's `Seasons`
collection ordered by that `StartDate` descending (most recent season first).
A season whose tournament cannot be resolved keeps a default `StartDate` and
sorts last.

`ClubHistoryPage` MUST render the (team, season) rows across all teams as a
single list ordered by `StartDate` descending; rows with no season sort last.

#### Scenario: Seasons come back newest-first

- GIVEN a club whose team is registered in a tournament starting 2025-03-01
  and another starting 2027-03-01
- WHEN `GetClubHistoryAsync` is called for that club
- THEN the team's `Seasons` list has the 2027 season before the 2025 season
- AND each season entry's `StartDate` matches its tournament's start date

#### Scenario: History table shows the most recent season first

- GIVEN a club history with rows for seasons dated 2026 and 2027
- WHEN `ClubHistoryPage` renders
- THEN the first table body row is the 2027 season

### Requirement: Club History URL Uses the Slug

When `ClubHistoryPage` has loaded a club and the route identifier used to
reach it is not that club's slug (for example the team page navigated with
the club GUID), the page MUST replace the current history entry with the
slug URL `/panel/clubes/<slug>`.

#### Scenario: GUID URL is replaced with the slug

- GIVEN the page is opened at `/panel/clubes/<club-guid>`
- WHEN the club (slug `colon`) finishes loading
- THEN the URL becomes `/panel/clubes/colon`
- AND the history entry is replaced, not pushed

#### Scenario: Slug URL is left untouched

- GIVEN the page is opened at `/panel/clubes/colon` and the loaded club's
  slug is `colon`
- WHEN the club finishes loading
- THEN no navigation occurs
