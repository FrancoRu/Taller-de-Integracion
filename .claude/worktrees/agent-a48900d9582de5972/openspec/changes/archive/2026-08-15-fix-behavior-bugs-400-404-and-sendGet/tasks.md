# Tasks: Fix HTTP error-contract bugs (400→404 not-found + sendGet 401 pipeline)

## Review Workload Forecast

Grep-confirmed: 28 genuine not-found sites, 9 controllers (PlayerSanction 4, Player 3,
Division 2, PlayerStatistic 2, Match 4, Team 4, BlogPost 3, Tournament 3, Venue 3).
`PlayerController` L50 (create-time TeamId FK) stays 400, untouched.

| Field | Value |
|-------|-------|
| Estimated changed lines | ~280–320 (BE ~235, FE ~45) |
| 800-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

Design flagged risk vs a 400-line budget; real budget is 800, so ~300 lines fits and
the BE/FE split fallback is not needed. `single-pr` still needs `size:exception` sign-off.

### Suggested Work Units

| Unit | Goal | PR | Focused test | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | BE: extension + 28-site conversion + contract test | PR 1 | `dotnet test API.Tests --filter FullyQualifiedName~NotFoundContractTests` | `dotnet test API.Tests` | Revert extension + 9 controllers |
| 2 | FE: `sendGet` delegation + tests | PR 1 | `npm run test -- axiosUtils.test.ts` | Same (mocked axios) | Revert `axiosUtils.ts`/tests; independent of BE |

## Phase 1: Backend RED

- [x] 1.1 `API.Tests/NotFoundContractTests.cs`: `[Theory]`/`[InlineData]` over `CustomWebApplicationFactory`, one GET-by-id per controller (matches/teams/blog-posts/divisions/players/player-sanctions/player-statistics/tournaments/venues) with random `Guid`; assert 404, `application/problem+json`, non-empty `title`/`detail`/`traceId`. Must fail now (400).
- [x] 1.2 Same file: regression case — POST Player with nonexistent `TeamId` still 400. Must already pass.

## Phase 2: Backend GREEN

Shared rule for 2.2–2.10: convert each not-found `BadRequest` to `this.NotFoundProblem(nameof(Entity), id)`.
Per action: REPLACE `[ProducesResponseType(400)]`→`404` if not-found is the sole error reason;
ADD `404` alongside existing `400` if the action also returns 400 for another reason (business-state,
validation). Leave out-of-scope 400s (image/logo validation, business-state, create-time FK) untouched.

- [x] 2.1 Create `API/Utils/ControllerBaseExtensions.cs`: `NotFoundProblem(this ControllerBase, string entity, object id)` → `controller.Problem(detail:, statusCode: 404, title:)` per design contract.
- [x] 2.2 `MatchController.cs`: 4 sites (L79,101,123,204); `UpdateMatchDate`/`UpdateMatchScore` are mixed 400+404 (ADD); others REPLACE.
- [x] 2.3 `TeamController.cs`: 4 sites (L78,107,135,161).
- [x] 2.4 `BlogPostController.cs`: 3 sites (L75,103,128).
- [x] 2.5 `DivisionController.cs`: 2 sites (L67,116).
- [x] 2.6 `PlayerController.cs`: 3 sites (L77,100,127); leave L50 FK 400.
- [x] 2.7 `PlayerSanctionController.cs`: 4 sites (L59,106,130,164). L130 (Appeal) and L164 (ResolveAppeal) are mixed 400+404 (ADD, business-state appeal checks remain 400); L59/L106 REPLACE.
- [x] 2.8 `PlayerStatisticController.cs`: 2 sites (L80,102).
- [x] 2.9 `TournamentController.cs`: 3 sites (L67,94,161).
- [x] 2.10 `VenueController.cs`: 3 sites (L67,94,123).
- [x] 2.11 Run `dotnet test API.Tests --filter FullyQualifiedName~NotFoundContractTests` — 1.1 and 1.2 pass. (Deviation: Team/Venue GetById sites degraded to direct-controller unit tests in `SupabaseDependentControllerNotFoundTests` — see Deviations note.)
- [x] 2.12 Run full `dotnet test API.Tests` — `SmokeTests`, `AutomatedMatchGenerationTests` stay green. 16/16 passing.

## Phase 3: Frontend RED

- [x] 3.1 `axiosUtils.test.ts`: import `sendGet`; add 401-redirect case mirroring `sendDelete`'s (mock `axios.request` rejecting `buildUnauthorizedError(true)`, call `sendGet('divisions/123')`, assert `assignSpy` called with `/token-invalido`). Must fail now. Confirmed RED.
- [x] 3.2 Add 404-reject case: mock `axios.request` rejecting a 404 AxiosError, call `sendGet`, assert same error shape as other verbs (no reshaping). (Already true pre-fix — `sendGet`'s bespoke catch also re-threw unchanged; this case guards against regressions once GREEN routes through `throwError`.)

## Phase 4: Frontend GREEN

- [x] 4.1 `axiosUtils.ts`: replace `sendGet` body (bespoke try/catch, L270-285) with `return await sendRequest<T>('GET', resource, {}, null, query);`; signature unchanged.
- [x] 4.2 Run `npm run test -- axiosUtils.test.ts` — all 4 cases pass. Confirmed: 4/4 passing.

## Phase 5: Cross-cutting

- [x] 5.1 Grep repo for `status === 400`/`statusCode === 400` near API error handling — confirm no frontend code branches on 400-for-not-found (non-goal boundary). Confirmed: only 2 hits, both in `error.context.tsx` (generic `?? 400` fallback default and a `status < 400` success/error threshold) — neither is 400-for-not-found-specific branching. `isBadRequestResponse` matches ProblemDetails shape generically (detail/title/status), which now also matches the new 404 bodies — a positive side effect, not a regression.
- [x] 5.2 Run BE+FE suites together; check off tasks before requesting review. BE: 16/16 pass. FE: 42/42 pass (16 files, see Work Unit Evidence). `tsc --noEmit` clean.

## Phase 6: Corrective — PUT/DELETE/Nested-Action Runtime Coverage

Closes the CRITICAL gap from `sdd-verify` (Engram `sdd/fix-behavior-bugs-400-404-and-sendGet/verify-report`,
id 585): spec `api-not-found-semantics` Requirement 1's second scenario ("PUT/DELETE or nested action
against nonexistent parent returns 404") had zero runtime-covering tests — all prior BE not-found tests
were GET-shaped only. The 28 source sites were already correctly fixed in the prior apply pass (all call
the shared `NotFoundProblem` extension); this phase adds the missing runtime proof.

- [x] 6.1 `API.Tests/NotFoundContractTests.cs`: add `UpdateDivisionById_MissingEntity_Returns404ProblemDetails` — full HTTP round-trip PUT `api/divisions/{id}` against a nonexistent id via `CustomWebApplicationFactory`, asserting 404 + ProblemDetails shape. RED-confirmed (temporarily reverted `DivisionController.UpdateDivisionById`'s not-found branch to `BadRequest`, test failed with `status=BadRequest`; reverted, test passed) before being accepted as GREEN.
- [x] 6.2 Same file: add `RegisterTeam_MissingTournament_Returns404ProblemDetails` — full HTTP round-trip POST `api/tournaments/register-teams/{id}` (nested action referencing a nonexistent parent tournament id, the spec's own example category) against a nonexistent id, asserting 404 + ProblemDetails shape.
- [x] 6.3 `SupabaseDependentControllerNotFoundTests`: add `TeamController_DeleteTeamById_MissingEntity_Returns404ProblemDetails` — direct-controller unit test (same `null!`-SupabaseHelper pattern as the existing Team/Venue GET cases, since `DeleteTeamById` also constructor-injects the eager-websocket `SupabaseHelper`) exercising a real DELETE not-found path.
- [x] 6.4 Run `dotnet test Club12-Backend/Solution/Club12.sln` — full suite green, 21/21 (16 prior + 5: 2 full-host PUT/nested-action + 1 Supabase-unit DELETE + net new count includes pre-existing unrelated `AuthControllerLogoutTests` (2), untracked/out-of-scope for this change — see Risks).
- [x] 6.5 Confirm `.gitignore`'s pre-existing unrelated modification (`*.pdf`/`.atl/` ignores) is still untouched by this change — re-verified via `git diff -- .gitignore`, confirmed unrelated and not part of this change's diff.
