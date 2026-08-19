# Tasks: Backend build with 0 warnings

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~70-90 (14 files: 1 new + 13 modified, mostly 1-3 line edits) |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr |
| Chain strategy | size-exception |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Zero-warning solution build (all phases below) | PR 1 | `dotnet build Club12-Backend/Solution/Club12.sln --no-incremental` | `dotnet test Club12-Backend/Solution/Club12.sln` | Revert PR — `Directory.Build.props` deletion restores CS1591; all edits self-contained, no schema/API changes |

## Phase 1: Foundation

- [x] 1.1 Create `Club12-Backend/Directory.Build.props` with `<PropertyGroup><NoWarn>$(NoWarn);CS1591</NoWarn></PropertyGroup>`

## Phase 2: XML Doc-Comment Fixes (CS1572/1573/1574)

- [x] 2.1 `API/Controllers/MatchController.cs:26` — add `<param name="stageTeamMatchService">`
- [x] 2.2 `API/Controllers/TeamController.cs:28` — add `<param name="supabaseHelper">`; move `<param>` tags out of `<summary>`
- [x] 2.3 `API/Controllers/VenueController.cs:26` — add `<param name="supabaseHelper">`
- [x] 2.4 `Application/Interfaces/Services/IBlogPostService.cs` — L18 remove stray `<param name="userId">`; L32/33 rename `blogPostEntity`→`id`; L39 remove stray `<param name="userId">`
- [x] 2.5 `Application/Interfaces/Services/ITournamentService.cs:38/40` — rename `tournamentEntity`→`id`
- [x] 2.6 `Application/Interfaces/Services/IDivisionService.cs:19,38` — remove stray `<param name="userId">` at both sites
- [x] 2.7 `Application/Interfaces/Services/IVenueService.cs:39` — remove `<param name="filter">` (parameterless method)
- [x] 2.8 `Infrastructure/Backup/SupabaseBackupStorage.cs:85` — `<see cref="rawStorage"/>` → `<c>rawStorage</c>`

## Phase 3: Nullable-Reference Fixes (CS8600/8602/8604/8619)

- [x] 3.1 `API/AutoMapperProfiles/AutoMapperProfiles.cs:141,142` — `dest.HomeTeam!.Score`, `dest.VisitorTeam!.Score` in `ForPath` selectors (expression-tree `!`, per design)
- [x] 3.2 **[RECONCILED]** `API/AutoMapperProfiles/AutoMapperProfiles.cs:149,150` — real null guard on `src.HomeTeam`/`src.VisitorTeam` in `MapFrom` source lambdas, mirroring the existing `WinningTeam` guard at line 143 (`src.HomeTeam != null ? src.HomeTeam.Name : null`, same for VisitorTeam). NOT `!` — `MapFrom` lambdas are real runtime delegates, not structural expression-tree destination paths
- [x] 3.3 **[RECONCILED]** `Infrastructure/Repositories/ScorerRepository.cs:41` — `s.Match!.Stage.Division.TournamentId`: single `!` on `Match` only (genuinely nullable nav); `Stage`/`Division` are already `required` on their entities, no extra `!` needed
- [x] 3.4 `Application/Utils/Helper/SupabaseHelper/SupabaseHelper.cs:128/129` — `... .List(prefix) ?? new List<Supabase.Storage.FileObject>()` (real guard, SDK returns nullable list)
- [x] 3.5 **[RECONCILED]** `Application/Utils/Helper/SupabaseHelper/SupabaseHelper.cs:131` — real guard/fallback on `file.Name` (e.g. `?? string.Empty` or equivalent), NOT `file.Name!` — externally-deserialized API field, not structurally guaranteed non-null
- [x] 3.6 `Infrastructure/Identity/IdentityAppDbContextFactory.cs:34` — narrow `string[] extras` → `string?[] extras`
- [x] 3.7 `Infrastructure/Persistance/ApplicationDBContextFactory.cs:31` — same `string[]` → `string?[]` narrowing

## Phase 4: Verification

- [x] 4.1 Run `dotnet build Club12-Backend/Solution/Club12.sln --no-incremental` — confirm 0 errors, 0 warnings, 0 suggestions
- [x] 4.2 Run `dotnet test Club12-Backend/Solution/Club12.sln` — confirm no regressions vs. pre-change baseline

## Phase 5: Corrective — Test Coverage for RECONCILED Nullable-Guard Logic (from sdd-verify CRITICAL)

`sdd-verify` (Engram `sdd/backend-zero-warnings/verify-report`, id 622) found task 3.2's real null-guard logic (`AutoMapperProfiles.cs:149-150`, `Match` → `MinimalMatchResponse`, `HomeTeamName`/`VisitorTeamName`) had zero test coverage anywhere in the suite — a genuine spec-compliance gap (CRITICAL), since this is the one RECONCILED site with real new runtime branching logic (as opposed to an expression-tree-only or SQL-translated site).

- [x] 5.1 Add `Club12-Backend/API.Tests/AutoMapperProfilesTests.cs` — lightweight `MapperConfiguration`/`IMapper` unit tests (no `WebApplicationFactory` needed) against the real `MatchProfile`, covering both branches of the `HomeTeam`/`VisitorTeam` null guard on `Match` → `MinimalMatchResponse`:
  - `Map_ToMinimalMatchResponse_WithAssignedTeams_ResolvesRealTeamNames` — populated `HomeTeam`/`VisitorTeam` → `HomeTeamName`/`VisitorTeamName` resolve to the real team names
  - `Map_ToMinimalMatchResponse_WithUnassignedTeams_DegradesToNullInsteadOfThrowing` — null `HomeTeam`/`VisitorTeam` → `HomeTeamName`/`VisitorTeamName` resolve to `null`, no exception
- [x] 5.2 Re-run `dotnet build Club12-Backend/Solution/Club12.sln --no-incremental` — confirm still 0 errors, 0 warnings
- [x] 5.3 Re-run `dotnet test Club12-Backend/Solution/Club12.sln` — confirm 102/102 passing (100 baseline + 2 new), no regressions

### Discovery during 5.1 (non-blocking, informational)

Empirically reverted `AutoMapperProfiles.cs:149-150` from the guarded ternary back to `src.HomeTeam!.Name` / `src.VisitorTeam!.Name` and re-ran the new tests: **both still passed**, including the null-team case (no `NullReferenceException`). Root cause: AutoMapper's `MapFrom(Expression<Func<TSource,TMember>>)` overload structurally analyzes simple member-access-chain lambdas (`src.HomeTeam.Name`) and auto-inserts null-safety across the whole chain — the `!` null-forgiving operator emits no IL and doesn't change the expression tree, so it has no runtime effect here. This means the site was not actually at live NRE risk either way; the original CRITICAL's "real runtime delegate, `!` would throw" premise doesn't hold for this specific simple-member-chain shape. The reconciled explicit-guard code was restored (still correct, clearer intent, consistent with the `WinningTeam` precedent) and the new tests remain valid regression coverage for the spec-required "degrades gracefully" behavior regardless of the mechanism providing it.
