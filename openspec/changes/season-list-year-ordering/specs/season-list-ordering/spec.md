# Season List Ordering Specification

## Purpose

Define the order of the season list returned by `GET /api/seasons`
(consumed by the admin `/panel/temporadas` page and the public `/temporadas`
page, both of which render the array as received).

Scope is limited to behavior observable through
`SeasonService.GetAllSeasonsAsync` with xUnit + the SQLite in-memory harness.

## Requirements

### Requirement: Seasons Ordered by Year, Newest First

`GetAllSeasonsAsync` MUST return seasons ordered by `Season.Year` in
descending order (the most recent year first). Seasons whose `Year` is null
MUST appear after every season that has a year. When two seasons share the
same year (or are both null), they MUST be ordered by `Name` so the result
is deterministic across requests.

#### Scenario: Newest year first

- GIVEN seasons with years 2024, 2026 and 2025, created in that order
- WHEN `GetAllSeasonsAsync` is called
- THEN they are returned in the order 2026, 2025, 2024
- AND each returned season's `Year` is less than or equal to the previous
  one's

#### Scenario: Null-year seasons sort last

- GIVEN a season with `Year = 2025` and a season with `Year = null`
- WHEN `GetAllSeasonsAsync` is called
- THEN the `Year = 2025` season appears before the `Year = null` season

#### Scenario: Same year is broken by name

- GIVEN two seasons both with `Year = 2026`, named "Temporada B" and
  "Temporada A"
- WHEN `GetAllSeasonsAsync` is called
- THEN "Temporada A" appears before "Temporada B"
