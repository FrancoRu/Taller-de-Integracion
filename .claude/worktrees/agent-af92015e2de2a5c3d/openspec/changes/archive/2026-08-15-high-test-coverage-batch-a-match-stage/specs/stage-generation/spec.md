# Automated Stage Chain & Team Assignment Specification

## Purpose

Characterize and pin, via automated tests, the existing automated stage-chain generation and team-assignment behavior in `StageService`. Test-only characterization — no production code changes.

## Requirements

### Requirement: Automated Stage Chain Generation

`CreateAutomatedStagesAsync` MUST accept only tournament sizes of 8, 16, 32, or 64 registered teams, MUST create one `Group` stage per 4 teams named `"{template} - Grupo {A,B,C...}"`, MUST include a `QuarterFinal` stage only when registered teams ≥ 16, MUST always include `SemiFinal`, `ThirdPlace`, and `Final` stages, and MUST chain each stage's `StartDate` from the previous stage's `EndDate` plus its fixed gap.

#### Scenario: 8 teams produce a 2-group chain without quarter-finals
- GIVEN a division with 8 registered teams and no existing stages
- WHEN `CreateAutomatedStagesAsync` is called
- THEN 5 stages are created: `Grupo A`, `Grupo B`, `SemiFinal`, `ThirdPlace`, `Final`
- AND no `QuarterFinal` stage exists
- AND each stage's `StartDate` follows the previous stage's `EndDate` plus the documented gap

#### Scenario: 16/32/64 teams include quarter-finals
- GIVEN a division with 16, 32, or 64 registered teams
- WHEN `CreateAutomatedStagesAsync` is called
- THEN group-stage count is teams ÷ 4 (4, 8, or 16), lettered sequentially A, B, C...
- AND a `QuarterFinal` stage is included before `SemiFinal`
- AND `SemiFinal`, `ThirdPlace`, `Final` follow in that order

#### Scenario: Invalid team count is rejected
- GIVEN a registered team count not in {8, 16, 32, 64} (e.g. 10 or 12)
- WHEN `CreateAutomatedStagesAsync` is called
- THEN it throws `InvalidOperationException` and creates no stages

#### Scenario: Division not found or already has stages
- GIVEN a non-existent `divisionId`, OR a division that already has at least one stage
- WHEN `CreateAutomatedStagesAsync` is called
- THEN it throws `InvalidOperationException`

### Requirement: Team Assignment to Stage

`AssignTeamsToStageAsync` MUST assign teams up to the stage's slot capacity (per `StageType`), MUST reject manual assignment that would exceed available slots, MUST reject assignment to an already-full stage, and MUST cap automatic assignment at the number of available slots.

#### Scenario: Exact slot match assigns all teams
- GIVEN a stage with slot capacity 4 and 0 existing assignments
- WHEN 4 team IDs are assigned manually
- THEN all 4 `StageTeamMatch` records are created

#### Scenario: Too many teams for available slots
- GIVEN a stage with 1 available slot
- WHEN 3 team IDs are assigned manually
- THEN it throws `Exception` and creates no records

#### Scenario: Too few teams leaves slots open
- GIVEN a stage with slot capacity 4 and 0 existing assignments
- WHEN 2 team IDs are assigned manually
- THEN 2 `StageTeamMatch` records are created and 2 slots remain available

#### Scenario: Stage already at capacity
- GIVEN a stage where existing assignments already equal its slot capacity
- WHEN any assignment (manual or auto) is attempted
- THEN it throws `Exception`

#### Scenario: Duplicate team IDs are filtered
- GIVEN a manual request containing duplicate team IDs or IDs already assigned to the stage
- WHEN `AssignTeamsToStageAsync` is called
- THEN duplicates and already-assigned IDs are silently excluded from the created records

#### Scenario: Auto mode assigns up to available slots
- GIVEN a stage with N available slots and unassigned teams in the tournament
- WHEN `AssignTeamsToStageAsync` is called with `auto = true`
- THEN at most N teams are auto-assigned, selected from teams not already linked to the stage

## Non-Goals

- No fixes for discovered behavior in this change; all scenarios characterize existing `StageService` code as-is.
- No visibility changes to private helper methods; all scenarios exercise them only through public entry points.
