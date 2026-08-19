# Design: High Test Coverage — Batch A (Match & Stage generation)

## Technical Approach

Add two xUnit test files to the existing `Club12-Backend/API.Tests/` project, exercising
`MatchService` (a1) and `StageService` (a2) generation/assignment logic **black-box, through
their public `IMatchService`/`IStageService` entry points** resolved from the real host via
`CustomWebApplicationFactory` + in-memory SQLite. This mirrors the sole existing precedent for
this code, `AutomatedMatchGenerationTests.cs`. No production code is touched.

## Architecture Decisions

| Decision | Choice | Alternatives rejected | Rationale |
|----------|--------|-----------------------|-----------|
| Private-method access | Black-box via public entry points; **no `InternalsVisibleTo`** | Add `InternalsVisibleTo` to unit-test `DistributeMatchDates`/`ResolveGroupTeamCountAsync`/`CreateGroupStageMatchesAsync` directly | Codebase has **no** `InternalsVisibleTo` (grep-confirmed; explicitly documented as absent in `DatabaseBackupHostedService.cs`). Keeps "pure test-addition" with zero production change. |
| Test isolation level | Full integration harness (`CustomWebApplicationFactory`, real SQLite round-trip) for all scenarios | Direct unit tests with mocked/faked `IUnitOfWork` repositories | Services take `IUnitOfWork` and pass `Expression<Func<T,bool>>` predicates to repositories (e.g. `CountAsync(s => s.DivisionId==… && s.StageType==Group)`). Faking would reimplement EF query evaluation; no mocking library exists (Backup tests hand-roll fakes). Real SQLite evaluates predicates correctly for free and matches precedent. |
| a1/a2 split | Two independent files, one PR each, sequential | Single file | 800-line budget; each file self-contained (own private seed helpers, like the precedent), independently buildable/testable in the same project. |
| Discovered bugs | **Characterize** current behavior; do not fix | Assert intended behavior | Non-goal per proposal — tests must stay green against unmodified services. |

## Data Flow

    Test → factory.Services.CreateScope() → GetRequiredService<IMatchService|IStageService>()
         → seed entities via ApplicationDBContext (SQLite) → call public method
         → assert on returned List<Match|Stage> and/or DB read-back

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Club12-Backend/API.Tests/MatchServiceGenerationTests.cs` | Create | a1 — `GenerateFixtureAsync` + `CreateAutomatedMatchesAsync` Group path (drives `ResolveGroupTeamCountAsync`, `CreateGroupStageMatchesAsync`, reachable `DistributeMatchDates`). ~400–550 lines. |
| `Club12-Backend/API.Tests/StageServiceTests.cs` | Create | a2 — `CreateAutomatedStagesAsync` + `AssignTeamsToStageAsync`. ~400–600 lines. |

Both use `IClassFixture<CustomWebApplicationFactory>`; no changes to the factory or production code.

## Interfaces / Contracts

No new interfaces. Consume `IMatchService.GenerateFixtureAsync(Guid, IEnumerable<Team>)`,
`IMatchService.CreateAutomatedMatchesAsync(Guid)`, `IStageService.CreateAutomatedStagesAsync(Guid)`,
`IStageService.AssignTeamsToStageAsync(Stage, List<Guid>?, bool)`.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Integration (a1) | Fixture rotation for 2/4/6 teams → `(N-1)*2` rounds; no self-pairing; no dup pairing per round; home/away swap round1↔round2; odd count & count<2 throw `ArgumentException`; Group match count `n*(n-1)/2`; `ResolveGroupTeamCount` throws (no groups / no teams / not-divisible / <2 per group); single-match midpoint date, multi-match spread, `end<start` throws | Seed via `ApplicationDBContext`, call public method, assert on returned matches + DB |
| Integration (a2) | Valid sizes 8/16/32/64; invalid & non-divisible-by-4 throw; group naming A/B/C…; date chaining/order; QuarterFinal present only when ≥16; already-has-stages & division-not-found throw; `AssignTeams` exact-fit / under / over-capacity throws / full-stage throws / duplicate filtering / auto mode | Same harness |
| Unit | — | Not used: predicate-driven repos + no mock library make direct fakes higher-risk than real DB |

### Bugs to CHARACTERIZE (assert current, buggy behavior — do NOT fix)

- **BUG-1 (`GenerateFixtureAsync`)**: generated `Match` objects set **no `StageId`** (stays `Guid.Empty`)
  and the `divisionId` parameter is **unused**. Tests assert `StageId == Guid.Empty` and that the
  division argument does not alter output.
- **BUG-2 (`GenerateFixtureAsync` dates)**: **all** first-round matches share one identical
  `MatchDate` (`DateTime.UtcNow`); the `AddDays(7 * firstRoundCount)` spacing only separates the
  round-1 block from the round-2 block, not matches within a round. Tests assert all round-1 matches
  share one date, all round-2 matches share a later date, gap = `7 * firstRoundCount` days.

### Documented (not a bug)
- `DistributeMatchDates` `matchCount <= 0` guard is **unreachable** through the public surface
  (callers pass computed positive counts). Cover reachable edges only; note the guard, do not force it.

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or
process-integration boundary. Pure test addition.

## Migration / Rollout

No migration required. Rollback = delete the added test file(s).

## Open Questions

- [ ] None blocking. Assumptions (black-box, odd-team throws, bugs logged-not-fixed) are resolved
      per proposal and confirmed against source.
