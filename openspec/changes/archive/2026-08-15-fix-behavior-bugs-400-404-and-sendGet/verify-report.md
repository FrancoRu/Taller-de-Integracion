```yaml
schema: gentle-ai.verify-result/v1
evidence_revision: sha256:re-verify-2026-08-15-corrective-pass
verdict: pass-with-warnings
blockers: 0
critical_findings: 0
requirements: 7/7
scenarios: 9/9
test_command: dotnet test Club12-Backend/Solution/Club12.sln && npm run test (Club12-WebClient)
test_exit_code: 0
build_command: dotnet build Club12-Backend/Solution/Club12.sln
build_exit_code: 0
```

## Verification Report (RE-VERIFICATION — corrective pass)

**Change**: fix-behavior-bugs-400-404-and-sendGet
**Mode**: Strict TDD, full spec-driven re-verification of the single prior CRITICAL only (all other dimensions previously PASS and reconfirmed unchanged)
**Prior verify**: Engram id 585 — FAIL, 1 CRITICAL (Requirement 1 Scenario 2 "PUT/DELETE or nested action against nonexistent parent returns 404" had zero runtime test coverage)
**Corrective apply**: Engram id 580 (revision 2) — claims 3 new tests added, 21/21 passing

### Independent Test Execution (re-run myself, not trusted from apply-progress)
- `dotnet test Club12-Backend/Solution/Club12.sln` -> **21/21 passed**, 0 failed. Matches claim.
  - Delta from prior 16: +3 corrective tests (Division PUT, Tournament nested-action, Team DELETE) + 2 pre-existing `AuthControllerLogoutTests` from the unrelated sibling change (`structural-refactor-auth-boundary-and-teamspage`), correctly out of this change's scope.
- `npm run test` (Club12-WebClient) -> **42/42 passed** (16 files), no regression.
- `dotnet build Club12-Backend/Solution/Club12.sln` -> succeeded, 0 errors.

### New Test Methods — Read Directly, Confirmed Non-Trivial
Read `Club12-Backend/API.Tests/NotFoundContractTests.cs` in full.

| Test | Real call | Layer | Assertion |
|---|---|---|---|
| `UpdateDivisionById_MissingEntity_Returns404ProblemDetails` | `client.PutAsJsonAsync("api/divisions/{id}", ...)` against `Guid.NewGuid()` | Full-host (`CustomWebApplicationFactory`) | 404 + `application/problem+json` + status/title/detail/traceId |
| `RegisterTeam_MissingTournament_Returns404ProblemDetails` | `client.PostAsJsonAsync("api/tournaments/register-teams/{id}", ...)` against nonexistent tournament id | Full-host | Same ProblemDetails assertion |
| `TeamController_DeleteTeamById_MissingEntity_Returns404ProblemDetails` | `controller.DeleteTeamById(id)` direct-controller call | Direct-controller unit test (same `null!`-SupabaseHelper degradation pattern already accepted for Team/Venue GET) | `ObjectResult` 404 + `ProblemDetails` shape |

Cross-checked production code directly:
- `DivisionController.UpdateDivisionById` -> `return this.NotFoundProblem(nameof(Division), id);`
- `TournamentController.RegisterTeam` -> `return this.NotFoundProblem(nameof(Tournament), id);`
- `TeamController.DeleteTeamById` -> `return this.NotFoundProblem(nameof(Team), id);`

All three tests exercise real production not-found branches, not stubs or trivial assertions.

### Nested-Action Scenario Confirmation
Re-read spec `api-not-found-semantics` Requirement 1 Scenario 2 verbatim: "WHEN a client sends PUT/DELETE for that id, or a nested action referencing it as parent." `RegisterTeam` takes the Tournament id as the parent/`id` route parameter and performs a nested sub-resource action (registering a list of teams) against it — this is a genuine nested-action-against-nonexistent-parent case, directly analogous to the spec's own cited example ("adding a sanction to a nonexistent player"). Confirmed this test satisfies the scenario.

### Production Code Regression Check
All 3 corrective-pass files are untracked (no intermediate commit boundary exists to isolate this pass via `git diff` alone), so corroborated via file mtimes plus content diff:
- Only `NotFoundContractTests.cs` and `tasks.md` carry corrective-pass-window timestamps.
- All other controllers (Match/Player/PlayerSanction/PlayerStatistic/BlogPost/Team/Tournament/Venue) predate the corrective window — untouched.
- `DivisionController.cs` has a later mtime (consistent with the claimed temporary-revert-then-restore RED check), but `git diff` against HEAD shows content byte-identical to the expected original-pass ADD-not-REPLACE fix (`BadRequest(...)` -> `this.NotFoundProblem(nameof(Division), id)`), nothing left broken. Confirms clean restoration — net-zero production change from this corrective pass.
- Conclusion: **no production code changed in this corrective pass**, matches apply-progress's claim.

### Spec Compliance Matrix (updated)

#### api-not-found-semantics (4 requirements, 5 scenarios)
| Requirement | Scenario | Test | Result |
|---|---|---|---|
| Not-Found Status | GET by nonexistent ID -> 404 | `GetById_MissingEntity_Returns404ProblemDetails` (7-route theory) + `SupabaseDependentControllerNotFoundTests` (Team/Venue GET) | COMPLIANT |
| Not-Found Status | PUT/DELETE/nested action on nonexistent parent -> 404 | `UpdateDivisionById_MissingEntity_Returns404ProblemDetails` (PUT) + `RegisterTeam_MissingTournament_Returns404ProblemDetails` (nested) + `TeamController_DeleteTeamById_MissingEntity_Returns404ProblemDetails` (DELETE) | **COMPLIANT (was UNTESTED, now closed)** |
| ProblemDetails Body Shape | 404 body matches ProblemDetails | Same tests, all assert status/title/detail/traceId | COMPLIANT |
| ProducesResponseType Reflects 404 | OpenAPI metadata | Static evidence (compile-time attribute check) | COMPLIANT (static, attribute-only) |
| Create-Time FK Validation Stays 400 | POST invalid FK stays 400 | `CreatePlayer_MissingTeamId_StaysBadRequest` | COMPLIANT |

#### frontend-http-error-pipeline (3 requirements, 4 scenarios) — unchanged, all COMPLIANT (not touched by corrective pass)

**Compliance summary**: 9/9 scenarios compliant, 0 UNTESTED.

### Issues Found

**CRITICAL**: 0 — the sole prior CRITICAL is closed.

**WARNING**: 2, both re-confirmed unchanged (not escalated):
1. `.gitignore` still shows only the pre-existing unrelated modification (`*.pdf`/`.atl/` ignore entries), re-diffed via `git diff -- .gitignore`, confirmed untouched by this change's scope.
2. Team/Venue GetById (and now DeleteTeamById) covered by controller-unit tests instead of full HTTP integration, due to the pre-existing Supabase eager-websocket testability gap (unrelated to this change) — non-vacuous but a real degradation from the design's stated integration-test preference, consistently applied.

**SUGGESTION**: 1, re-confirmed unchanged: `.codegraph/` still appears as untracked (`??`) in `git status`, tooling artifact, should likely be gitignored — not this change's responsibility.

### Verdict

**PASS WITH WARNINGS** — 0 CRITICAL, 2 WARNING (both pre-existing/unrelated to scope, re-confirmed not escalated), 1 SUGGESTION. All 9/9 spec scenarios now COMPLIANT with runtime-verified coverage. Build and full test suites (BE 21/21, FE 42/42) independently re-run and green. No production code regression in the corrective pass. Ready for archive.
