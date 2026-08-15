# Proposal: High Test Coverage — Batch A (Match & Stage generation)

## Intent

`MatchService` fixture/group-stage generation and `StageService` automated-stage/team-assignment logic directly determine tournament outcomes yet have zero or knockout-only coverage. Silent bugs in round-robin rotation, group divisibility, date spacing, tournament-size validation, or slot-capacity math would corrupt real fixtures undetected. This change adds characterization tests for the already-correct existing behavior. It is a pure test-addition — no refactor, no bug fix.

## Scope

### In Scope (grounded in current source)
- `MatchService.GenerateFixtureAsync`: even counts (2/4/6) produce `(N-1)*2` rounds; no team plays itself; no duplicate pairing within a round; home/away swap across the double fixture; odd count and count `< 2` throw `ArgumentException` (codebase rejects odd — no bye).
- `MatchService.CreateAutomatedMatchesAsync` (Group path) → exercises `ResolveGroupTeamCountAsync` (no groups / no teams / not divisible / `<2` per group all throw) and `CreateGroupStageMatchesAsync` (`n*(n-1)/2` count).
- `DistributeMatchDates` via public paths: single-match midpoint (already via Final), `end < start` throws, normal multi-match spread. `matchCount <= 0` guard is unreachable through the public surface (documented, not forced).
- `StageService.CreateAutomatedStagesAsync`: valid 8/16/32/64; invalid sizes throw; not-divisible-by group size throws; group-letter naming (A/B/C…); date chaining; QuarterFinal only when `>= 16`; already-has-stages and division-not-found throw.
- `StageService.AssignTeamsToStageAsync`: exact-fit, under-capacity, over-capacity throws, full-stage throws, duplicate filtering, auto mode.

### Out of Scope (non-goals)
- No bug fixes even if discovered (logged as Risks below).
- No refactoring of `MatchService`/`StageService`, no visibility changes to private methods.
- No controller changes. Existing `AutomatedMatchGenerationTests.cs` stays as-is (not duplicated).
- Batch B (sanction/JWT) and Batch C deferred.

## Capabilities
### New Capabilities
- None — pure test addition (spec phase may document tested invariants; no product behavior changes).
### Modified Capabilities
- None.

## Approach

Integration-style xUnit tests via existing `CustomWebApplicationFactory` + `ApplicationDBContext` (same seeding pattern as `AutomatedMatchGenerationTests`). Private helpers exercised through public entry points only. Split delivery to fit the 800-line budget:
- **a1 — match-service**: `GenerateFixtureAsync` + group-stage generation + reachable `DistributeMatchDates` edges (~1 file, ~400–550 lines).
- **a2 — stage-service**: `CreateAutomatedStagesAsync` + `AssignTeamsToStageAsync` (~1 file, ~400–600 lines).

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Club12-Backend/API.Tests/MatchServiceGenerationTests.cs` | New | a1 fixture + group tests |
| `Club12-Backend/API.Tests/StageServiceTests.cs` | New | a2 stage + assignment tests |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Private `DistributeMatchDates`/`ResolveGroupTeamCount` limit isolation; `matchCount<=0` unreachable | High | Test observable behavior via public API; flag guard as documented-only |
| Bug: `GenerateFixtureAsync` sets no `StageId`; `divisionId` param unused | Med | Log as follow-up, do NOT fix here |
| Bug: all first-round fixture matches share identical `MatchDate` (spacing only splits round 1 vs 2) | Med | Log as follow-up; test rotation/pairing invariants, not per-match dates |
| Combined scope exceeds 800 lines | High | Ship as a1 then a2 sequential PRs |

## Rollback Plan

Delete the added test file(s). No production code touched, so revert is isolated and risk-free.

## Dependencies

- Existing `CustomWebApplicationFactory` test harness (present).

## Success Criteria

- [ ] a1 + a2 test suites pass green against unmodified services.
- [ ] Named invariants covered: rotation correctness, no self/duplicate pairings, home/away swap, group divisibility, tournament-size validation, slot-capacity math, date bounds.
- [ ] Each PR under the 800-line budget.
- [ ] Discovered bugs recorded as follow-ups, not fixed in this change.

## Proposal question round

Cannot ask interactively (delegated executor). Assumptions needing user review:
1. Test the private date/group helpers strictly through public entry points (no `InternalsVisibleTo`, keeping "no production change")? Assumed **yes**.
2. Confirm odd-team fixtures are expected to **throw** (current behavior), so tests assert rejection rather than bye insertion? Assumed **yes**.
3. Log the `GenerateFixtureAsync` unused-`divisionId`/no-`StageId` and identical-`MatchDate` observations as separate follow-ups without fixing them here? Assumed **yes**.
