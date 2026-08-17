# Tasks: Cleanup — Mechanical / Behavior-Preserving Fixes (Batch 1, Backend)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~555 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (test+constants) → PR 2 (CS1998) → PR 3 (dead code) → PR 4 (param naming) |
| Delivery strategy | single-pr |
| Chain strategy | pending — user decision required |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

Estimate exceeds the 400-line budget (mostly the ~127-reference param-naming
slice). `single-pr` delivery strategy requires `size:exception` before apply
unless the user instead picks `stacked-to-main` or `feature-branch-chain`.

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Characterization test + `KnockoutMatchCount` constant extraction | PR 1 | `dotnet test --filter FullyQualifiedName~AutomatedMatchGenerationTests` | `CustomWebApplicationFactory` SQLite host (API.Tests) | Revert `MatchService.cs` const usage + delete new test/constant file |
| 2 | CS1998 fix (3 methods, `Task.FromResult`) | PR 2 | `dotnet build Club12-Backend/Solution/Club12.sln` (zero CS1998) + rerun Unit 1 test | Same harness, no seed changes | Revert `MatchService.cs` async-signature diff only |
| 3 | Bearer scheme constant + dead code removal (Match/Team controllers, MatchService unused vars) | PR 3 | `dotnet test Club12-Backend/Solution/Club12.sln` (full suite) | Existing `SmokeTests` + auth flow smoke | Revert per-file; no cross-file coupling |
| 4 | Param naming normalization, 10 controllers | PR 4 | `dotnet build` (zero new warnings) + `dotnet test` full suite | Existing controller integration tests | Revert per-controller file independently |

## Phase 1: Characterization Test (RED, TDD-first)

- [x] 1.1 Create `API.Tests/AutomatedMatchGenerationTests.cs`: seed `Stage` rows (QuarterFinal, SemiFinal, Final, ThirdPlace) via `CustomWebApplicationFactory` + `ApplicationDBContext` scope, invoke `CreateAutomatedMatchesAsync`, assert QF→4 matches, SF→2, Final/ThirdPlace→1, `Type=Playoff`, dates within `[StartDate,EndDate]` — against **current pre-refactor literals**.
- [x] 1.2 Run the test, confirm GREEN against current code (characterizes existing behavior before touching `MatchService.cs`). — 4/4 passed.

## Phase 2: Constants Extraction (satisfies Knockout Match Count Constants requirement)

- [x] 2.1 Create `Application/Utils/Constants/Stage/KnockoutMatchCount.cs`: `public static class KnockoutMatchCount { public const int QUARTER_FINAL = 4; public const int SEMI_FINAL = 2; }` with XML docs, mirroring `MaxTeams.cs` structure/namespace.
- [x] 2.2 In `Application/Services/MatchService.cs:288-290`, replace literals `4`/`2` with `KnockoutMatchCount.QUARTER_FINAL` / `KnockoutMatchCount.SEMI_FINAL`.
- [x] 2.3 Add unit assertions `Assert.Equal(4, KnockoutMatchCount.QUARTER_FINAL)` / `Assert.Equal(2, KnockoutMatchCount.SEMI_FINAL)` to `AutomatedMatchGenerationTests.cs`.
- [x] 2.4 Re-run Phase 1 test — MUST stay GREEN with identical counts (proves equivalence against named constants, not a re-hardcoded literal). — 5/5 passed.

## Phase 3: CS1998 Warning Elimination

- [x] 3.1 In `Application/Services/MatchService.cs:268-311`, remove `async` from `CreateGroupStageMatchesAsync`, `CreateKnockoutStageMatchesAsync`, `CreateFinalStageMatchesAsync`; keep `Task<List<Match>>` return type; wrap final value in `return Task.FromResult(matches)`.
- [x] 3.2 Confirm call sites (`CreateAutomatedMatchesAsync` switch) are untouched — they still `await` these calls normally.
- [x] 3.3 `dotnet build Club12-Backend/Solution/Club12.sln` — zero CS1998 warnings in touched files, zero new errors/warnings. — NOTE: baseline build already had 0 CS1998 warnings in this environment/SDK (confirmed via isolated repro); fix applied anyway per design decision, remains a valid simplification.
- [x] 3.4 Re-run Phase 1/2 test suite — MUST stay GREEN (return values/exceptions unchanged). — 5/5 passed.

## Phase 4: Dead Code Removal

- [x] 4.1 Delete commented block `API/Controllers/MatchController.cs:215-358`.
- [x] 4.2 Delete dead method `API/Controllers/TeamController.cs:190-251`.
- [x] 4.3 Remove unused computed values `playerStats`, `homeScorers`, `awayScorers` in `Application/Services/MatchService.cs:33-95`; keep DB `includes` that populate the returned `match`.
- [x] 4.4 `dotnet build` — zero new warnings/errors; confirm no route/verb/signature changed. Build succeeded 0 errors (also removed 2 pre-existing CS8602 warnings that lived in the deleted dead code).

## Phase 5: Auth Scheme Constant

- [x] 5.1 In `API/Utils/StartupExtensions.cs:160-161`, replace both `"Bearer"` literals with `JwtBearerDefaults.AuthenticationScheme` (add `using Microsoft.AspNetCore.Authentication.JwtBearer;` if not already present).
- [x] 5.2 Confirm configured scheme value still equals `"Bearer"` (framework constant identity) — app builds and starts (SmokeTests boots real host and passes).

## Phase 6: Controller Parameter Naming Normalization (10 controllers, 127 references)

- [x] 6.1 `VenueController.cs`: rename `_`-prefixed primary-ctor params + internal usages + matching `<param name="_x">` XML docs to no-underscore.
- [x] 6.2 `UserController.cs` reference pattern confirmed as target convention (no change needed — already compliant).
- [x] 6.3 `TournamentController.cs`: same rename pass.
- [x] 6.4 `PlayerStatisticController.cs`: same rename pass.
- [x] 6.5 `PlayerSanctionController.cs`: same rename pass.
- [x] 6.6 `PlayerController.cs`: same rename pass.
- [x] 6.7 `DivisionController.cs`: same rename pass.
- [x] 6.8 `BlogPostController.cs`: same rename pass.
- [x] 6.9 `StageController.cs`: same rename pass.
- [x] 6.10 `MatchController.cs` + `TeamController.cs`: same rename pass (post dead-code removal, so line numbers are stable).
- [x] 6.11 `dotnet build` — zero new warnings/errors across all 10 files.

## Phase 7: Full Verification

- [x] 7.1 `dotnet test Club12-Backend/Solution/Club12.sln` — full suite, all previously passing tests still pass, new `AutomatedMatchGenerationTests` passes. — 6/6 passed (1 SmokeTest + 5 new).
- [x] 7.2 `dotnet build Club12-Backend/Solution/Club12.sln` — zero new errors/warnings, zero CS1998 in touched files. — Clean rebuild: 411 warnings (down from 412 baseline), 0 errors, 0 CS1998.
- [x] 7.3 Diff review: confirm no controller route, HTTP verb, DTO shape, or public method signature changed anywhere. — Confirmed via `git diff`: only constructor parameter identifiers, internal usages, dead-code deletions, and internal literal→constant swaps changed; no `[Http*]`/`[Route]` attributes, DTO types, or public method signatures touched.
