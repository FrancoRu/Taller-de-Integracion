# Match Fixture & Group-Stage Generation Specification

## Purpose
Characterize and pin, via automated tests, the existing round-robin fixture generation, group-stage match creation, and match-date distribution behavior in `MatchService`. Test-only characterization — no production code changes.

## Requirements

### Requirement: Double Round-Robin Fixture Generation
`GenerateFixtureAsync` MUST generate a double round-robin fixture (`N * (N-1)` total matches) for an even, positive team count, with no team facing itself and no duplicate pairing within a round.

#### Scenario: 4 teams produce 12 matches
- GIVEN 4 teams
- WHEN `GenerateFixtureAsync` is called
- THEN 12 matches are persisted, none with `HomeTeamId == VisitorTeamId`
- AND no pairing repeats within a single round

#### Scenario: 8 teams produce 56 matches
- GIVEN 8 teams
- WHEN `GenerateFixtureAsync` is called
- THEN 56 matches are persisted
- AND home/away are swapped between corresponding first-half and second-half round matches

#### Scenario: Odd or sub-minimum team count is rejected
- GIVEN an odd team count (e.g. 5) or fewer than 2 teams
- WHEN `GenerateFixtureAsync` is called
- THEN it throws `ArgumentException` and persists no matches

### Requirement: Group-Stage Team Count Resolution
`CreateAutomatedMatchesAsync` on a `Group` stage MUST resolve teams-per-group as registered-teams ÷ group-stage-count, and MUST reject configurations that cannot distribute evenly or yield fewer than 2 teams per group.

#### Scenario: No group stages exist
- GIVEN a division with zero `Group`-type stages
- WHEN `CreateAutomatedMatchesAsync` runs for a group stage
- THEN it throws `InvalidOperationException`

#### Scenario: No teams registered
- GIVEN group stages exist but zero teams are registered
- WHEN `CreateAutomatedMatchesAsync` runs
- THEN it throws `InvalidOperationException`

#### Scenario: Teams not evenly divisible by group count
- GIVEN 10 registered teams and 3 group stages
- WHEN `CreateAutomatedMatchesAsync` runs
- THEN it throws `InvalidOperationException`

#### Scenario: Fewer than 2 teams resolve per group
- GIVEN a configuration resolving to fewer than 2 teams per group
- WHEN `CreateAutomatedMatchesAsync` runs
- THEN it throws `InvalidOperationException`

#### Scenario: Valid distribution creates round-robin matches
- GIVEN 8 registered teams across 2 groups (4 teams/group)
- WHEN `CreateAutomatedMatchesAsync` runs for a group stage
- THEN 6 matches are created (`4*3/2`)
- AND for 8 teams/group (e.g. 16 teams, 2 groups) 28 matches are created (`8*7/2`)

### Requirement: Match Date Distribution
`DistributeMatchDates`, exercised only through public group/knockout/final match creation, MUST place a single match at the stage date-range midpoint and MUST spread multiple matches evenly across the range inclusive of both endpoints.

#### Scenario: Single match uses the range midpoint
- GIVEN a stage date range resolving to exactly 1 match
- WHEN matches are generated
- THEN the match date equals `StartDate + (EndDate - StartDate) / 2`

#### Scenario: Multiple matches spread across the range
- GIVEN a stage date range resolving to N > 1 matches
- WHEN matches are generated
- THEN the first date equals `StartDate`, the last equals `EndDate`, and intermediate dates are evenly spaced

#### Scenario: End date before start date
- GIVEN a stage `EndDate` earlier than its `StartDate`
- WHEN matches are generated
- THEN it throws `ArgumentException`

#### Scenario: Non-positive match count (documented only, not independently testable)
- GIVEN `DistributeMatchDates` internally guards `matchCount <= 0` with `ArgumentException`
- WHEN reached only through public entry points
- THEN this branch is unreachable under current invariants (minimum resolvable count is always ≥ 1)
- AND it is documented here, not covered by a runnable test, per proposal Risk #1

## Known Behavior — Not Fixed In This Change
- `GenerateFixtureAsync` does not set `Match.StageId` and ignores its `divisionId` parameter. Characterized as-is; tracked as a follow-up, not a spec violation.
- All matches within one fixture round share an identical `MatchDate` (spacing only differentiates first vs. second half). Characterized as-is; tracked as a follow-up, not a spec violation.
