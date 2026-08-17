# Tasks: Structural Refactor — Auth Boundary (Slice A only)

## Scope Note

Honest combined estimate: Slice A (backend) ≈ 150-220 changed lines. Slice B
(`TeamsPage.tsx` decomposition) alone ≈ 900-1050 changed lines (602-line
source split into 4 new files + container rewrite + full RTL behavior suite
over filter/debounce/pagination/create/edit/delete) — already over the
800-line budget by itself before adding Slice A. Per design's own note
("A and B are independent"), **this change is scoped to Slice A only**.
Propose a sibling change `refactor-teamspage-decomposition` for Slice B,
reusing `teamspage-decomposition/spec.md` and the Slice-B design section
already written; that change's own `sdd-tasks` pass should further chain/stack
its PRs given the ~900-1050 estimate.

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 150-220 (Slice A only) |
| 800-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr |
| Chain strategy | size-exception (not needed — well under budget) |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | `LogoutAsync` boundary fix, backend only | PR 1 | `dotnet test API.Tests --filter Logout` | `CustomWebApplicationFactory` in-process host | Revert 3 files restores direct `UserManager` injection |

## Phase 1: RED — Regression Tests (write first, must fail)

- [x] 1.1 Add `API.Tests/AuthControllerLogoutTests.cs`: seed a user with non-null `RefreshToken`/`RefreshTokenExpiryTime` via `CustomWebApplicationFactory`, call `POST api/auth/logout` with that user's id claim, assert `204` and both fields persisted `null`.
- [x] 1.2 Same file: missing-user case — id claim with no matching user, assert `204` and no `UpdateAsync` call/persistence change.
- [x] 1.3 Run `dotnet test API.Tests --filter Logout` — confirm both fail (method doesn't exist yet / controller still uses `UserManager` directly).
  - **Deviation**: this is a characterization/regression test of *existing* observable HTTP behavior (design.md's "Regression test locks response + side effect"), not a fail-first test for a not-yet-existing method — `Controller Layer Isolation` is a static/compile-time property, not a runtime one. Run against pre-refactor code it legitimately PASSED (2/2) on the first attempt (after fixing a test-harness bug: a stale-DbContext read using the same scoped `UserManager` instance across the HTTP call was reading cached in-memory state instead of the persisted row — fixed by using a fresh `IServiceScope` to verify post-request state). This is expected and correct for a characterization test: it must pass before AND after the refactor to prove behavior preservation, per design.md's testing strategy.
- [x] 1.3b Re-ran after fixing the harness scope bug — 2/2 pass against pre-refactor code (confirms the test correctly characterizes current behavior before changing anything).

## Phase 2: GREEN — Boundary Implementation

- [x] 2.1 `Application/Interfaces/Services/IAuthenticationService.cs`: add `Task LogoutAsync(Guid userId, CancellationToken ct = default)` with XML doc.
- [x] 2.2 `Infrastructure/Identity/IdentityAuthenticationService.cs`: implement `LogoutAsync` — move `AuthController.Logout` lines 99-105 verbatim (`FindByIdAsync` → clear `RefreshToken`/`RefreshTokenExpiryTime` → `UpdateAsync`; missing user = no-op).
- [x] 2.3 `API/Controllers/AuthController.cs`: drop `UserManager<ApplicationUser> userManager` ctor param, remove `using Infrastructure.Identity;` and `using Microsoft.AspNetCore.Identity;`; `Logout` becomes `await authenticationService.LogoutAsync(id, ct); return NoContent();`.
- [x] 2.4 Run `dotnet test API.Tests --filter Logout` — confirm both pass. (2/2 passed post-refactor, identical to pre-refactor result — proves zero behavior change.)

## Phase 3: Verify / Cleanup

- [x] 3.1 `dotnet build` — confirm no unused-using warnings on `AuthController.cs`. (0 errors, 322 pre-existing CS1591/CS1573/CS8602 warnings unrelated to this change; none reference unused usings in `AuthController.cs`.)
- [x] 3.2 Grep `AuthController.cs` for `UserManager`/`ApplicationUser`/`Infrastructure.Identity` — confirm zero matches (locks Controller Layer Isolation requirement). (Confirmed: zero matches.)
- [x] 3.3 Run full `dotnet test API.Tests` — confirm no regression in other `AuthController` actions (unchanged). (Full suite: 18/18 passed, 0 failed.)
- [ ] 3.4 Orchestrator: open sibling SDD change `refactor-teamspage-decomposition` for Slice B (reuse existing `teamspage-decomposition/spec.md` + design's Slice-B section) before this PR merges or right after. — **Not applicable to sdd-apply; orchestrator-owned action, left for orchestrator.**
