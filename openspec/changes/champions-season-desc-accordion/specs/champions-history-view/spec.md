# Champions History View Specification

## Purpose

Define the ordering of seasons on `/campeones` and their collapsible
presentation.

Scope: `ChampionService.GetChampionsHistoryAsync` under the xUnit + SQLite
harness, `groupChampions` and `PublicChampionsPage` under Vitest + Testing
Library.

## Requirements

### Requirement: Champion History Carries the Season Year

`GetChampionsHistoryAsync` MUST set `ChampionHistoryResponse.SeasonYear` to
the `Year` of the tournament's season, or null when the tournament has no
season or the season has no year.

#### Scenario: A season's year is exposed

- GIVEN a finished tournament grouped under a season with `Year = 2026` and a
  decided division champion
- WHEN `GetChampionsHistoryAsync` is called
- THEN the corresponding history entry has `SeasonYear` equal to 2026

#### Scenario: No season means no year

- GIVEN a finished tournament with no season and a decided division champion
- WHEN `GetChampionsHistoryAsync` is called
- THEN the corresponding history entry has `SeasonYear` null

### Requirement: Seasons Grouped Newest-First

`groupChampions` MUST return the season buckets ordered by `seasonYear`
descending. Buckets with no year (including the "Sin temporada" bucket) MUST
come after every bucket that has one; among buckets that tie on year (or are
both null), `seasonName` descending breaks the tie so the result is
deterministic. Each returned bucket MUST carry its `seasonYear`.

#### Scenario: Newer year first

- GIVEN champion entries for `seasonYear` 2025 and 2026, listed 2025 before
  2026
- WHEN `groupChampions` runs
- THEN the returned buckets are ordered 2026, then 2025

#### Scenario: Null-year season sorts last

- GIVEN a `seasonYear` 2025 bucket and a null-year "Sin temporada" bucket
- WHEN `groupChampions` runs
- THEN the 2025 bucket comes before the "Sin temporada" bucket

### Requirement: Each Season Is a Collapsible Accordion, Newest Expanded

`PublicChampionsPage` MUST render each season as an accordion. Only the first
(newest) season MUST be expanded by default; every other season MUST start
collapsed.

#### Scenario: Only the newest season is open

- GIVEN champion history spanning two seasons (2026 and 2025)
- WHEN `PublicChampionsPage` renders
- THEN the 2026 season's panel content is visible
- AND the 2025 season's panel is collapsed
- AND expanding the 2025 season reveals its tournaments
