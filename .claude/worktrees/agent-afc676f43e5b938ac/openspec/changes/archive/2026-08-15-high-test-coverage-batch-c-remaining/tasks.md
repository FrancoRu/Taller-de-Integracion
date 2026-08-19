# Tasks: High Test Coverage — Batch C (remaining medium-value logic)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 360–490 (3 new files, no production edits) |
| **Actual changed lines** | **873 (3 new files, no production edits) — exceeds the 800-line PR budget; flagged as a risk for delivery/review** |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr |
| Chain strategy | size-exception |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Sanction expiry boundary tests | PR 1 (single) | `dotnet test Club12-Backend/API.Tests --filter FullyQualifiedName~PlayerSanctionServiceTests` | `CustomWebApplicationFactory` (SQLite, real `PlayerSanctionService`) | delete `PlayerSanctionServiceTests.cs` |
| 2 | Team registration diff tests | PR 1 (single) | `dotnet test Club12-Backend/API.Tests --filter FullyQualifiedName~TeamServiceRegisterTests` | `CustomWebApplicationFactory` (SQLite, real `TeamService`) | delete `TeamServiceRegisterTests.cs` |
| 3 | Scorer ranking aggregation tests | PR 1 (single) | `dotnet test Club12-Backend/API.Tests --filter FullyQualifiedName~ScorerRepositoryTests` | `CustomWebApplicationFactory` (SQLite, real `ScorerRepository`) | delete `ScorerRepositoryTests.cs` |

All three units are independent (separate files, no shared seed base class), can be implemented in any order, and merge as one PR (`size:exception`, `single-pr` delivery strategy, under the 800-line change budget).

## Phase 1: Sanction Expiry (`Club12-Backend/API.Tests/PlayerSanctionServiceTests.cs`)

- [x] 1.1 Create file skeleton: `IClassFixture<CustomWebApplicationFactory>`, DI-scope resolve `IPlayerSanctionService` + `ApplicationDBContext`, local seed helper for `Player` + `PlayerSanction` (UTC whole-second `IssuedDate`, whole-day `Duration`).
- [x] 1.2 RED/GREEN: `GetExpiredSanctionsAsync_BeforeExpiry_IsExcluded` — sanction where `IssuedDate.AddDays(Duration)` is one day after cutoff → not in result (spec: Sanction not yet expired is excluded).
- [x] 1.3 RED/GREEN: `GetExpiredSanctionsAsync_ExactlyAtBoundary_IsIncluded` — `IssuedDate.AddDays(Duration) == cutoffDate` → included (spec: inclusive boundary, `PlayerSanctionService.cs:48-53`).
- [x] 1.4 RED/GREEN: `GetExpiredSanctionsAsync_WellBeforeCutoff_IsIncluded` — expiry several days before cutoff → included.
- [x] 1.5 RED/GREEN: `GetExpiredSanctionsAsync_NoMatches_ReturnsEmpty` — only non-expired sanctions seeded → empty collection.
- [x] 1.6 RED/GREEN: `GetExpiredSanctionsAsync_IncludesPlayerNavigation` — expired sanction linked to seeded `Player` → returned `Player` non-null and matches seed.
- [x] 1.7 SQLite translated `AddDays(column)` successfully (no throw) — documented as a class-level XML remark instead of a limitation workaround, per design Open Question.

## Phase 2: Team Tournament Registration (`Club12-Backend/API.Tests/TeamServiceRegisterTests.cs`)

- [x] 2.1 Create file skeleton: `IClassFixture<CustomWebApplicationFactory>`, resolve `ITeamService` + `ApplicationDBContext`, seed helper for `Tournament`/`Team` with deterministic GUIDs and ASCII names.
- [x] 2.2 RED/GREEN: `RegisterTeamsToTournamentAsync_UnassignedTeam_IsRegistered` — team with `TournamentId == null` in `teamIds` → assigned to target tournament (re-query to assert).
- [x] 2.3 RED/GREEN: `RegisterTeamsToTournamentAsync_DroppedTeam_IsUnassigned` — team currently in target tournament, absent from `teamIds` → `TournamentId` becomes null.
- [x] 2.4 RED/GREEN: `RegisterTeamsToTournamentAsync_EmptyList_UnassignsAllCurrentMembers` — 2+ teams in target tournament, empty `teamIds` → all become null.
- [x] 2.5 RED/GREEN: `RegisterTeamsToTournamentAsync_ExistingMember_StaysRegistered` — team already in target tournament, included in `teamIds` → unchanged.
- [x] 2.6 RED/GREEN: `RegisterTeamsToTournamentAsync_CrossTournamentTeam_IsReassigned` — team in a different tournament, included in `teamIds` → reassigned to target.
- [x] 2.7 RED/GREEN: `RegisterTeamsToTournamentAsync_UninvolvedTeam_IsUntouched` — team in a different tournament, absent from `teamIds` → `TournamentId` unchanged.

## Phase 3: Scorer Ranking Query (`Club12-Backend/API.Tests/ScorerRepositoryTests.cs`)

- [x] 3.1 Create file skeleton: `IClassFixture<CustomWebApplicationFactory>`, resolve `IScorerRepository` + `ApplicationDBContext`, ASCII-only seed helper for `Player`/`Team`/`Tournament`/`Match`/`Scorer`.
- [x] 3.2 RED/GREEN: `GetPlayerScoresAsync_MultipleScores_SumsPoints` — player with 2 `Scorer` records (2 + 3) → `Points == 5`.
- [x] 3.3 RED/GREEN: `GetPlayerScoresAsync_NoScores_DefaultsToZero` — player with no `Scorer` records → `Points == 0`.
- [x] 3.4 RED/GREEN: `GetPlayerScoresAsync_NoSecondName_FormatsWithoutTrailingSeparator` — null/empty `SecondName` → `FullName == LastName.ToUpper() + " " + FirstName`.
- [x] 3.5 RED/GREEN: `GetPlayerScoresAsync_WithSecondName_AppendsSecondName` — non-empty `SecondName` → `FullName` includes `" " + SecondName`.
- [x] 3.6 RED/GREEN: `GetPlayerScoresAsync_DistinctTotals_OrdersDescending` — players with distinct point totals → returned in descending `Points` order.
- [x] 3.7 RED/GREEN: `GetPlayerScoresAsync_PageTwo_ReturnsNextSliceAndFullTotalCount` — more players than one page, `PageNumber = 2` → second page follows ranking order, `TotalCount` reflects full filtered set.
- [x] 3.8 RED/GREEN: `GetPlayerScoresAsync_TournamentFilter_RestrictsPlayersAndScorers` — players/scorers across two tournaments, `TournamentId` set → only that tournament's players/scores counted.
- [x] 3.9 RED/GREEN: `GetPlayerScoresAsync_MatchTeamOrPlayerFilter_NarrowsResultSet` — `MatchId`/`TeamId`/`PlayerId` set → only matching player(s) returned.
- [x] 3.10 SQLite translated `ToUpper()`/null-coalescing concat/correlated `Sum` successfully (no throw) — documented as a class-level XML remark, per design.

## Phase 4: Verification

- [x] 4.1 Run `dotnet test Club12-Backend/API.Tests --filter FullyQualifiedName~PlayerSanctionServiceTests` — all pass (5/5).
- [x] 4.2 Run `dotnet test Club12-Backend/API.Tests --filter FullyQualifiedName~TeamServiceRegisterTests` — all pass (6/6).
- [x] 4.3 Run `dotnet test Club12-Backend/API.Tests --filter FullyQualifiedName~ScorerRepositoryTests` — all pass (8/8).
- [x] 4.4 Run full `dotnet test Club12-Backend/API.Tests` to confirm no regressions against existing Batch A/B suites — 121/121 pass.
- [x] 4.5 Confirm zero production files (`Application/`, `Infrastructure/`, `API/Controllers/`) changed — diff limited to the 3 new test files (confirmed via `git status --short`). Full build: 0 warnings, 0 errors.
