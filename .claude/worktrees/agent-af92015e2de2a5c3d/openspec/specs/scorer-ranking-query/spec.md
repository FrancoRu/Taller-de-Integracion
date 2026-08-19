# Scorer Ranking Query Specification

## Purpose

Characterizes `ScorerRepository.GetPlayerScoresAsync`, the EF aggregation query that produces the top-scorer ranking (per-player point totals, name formatting, ordering, pagination, and filters). Test-only characterization — no production code changes.

## Requirements

### Requirement: Points Aggregation

For each player in scope, `Points` MUST equal the sum of that player's `Scorer.Points` records (scoped by any active `TournamentId`/`MatchId` filter), defaulting to `0` when the player has no scorer records.

#### Scenario: Player with multiple scores is summed

- GIVEN a player with two `Scorer` records of 2 and 3 points
- WHEN `GetPlayerScoresAsync` is called
- THEN the player's `Points` equals 5

#### Scenario: Player with no scores defaults to zero

- GIVEN a player with no `Scorer` records
- WHEN `GetPlayerScoresAsync` is called
- THEN the player's `Points` equals 0

### Requirement: Full Name Formatting

`FullName` MUST be `LastName.ToUpper() + " " + FirstName`, and MUST append `" " + SecondName` when `SecondName` is non-null and non-empty.

#### Scenario: Player without a second name

- GIVEN a player with `SecondName` null or empty
- WHEN `GetPlayerScoresAsync` is called
- THEN `FullName` equals `LastName.ToUpper() + " " + FirstName`

#### Scenario: Player with a second name

- GIVEN a player with a non-empty `SecondName`
- WHEN `GetPlayerScoresAsync` is called
- THEN `FullName` equals `LastName.ToUpper() + " " + FirstName + " " + SecondName`

### Requirement: Descending Order and Pagination

Results MUST be ordered by `Points` descending, then paginated using `(PageNumber - 1) * PageSize` skip and `PageSize` take; the returned total count MUST reflect the unpaginated filtered set.

#### Scenario: Higher scorers rank first

- GIVEN players with distinct point totals
- WHEN `GetPlayerScoresAsync` is called
- THEN items are returned in descending `Points` order

#### Scenario: Page 2 returns the next slice

- GIVEN more players than fit on one page
- WHEN `GetPlayerScoresAsync` is called with `PageNumber = 2`
- THEN the second page's items follow the first page's items in ranking order
- AND `TotalCount` equals the full filtered player count, not the page size

### Requirement: Scope Filters

`TournamentId`, `MatchId`, `TeamId`, and `PlayerId` filters MUST each restrict both the player set and the scorer records summed into `Points` to the matching scope.

#### Scenario: Tournament filter restricts to that tournament's teams and scorers

- GIVEN players/scorers across two tournaments
- WHEN `GetPlayerScoresAsync` is called with `TournamentId` set to one of them
- THEN only players on teams in that tournament are returned
- AND only that tournament's scorer records contribute to `Points`

#### Scenario: Match/Team/Player filters narrow the result set

- GIVEN players across multiple matches, teams, and identities
- WHEN `GetPlayerScoresAsync` is called with `MatchId`, `TeamId`, or `PlayerId` set
- THEN only the matching player(s) are returned, scoped accordingly

## Known Test-Harness Limitation

- The query uses `ToUpper()`, null-coalescing string concatenation, and a correlated `Sum` subquery, all EF-translated to SQL. SQLite (test provider) may translate these differently than Npgsql (production). Tests MUST assert against the actual SQLite-translated output and use ASCII-only test data; any expression SQLite cannot translate MUST be documented as a harness limitation rather than assumed equivalent to Npgsql.
