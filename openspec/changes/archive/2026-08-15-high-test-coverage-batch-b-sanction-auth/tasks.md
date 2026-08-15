# Tasks: High Test Coverage — Batch B (Sanction Appeal + Auth JWT)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~300–420 (b1 ~210–270, b2 ~90–150); additions only, two new files, zero deletions |
| Session review budget | 800 changed lines (overrides generic 400 default) |
| 400-line budget risk | Low (also Low against the real 800-line session budget) |
| Chained PRs recommended | No |
| Suggested split | Single PR containing two independently-revertible sub-slices (b1, b2) |
| Delivery strategy | single-pr |
| Chain strategy | pending (not needed — estimate stays under both thresholds) |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | b2: characterize `AuthService.GenerateJwtTokenAsync` | PR 1 (single PR) | `dotnet test --filter FullyQualifiedName~AuthServiceJwtTests` | N/A — pure unit test, no host/DB | Delete `AuthServiceJwtTests.cs` |
| 2 | b1: characterize sanction appeal state machine | PR 1 (single PR) | `dotnet test --filter FullyQualifiedName~PlayerSanctionAppealTests` | `CustomWebApplicationFactory` in-process HTTP harness (SQLite in-memory) | Delete `PlayerSanctionAppealTests.cs` |

**Strict TDD note (characterization, not new-feature TDD):** every test in both phases MUST pass against the current, unmodified production code on first run. A failing test signals a spec/design mismatch to fix in the test — never a production-code change (both slices are test-only; zero production files touched).

## Phase 1: b2 — Auth JWT Generation (unit, no DB)

- [x] 1.1 Create `Club12-Backend/API.Tests/AuthServiceJwtTests.cs`; add `BuildConfig()` helper via `ConfigurationBuilder().AddInMemoryCollection(...)` for `JWT:Key/Issuer/Audience`; instantiate `new AuthService(config)` directly.
- [x] 1.2 Test: access token claims include user id + role(s) — spec `Access Token Claims and Expiry` / "Token carries expected claims".
- [x] 1.3 Test: token expiry ≈ `UtcNow.AddHours(24)` and `TokenResponse.ExpiresIn == TimeSpan.FromHours(24)` — "Token expiry is genuinely 24 hours from issuance".
- [x] 1.4 Test: access token round-trips via `JwtSecurityTokenHandler.ValidateToken` with matching `TokenValidationParameters`; assert issuer/audience — `Access Token Signature Verifiability`.
- [x] 1.5 Test: two `GenerateJwtTokenAsync` calls yield different `RefreshToken` values — `Refresh Token Uniqueness`.
- [x] 1.6 Run `dotnet test --filter FullyQualifiedName~AuthServiceJwtTests`; confirm all 4 pass unmodified (characterization check). — 4/4 passed, 45ms.

## Phase 2: b1 — Sanction Appeal Workflow (integration via `CustomWebApplicationFactory`)

- [x] 2.1 Create `Club12-Backend/API.Tests/PlayerSanctionAppealTests.cs` implementing `IClassFixture<CustomWebApplicationFactory>`, mirroring the `NotFoundContractTests`/`AutomatedMatchGenerationTests` constructor + `HttpClient` pattern.
- [x] 2.2 Add private `SeedSanctionAsync(ApplicationDBContext db, SanctionAppealStatus status)` seeding Team→Player, Tournament→Division→Stage→Match, and `PlayerSanction` in the given status, mirroring `SeedStageAsync`'s object-graph depth (required FKs enforced by SQLite).
- [x] 2.3 Test: appeal blocked (400) when `AppealStatus == Pending`, status unchanged — `Appeal Submission Guard` / "Appeal blocked while already pending".
- [x] 2.4 Test: appeal from `None` succeeds (200), fresh-scope re-read shows persisted `AppealStatus == Pending` — "Appeal succeeds from no prior appeal".
- [x] 2.5 Test: appeal against missing sanction id returns 404 — "Appeal against a missing sanction".
- [x] 2.6 Test: resolve blocked (400) when `AppealStatus != Pending` (cover `None`/`Accepted`/`Rejected`) — `Appeal Resolution Guard` / "Resolution blocked when not pending".
- [x] 2.7 Test: resolve with accept decision from `Pending` persists `Accepted` (fresh-scope re-read) — "Resolution accepts a pending appeal".
- [x] 2.8 Test: resolve with reject decision from `Pending` persists `Rejected` (fresh-scope re-read) — "Resolution rejects a pending appeal".
- [x] 2.9 Test: resolve against missing sanction id returns 404 — "Resolution against a missing sanction".
- [x] 2.10 Run `dotnet test --filter FullyQualifiedName~PlayerSanctionAppealTests`; confirm all 7 pass unmodified (characterization check). — 9/9 passed (7 scenarios; the "not pending" resolve guard expands into a 3-case Theory), 1s.

## Phase 3: Verification

- [x] 3.1 Run full `dotnet test` for `API.Tests` to confirm no regressions from either new file. — 106/106 passed, 1s (includes concurrently-added Batch A tests; no regressions).
- [x] 3.2 Diff-review both files' authored line counts against the 800-line session review budget; confirm combined total lands within the ~300–420 estimate. — AuthServiceJwtTests.cs 126 lines, PlayerSanctionAppealTests.cs 284 lines; combined 410 authored lines (`wc -l`), within the ~300–420 estimate and well under the 800-line session budget.

## STATUS: 18/18 tasks complete. All done.
