# Proposal: High Test Coverage — Batch C (remaining medium-value logic)

## Intent
Batch C closes the last untested high-value backend logic identified in `sdd/high-test-coverage/explore`, after Batches A (Match/Stage) and B (Sanction appeal/Auth JWT) shipped. Characterize three behaviors that silently affect sanction lifecycle, tournament registration, and top-scorer reporting: `PlayerSanctionService.GetExpiredSanctionsAsync` (date-math predicate), `TeamService.RegisterTeamsToTournamentAsync` (diff/assign/unassign), and `ScorerRepository.GetPlayerScoresAsync` (EF aggregation query). Pure test-addition, same discipline as A/B.

## Scope

### In Scope
- `PlayerSanctionServiceTests`: pin `GetExpiredSanctionsAsync` — expiry = `IssuedDate.AddDays(Duration) <= cutoffDate` (inclusive). Boundary cases: before / exactly-at / after expiry, empty result, `Player` include populated.
- `TeamServiceRegisterTests`: pin `RegisterTeamsToTournamentAsync` — add new team, unassign team dropped from list, leave already-registered team unchanged, reassign team from another tournament, empty list unassigns all current members.
- `ScorerRepositoryTests`: pin `GetPlayerScoresAsync` — points Sum aggregation, descending order, pagination, Tournament/Team/Match/Player filters, `FullName` formatting (with/without SecondName), zero-score players default to 0.
- All three via the existing `CustomWebApplicationFactory` integration harness (real service + real `ApplicationDBContext` over in-memory SQLite), matching the Batch A/B pattern.

### Out of Scope
- `sortPositions()` (frontend, `divisionStandings.tsx`): EXCLUDED. Backend never populates `positions` (`PositionEntityConfiguration` is an empty stub; no service/repo computes standings), so `division.positions` is always empty. The function is module-local (not exported) — testing it would need a production change and would validate logic wired to dead data. Logged as a follow-up gap, not a test.
- Any bug fix, refactor, or production-code change (log discoveries as follow-ups).
- Extracting/relocating logic, new mocking libraries, CRUD pass-through services.

## Capabilities

### New Capabilities
None — test-only change, no spec-level behavior added.

### Modified Capabilities
None — no requirement changes; characterization tests pin existing behavior.

## Approach
Add three xUnit test classes to `Club12-Backend/API.Tests`, each `IClassFixture<CustomWebApplicationFactory>`. Resolve the real `IPlayerSanctionService` / `ITeamService` / `IScorerRepository` from a DI scope, seed entities through `ApplicationDBContext`, exercise the method, and assert observed behavior (or re-query DB state for the Team case). No fakes/mocks (none exist in the project); no production edits.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Club12-Backend/API.Tests/PlayerSanctionServiceTests.cs` | New | Expiry date-math boundary tests |
| `Club12-Backend/API.Tests/TeamServiceRegisterTests.cs` | New | Register diff/assign/unassign tests |
| `Club12-Backend/API.Tests/ScorerRepositoryTests.cs` | New | Top-scorer aggregation integration tests |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| SQLite vs Npgsql translation of `IssuedDate.AddDays(Duration)` may differ or fail | Med | Confirm at design/apply; if untranslatable in SQLite, document harness limit rather than change production |
| `GetPlayerScoresAsync` uses `ToUpper`/null-concat/subquery `Sum` — SQLite translation edge cases | Low | Use ASCII test data; assert on values SQLite reliably translates |
| Seeding the Scorer→Match→Stage→Division→Tournament chain is verbose | Low | Small shared seed helpers within the test class |

## Rollback Plan
Delete the three new test files. No production code touched, so no functional rollback needed.

## Dependencies
- Existing `CustomWebApplicationFactory` harness (in place).
- Batches A and B merged (done).

## Success Criteria
- [ ] Three new test files added; full `API.Tests` suite green.
- [ ] `GetExpiredSanctionsAsync` inclusive-boundary behavior pinned.
- [ ] `RegisterTeamsToTournamentAsync` add/remove/keep/reassign/empty paths pinned.
- [ ] `GetPlayerScoresAsync` aggregation, ordering, pagination, and filters pinned.
- [ ] No production code changed; `sortPositions`/`positions` dead-data gap logged as follow-up.
- [ ] Authored test lines within single-PR 800-line budget (est. ~360–490).

## Proposal question round
Non-interactive launch — one assumption needs user confirmation before spec/design:
1. Exclude `sortPositions()` from this batch and log the dead `positions` data-source as a follow-up (recommended), rather than testing the pure function against fabricated data or exporting it (a production change)? Assumed: **yes, exclude**.
