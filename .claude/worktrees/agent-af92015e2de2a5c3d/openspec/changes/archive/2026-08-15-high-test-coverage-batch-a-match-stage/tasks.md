# Tasks: High Test Coverage — Batch A (Match & Stage Generation)

Two independent slices, each its own single PR against `develop`. No cross-slice
dependency — either may merge first. Both are pure test-addition; zero production
code changes (verified in Phase-final tasks of each slice).

---

## Slice A1 — MatchServiceGenerationTests.cs

### Review Workload Forecast (A1)

| Field | Value |
|-------|-------|
| Estimated changed lines | 480–560 (new file only) |
| 800-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr (per-slice) |
| Chain strategy | pending — not applicable, estimate is well under the 800-line budget |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
800-line budget risk: Low

### Suggested Work Units (A1)

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Characterize `GenerateFixtureAsync` + group-path `CreateAutomatedMatchesAsync` | PR A1 | `dotnet test Club12-Backend/API.Tests --filter FullyQualifiedName~MatchServiceGenerationTests` | `CustomWebApplicationFactory` + in-memory SQLite (real integration, same as `AutomatedMatchGenerationTests.cs`) | Delete `Club12-Backend/API.Tests/MatchServiceGenerationTests.cs`; no production file touched |

### Phase 1: Foundation
- [x] 1.1 Create `Club12-Backend/API.Tests/MatchServiceGenerationTests.cs`: class `MatchServiceGenerationTests : IClassFixture<CustomWebApplicationFactory>`, using-block mirroring `AutomatedMatchGenerationTests.cs`.
- [x] 1.2 Add private seed helpers: `SeedTeamsAsync(db, count)`, `SeedDivisionAsync(db)`, `SeedGroupStagesAsync(db, divisionId, groupCount)`, `SeedGroupStageWithTeamsAsync(db, teamsPerGroup, groupCount)` — local to this file, no shared/production code.

### Phase 2: RED — `GenerateFixtureAsync` (Requirement: Double Round-Robin Fixture Generation)
- [x] 2.1 ~~Test: 4 teams → 12 matches persisted...~~ DEVIATION: superseded — see apply report. `Match.StageId` is a REQUIRED FK; BUG-1 leaves it `Guid.Empty`, so persistence always throws `DbUpdateException` (FK violation) for any valid team count. Rotation/pairing is unobservable via black-box read-back. Replaced by `GenerateFixtureAsync_ValidTeamCount_ThrowsForeignKeyViolation_Bug1` (Theory 2/4/8).
- [x] 2.2 ~~Test: 8 teams → 56 matches...~~ Same deviation as 2.1 — covered by the same replacement Theory (teamCount=8 case).
- [x] 2.3 Theory: odd count (5) and sub-minimum count (0, 1) each throw `ArgumentException`, zero matches persisted (spec "Odd or sub-minimum team count is rejected"). — `GenerateFixtureAsync_InvalidTeamCount_ThrowsAndPersistsNoMatches`.
- [x] 2.4 Characterize BUG-1: DEVIATION — `StageId == Guid.Empty` is never durably observable (FK violation on insert, see 2.1). Replaced by `GenerateFixtureAsync_ValidTeamCount_ThrowsForeignKeyViolation_Bug1` (throw characterization) + `GenerateFixtureAsync_DivisionIdArgumentDoesNotAffectFailure_Bug1` (divisionId proven irrelevant to the identical failure).
- [x] 2.5 Characterize BUG-2: DEVIATION — unobservable via black-box read-back for the same FK reason as 2.1/2.4 (matches never persist). Documented via code comment (source-level confirmation of `currentMatchDate` reuse) instead of a runnable assertion, mirroring the treatment of the two other unreachable guards in this file.

### Phase 3: RED — Group-stage resolution (Requirement: Group-Stage Team Count Resolution)
- [x] 3.1 DEVIATION: "zero Group stages" is unreachable — `ResolveGroupTeamCountAsync`'s `totalGroups<=0` guard's own count query always includes the very Group-type stage being queried (self-inclusive), so `totalGroups >= 1` whenever the code path is reached at all. Documented via code comment instead of a runnable test (same unreachable-guard pattern as the `matchCount<=0` case in Phase 4/design.md).
- [x] 3.2 Test: group stages exist, zero teams registered → throws (spec "No teams registered"). — `CreateAutomatedMatchesAsync_GroupStage_NoTeamsRegistered_ThrowsInvalidOperationException`.
- [x] 3.3 Test: 10 teams / 3 group stages (not divisible) → throws (spec "Teams not evenly divisible by group count"). — `CreateAutomatedMatchesAsync_GroupStage_TeamsNotDivisibleByGroupCount_Throws`.
- [x] 3.4 Test: configuration resolving to <2 teams/group → throws (spec "Fewer than 2 teams resolve per group"). — `CreateAutomatedMatchesAsync_GroupStage_FewerThanTwoTeamsPerGroup_Throws`.
- [x] 3.5 Test: 8 teams/2 groups (4/group) → 6 matches (`4*3/2`); 16 teams/2 groups (8/group) → 28 matches (`8*7/2`) (spec "Valid distribution creates round-robin matches"). — `CreateAutomatedMatchesAsync_GroupStage_ValidDistribution_CreatesRoundRobinMatches` (Theory).

### Phase 4: RED — Match date distribution, reachable via group path only
- [x] 4.1 Test: configuration resolving to exactly 1 group match → date == `StartDate + (EndDate-StartDate)/2` (spec "Single match uses the range midpoint"). — `CreateAutomatedMatchesAsync_GroupStage_SingleMatch_UsesRangeMidpoint`.
- [x] 4.2 Test: configuration resolving to N>1 group matches → first==`StartDate`, last==`EndDate`, evenly spaced (spec "Multiple matches spread across the range"). — `CreateAutomatedMatchesAsync_GroupStage_MultipleMatches_SpreadEvenlyAcrossRange`.
- [x] 4.3 Test: stage `EndDate < StartDate` → `CreateAutomatedMatchesAsync` throws `ArgumentException` (spec "End date before start date"). — `CreateAutomatedMatchesAsync_GroupStage_EndDateBeforeStartDate_ThrowsArgumentException`.
- [x] 4.4 Do NOT write a test for `matchCount<=0` — add one code comment noting it is unreachable via the public surface (spec "Non-positive match count", documented-only).

### Phase 5: Verification
- [x] 5.1 Run `dotnet test Club12-Backend/API.Tests --filter FullyQualifiedName~MatchServiceGenerationTests` — all green against unmodified `MatchService`. Result: 15/15 passed.
- [x] 5.2 Confirm `git diff --stat Club12-Backend/Application` is empty (no production change). Confirmed empty; `git status` shows `MatchService.cs` untouched (only the new untracked test file).

---

## Slice A2 — StageServiceTests.cs

### Review Workload Forecast (A2)

| Field | Value |
|-------|-------|
| Estimated changed lines | 460–580 (new file only) |
| 800-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr (per-slice) |
| Chain strategy | pending — not applicable, estimate is well under the 800-line budget |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
800-line budget risk: Low

### Suggested Work Units (A2)

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Characterize `CreateAutomatedStagesAsync` + `AssignTeamsToStageAsync` | PR A2 | `dotnet test Club12-Backend/API.Tests --filter FullyQualifiedName~StageServiceTests` | `CustomWebApplicationFactory` + in-memory SQLite (real integration, same pattern as `AutomatedMatchGenerationTests.cs`) | Delete `Club12-Backend/API.Tests/StageServiceTests.cs`; no production file touched |

### Phase 1: Foundation
- [x] 1.1 Create `Club12-Backend/API.Tests/StageServiceTests.cs`: class `StageServiceTests : IClassFixture<CustomWebApplicationFactory>`.
- [x] 1.2 Add private seed helpers: `SeedTournamentWithTeamsAsync(db, teamCount)`, `SeedDivisionAsync(db, tournament, withStages: bool)`, `SeedStageWithSlotsAsync(db, stageType, existingAssignmentCount)` — local to this file.

### Phase 2: RED — `CreateAutomatedStagesAsync` (Requirement: Automated Stage Chain Generation)
- [x] 2.1 Test: 8 teams → 5 stages (`Grupo A`, `Grupo B`, `SemiFinal`, `ThirdPlace`, `Final`), no `QuarterFinal`; each `StartDate` follows the previous stage's `EndDate` + documented gap (spec "8 teams produce a 2-group chain without quarter-finals").
- [x] 2.2 Theory: 16/32/64 teams → group count = teams/4 (4, 8, 16) lettered A, B, C…; `QuarterFinal` present before `SemiFinal`, `ThirdPlace`, `Final` in order (spec "16/32/64 teams include quarter-finals").
- [x] 2.3 Theory: invalid counts (10, 12) → `InvalidOperationException`, zero stages created (spec "Invalid team count is rejected").
- [x] 2.4 Test: non-existent `divisionId` → throws; division that already has ≥1 stage → throws (spec "Division not found or already has stages").

### Phase 3: RED — `AssignTeamsToStageAsync` (Requirement: Team Assignment to Stage)
- [x] 3.1 Test: capacity 4, 0 existing → assign 4 IDs manually → 4 `StageTeamMatch` records created (spec "Exact slot match assigns all teams").
- [x] 3.2 Test: 1 available slot → assign 3 IDs manually → throws `Exception`, zero records created (spec "Too many teams for available slots").
- [x] 3.3 Test: capacity 4, 0 existing → assign 2 IDs → 2 records created, 2 slots remain (spec "Too few teams leaves slots open").
- [x] 3.4 Test: existing assignments == capacity → manual and auto assignment each throw `Exception` (spec "Stage already at capacity").
- [x] 3.5 Test: manual request with duplicate/already-assigned IDs → duplicates silently excluded from created records (spec "Duplicate team IDs are filtered").
- [x] 3.6 Test: `auto=true` with N available slots and unassigned teams in the tournament → at most N teams auto-assigned, drawn only from teams not already linked to the stage (spec "Auto mode assigns up to available slots").

### Phase 4: Verification
- [x] 4.1 Run `dotnet test Club12-Backend/API.Tests --filter FullyQualifiedName~StageServiceTests` — all green against unmodified `StageService`.
- [x] 4.2 Confirm `git diff --stat Club12-Backend/Application` is empty (no production change).

---

## Follow-up note (non-blocking, not part of this change)

BUG-1 (`GenerateFixtureAsync` never sets `Match.StageId`, ignores its `divisionId` param) and
BUG-2 (all round-1 fixture matches share one `MatchDate`) are characterized as current behavior
in Phase 2 of Slice A1 — not fixed. Log as a candidate follow-up change; do not alter
`MatchService` production code in this batch.
