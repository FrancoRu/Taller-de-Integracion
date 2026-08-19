# Design: Cleanup — Mechanical / Behavior-Preserving Fixes (Batch 1, Backend)

## Technical Approach

Pure refactor of `Club12-Backend`. Group edits into five independently-buildable, independently-revertible concern-slices (constants → CS1998 → dead code → naming → tests), each compiler- and smoke-test-verified. No DTO, route, controller signature, or DB schema is touched. Verification runs against the existing `API.Tests` SQLite host harness plus one new equivalence test for knockout/automated match generation.

## Architecture Decisions

### Decision: `"Bearer"` scheme string

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Custom `Application.Utils.Constants` class | Matches proposal wording; new symbol duplicating a framework value | Rejected |
| `JwtBearerDefaults.AuthenticationScheme` (already-referenced framework const = `"Bearer"`) | Idiomatic, zero drift risk, no new file | **Chosen** |

Both literals at `StartupExtensions.cs:160-161` become `JwtBearerDefaults.AuthenticationScheme`. This is a refinement of the proposal's "use Constants convention" assumption — the framework already provides the named constant.

### Decision: knockout match-count numbers `4`/`2`

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Derive `MaxTeams.QUARTER_FINAL / 2` | Semantically true (teams/2=matches) but introduces a magic `2` divisor | Rejected |
| New `Application/Utils/Constants/Stage/KnockoutMatchCount.cs` with `QUARTER_FINAL=4; SEMI_FINAL=2;` | Literal-preserving; mirrors `MaxTeams.cs` structure (namespace `Application.Utils.Constants.Stage`, `static class`, `public const int`, XML docs) exactly | **Chosen** |

Guarantees identical values; follows the established per-category constant-file convention precisely.

### Decision: CS1998 fix (three methods)

Inspected each body at `MatchService.cs:268-311`. `CreateGroupStageMatchesAsync`, `CreateKnockoutStageMatchesAsync`, `CreateFinalStageMatchesAsync` are **all purely synchronous** (list building + sync `DistributeMatchDates`/`BuildMatch`); no genuine `await` was dropped. Fix = remove `async`, keep `Task<List<Match>>` return, `return Task.FromResult(matches)`. Chosen over "make synchronous + drop `await` at call sites" because the call site (`CreateAutomatedMatchesAsync` switch) still genuinely awaits `ResolveGroupTeamCountAsync`; keeping the `Task` return leaves all call sites untouched → smaller, lower-risk diff.

### Decision: unused computed values `MatchService.cs:33-95`

`playerStats`, `homeScorers`, `awayScorers` are dead (commented projection bodies; never assigned to or returned; method returns `match`). Remove all three. DB `includes` remain (they populate the returned `match`). Behavior-preserving.

### Decision: param-naming normalization ordering

One mechanical pass, edited per-controller. Primary-constructor params are file-local, so each controller renames independently and stays buildable on its own — a per-file (not global) rename is safe and reviewable. Update matching `<param name="_x">` XML-doc references too.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Application/Utils/Constants/Stage/KnockoutMatchCount.cs` | Create | `QUARTER_FINAL=4; SEMI_FINAL=2;` mirroring `MaxTeams.cs` |
| `API/Utils/StartupExtensions.cs` | Modify | 2× `"Bearer"` → `JwtBearerDefaults.AuthenticationScheme` |
| `Application/Services/MatchService.cs` | Modify | Knockout consts; CS1998 → `Task.FromResult`; delete dead `playerStats/homeScorers/awayScorers` |
| `API/Controllers/MatchController.cs` | Modify | Delete commented block `215-358`; normalize params |
| `API/Controllers/TeamController.cs` | Modify | Delete commented method `190-251`; normalize params |
| `API/Controllers/{Venue,User,Tournament,PlayerStatistic,PlayerSanction,Player,Division,BlogPost,Stage}Controller.cs` | Modify | Normalize `_`-prefixed primary-ctor params to no-underscore |
| `API.Tests/AutomatedMatchGenerationTests.cs` | Create | Equivalence test (see Testing) |

## Interfaces / Contracts

None changed. No new public interface, DTO, route, or entity. Constants are internal implementation detail; renames are parameter-local.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Integration | `CreateAutomatedMatchesAsync` yields identical counts/types/dates after constant extraction | New test resolves `IMatchService` + `ApplicationDBContext` from the `CustomWebApplicationFactory` scope, seeds a `Stage` (QuarterFinal, SemiFinal, Final, ThirdPlace) via DbContext, asserts QF→4, SF→2, Final/ThirdPlace→1 matches, `Type=Playoff`, dates within `[StartDate,EndDate]` |
| Unit | Constant literal equality | `Assert.Equal(4, KnockoutMatchCount.QUARTER_FINAL)`, `Assert.Equal(2, KnockoutMatchCount.SEMI_FINAL)` |
| Smoke | Harness still boots; no regression | Existing `SmokeTests`; full `dotnet test Club12.sln` |

Reuses the established real-host + in-memory-SQLite harness (not a new mocking dependency), matching existing test philosophy.

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary. Auth-scheme edit swaps a literal for its framework-defined constant of identical value.

## Migration / Rollout

No migration required. No data/schema/runtime-config change. Reversible via single-PR `git revert`.

## Contract Confirmation

- **Zero** DTO / route / controller-signature / API-contract changes (constants internal; renames parameter-local; deleted code was commented or discarded in-memory).
- **Zero** DB schema changes (no entity, `DbContext`, or migration touched).

## Open Questions

- [ ] None blocking. Proposal assumption to place Bearer under `Application.Utils.Constants` is refined here to the framework constant `JwtBearerDefaults.AuthenticationScheme`; flag for confirmation at apply.
