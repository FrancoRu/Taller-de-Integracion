# Proposal: Cleanup — Mechanical / Behavior-Preserving Fixes (Batch 1, Backend)

## Intent

Step (ii) of the archived clean-architecture audit program: apply behavior-preserving
cleanup now that test scaffolding exists (Strict TDD unblocked). This batch targets the
**backend only** (`Club12-Backend`). No functional, API, or contract change.

**Split decision (budget-driven):** A realistic estimate of the full backend+frontend
scope is ~670–955 changed units (add+deletions), exceeding the 800-line single-PR budget.
The dominant, review-risky item is backend param-naming (127 references). Backend and
frontend are cleanly separable (distinct repos, test runners, PRs). Therefore this scope
is **split**: `batch1` = backend (this proposal, ~555 units, fits 800); `batch2` = frontend
(deferred sibling, ~400 units), to be proposed next. This avoids silently exceeding budget.

## Scope

### In Scope (backend, `Club12-Backend`)
- Extract magic strings/numbers to named constants: `"Bearer"` scheme (`API/Utils/StartupExtensions.cs:160-161`); knockout numbers `4`/`2` (`Application/Services/MatchService.cs:288-290`) via existing `Application/Utils/Constants/{Stage/MaxTeams,Scorer/ScoreConstants}.cs` convention.
- Remove dead code: commented block `API/Controllers/MatchController.cs:215-358`; dead method `API/Controllers/TeamController.cs:190-251`; unused computed values `Application/Services/MatchService.cs:33-95`.
- Fix `async` methods with no `await` (CS1998) in `MatchService.cs:268-311`.
- Normalize primary-constructor parameter naming to the newer no-underscore convention (`UserController`/`AuthController`) across the 10 older controllers (127 `_`-prefixed references).

### Out of Scope (Non-Goals)
- **All frontend cleanup → deferred to `cleanup-mechanical-fixes-batch2`**: brand colors → `theme.ts`, query-key factories, `axiosUtils.ts:11` `INVALID_TOKEN_PATH`, dead i18n files (`languajes/*.ts`). Recorded decision for batch2: **remove the empty i18n files as dead scaffolding** (wiring i18n is a feature, out of mechanical scope).
- Deferred behavior changes (later slices): 400→404 status codes, `sendGet` 401-pipeline, `AuthController`→`UserManager` boundary leak, `TeamsPage` container/presentational split, `FormData` field-name contract, scheduled backups.

## Capabilities

### New Capabilities
- None (pure refactor; no product behavior introduced).

### Modified Capabilities
- None (no spec-level behavior change).

## Approach

Behavior-preserving edits verified against existing smoke tests (`dotnet test Club12-Backend/Solution/Club12.sln`). Add narrow equivalence tests where cheap: the extracted `"Bearer"` constant yields the same auth scheme; knockout constants equal prior literals. Constants reuse the existing `Application/Utils/Constants` folder pattern. Param renames are mechanical symbol renames within each controller (declaration + usages), no signature/route change.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `API/Utils/StartupExtensions.cs` | Modified | `"Bearer"` → constant |
| `Application/Services/MatchService.cs` | Modified | Knockout constants, remove unused values, CS1998 fix |
| `API/Controllers/*.cs` (10 files) | Modified | Remove dead code; normalize param naming |
| `Application/Utils/Constants/` | New/Modified | Auth scheme + knockout constants |
| `Club12.Tests` | New | Narrow equivalence tests |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Param rename misses a reference | Low | Compiler catches unresolved symbols; smoke tests |
| Deleting live-looking commented code | Low | Confirmed dead (commented/unreachable) in audit |
| Batch1+2 combined exceeds budget if merged | Med | Explicit split; each batch its own PR |

## Rollback Plan

All edits are localized and reversible via `git revert` of the single PR. No data, schema, or runtime-config change; reverting restores prior source with no migration.

## Dependencies

- Test scaffolding from archived `codebase-clean-architecture-audit` (already delivered).

## Success Criteria

- [ ] `dotnet test Club12-Backend/Solution/Club12.sln` green (existing + new equivalence tests).
- [ ] Zero API/route/contract change; no CS1998 warnings remain in touched files.
- [ ] All 127 param references normalized to no-underscore convention.
- [ ] PR under 800 changed lines; frontend `batch2` proposal filed as sibling.

## Proposal question round (assumptions needing user review)

As a delegated executor I cannot ask interactively; surfacing key assumptions:
1. **Split accepted?** batch1=backend, batch2=frontend (proposed next). If a single PR is preferred, the 800 budget must be raised or a `size:exception` accepted.
2. **Naming target** = newer no-underscore convention (`userManagementService`), matching User/Auth controllers. Confirm this is the intended canonical style.
3. **i18n decision** (batch2) = remove dead empty files rather than wire i18n. Confirm removal is acceptable.
