# Design: High Test Coverage — Batch C (remaining medium-value logic)

## Technical Approach

Add three xUnit characterization test files under `Club12-Backend/API.Tests/`, each an
`IClassFixture<CustomWebApplicationFactory>` that resolves the real service/repository from a DI
scope, seeds through the real `ApplicationDBContext` (in-memory SQLite), exercises the target
method, and asserts observed behavior. Identical discipline to Batch A/B: no mocking library, no
fakes, zero production changes. Rollback = delete the three files.

The one cross-cutting design problem is **SQLite (test harness) vs Npgsql (production) query
translation divergence**. This is resolved entirely by *controlling test data*, never by editing
production code or the harness.

## Architecture Decisions

### Decision: Resolve provider-translation risk via data shaping, not code/harness change

| Concern | Provider risk | Mitigation |
|---|---|---|
| `IssuedDate.AddDays(Duration)` date math | SQLite stores DateTime as ISO-8601 TEXT; local/fractional kinds make `<=` ambiguous | Seed **UTC, whole-second** `DateTime` literals; `Duration` in whole days so expiry lands on exact midnight; boundary case sets `cutoff` == expiry instant |
| `ToUpper` on FullName | SQLite `upper()` is ASCII-only; differs from Npgsql UPPER on accented chars | **ASCII-only** seeded names so `upper()` == UPPER |
| Null concat (`FirstName + " " + SecondName`, SecondName nullable) | Raw SQL `x \|\| NULL = NULL`; .NET treats null as empty | EF SQLite provider already emits `COALESCE(...,'')`; test asserts **.NET semantics** with one null-SecondName and one populated row |
| Subquery `Sum` of scores | SQL `SUM(∅) = NULL` | EF wraps non-nullable `.Sum()` in `COALESCE(...,0)`; assert **zero-score → 0** |

**Alternatives considered**: (a) switch the failing file to the EF InMemory provider — rejected: it
does not characterize real query translation and breaks the repo's single-harness convention;
(b) add production overloads/`AsEnumerable` client-eval to dodge translation — rejected: out of
scope, would mutate production behavior.
**Rationale**: data shaping keeps tests honest against the real SQL pipeline while sidestepping the
only genuine ASCII/date ambiguities. If a query proves **untranslatable in SQLite** (runtime throw),
document it as a harness limitation in the test file and a follow-up note — do **not** touch
production.

### Decision: Mirror Batch A file/harness conventions exactly

**Choice**: One file per behavior, service-named, with local private seed helpers; verify Team
mutations by **re-querying** the context in a fresh scope; run focused via
`dotnet test Club12-Backend/API.Tests --filter FullyQualifiedName~<Class>`.
**Alternatives**: shared seed base class — rejected as premature for 3 files.
**Rationale**: consistency with shipped `MatchServiceGenerationTests.cs` / `StageServiceTests.cs`.

## Data Flow

    Test → CustomWebApplicationFactory → DI scope → real Service/Repo → ApplicationDBContext (SQLite)
      │                                                                          │
      └──────────── seed (ASCII, UTC whole-second) ─── assert / re-query ────────┘

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Club12-Backend/API.Tests/PlayerSanctionServiceTests.cs` | Create | `GetExpiredSanctionsAsync` inclusive-boundary tests |
| `Club12-Backend/API.Tests/TeamServiceRegisterTests.cs` | Create | `RegisterTeamsToTournamentAsync` add/unassign/keep/reassign/empty diff |
| `Club12-Backend/API.Tests/ScorerRepositoryTests.cs` | Create | `GetPlayerScoresAsync` aggregation/order/pagination/filters/formatting |

## Test Scenarios

**PlayerSanctionServiceTests** (`GetExpiredSanctionsAsync`, verified at
`PlayerSanctionService.cs:48-53`): before-expiry excluded; exactly-at-cutoff **included** (inclusive
`<=`); after-expiry included; empty set → empty; returned entity has `Player` navigation populated
(`.Include(s => s.Player)`). All seeds UTC whole-second, whole-day `Duration`.

**TeamServiceRegisterTests** (`RegisterTeamsToTournamentAsync`): add new team to tournament;
unassign a dropped team; keep an already-registered team unchanged; reassign a team from another
tournament; empty list unassigns all. Assert by re-querying `TournamentId` per team. ASCII names,
deterministic GUIDs. (FK-only mutation → negligible provider risk.)

**ScorerRepositoryTests** (`GetPlayerScoresAsync`): `Sum` aggregation; descending order; pagination;
Tournament/Team/Match/Player filters; FullName formatting with and without `SecondName` (null →
no trailing separator); zero-score defaults to 0. ASCII-only seed data.

## Testing Strategy

| Layer | What | Approach |
|---|---|---|
| Integration | all 3 behaviors | real service + real `ApplicationDBContext` (SQLite) via `CustomWebApplicationFactory` |
| Unit/E2E | — | N/A (repo pattern is integration-only) |

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or
process-integration boundary. Test-only characterization.

## Migration / Rollout

No migration. Delivery: single PR, forecast ~360–490 authored lines (< 800-line budget).

## Open Questions

- [ ] **Blocking for apply — branch target.** This worktree is on `main`, which lacks the
  `Club12-Backend/API.Tests` project/`CustomWebApplicationFactory`, `TeamService.RegisterTeamsToTournamentAsync`,
  and `ScorerRepository.GetPlayerScoresAsync` (verified: `TeamService.cs` has only CRUD; scorer logic
  on main is the in-memory `PlayerStatisticService.GetTopScorersByDivision`). The harness and both
  methods exist on `develop` (where Batches A/B shipped). **Apply must target `develop`, not `main`.**
- [ ] Confirm at apply that EF SQLite translates `AddDays(column)` for the sanction predicate; if it
  throws, document the harness limitation in-file (no production change).
- [ ] `sortPositions()` remains out of scope (empty `positions` producer); logged as a follow-up gap.
