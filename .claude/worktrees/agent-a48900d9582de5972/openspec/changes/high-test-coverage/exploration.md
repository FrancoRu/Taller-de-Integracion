# Exploration: Business-logic test coverage gap (high-test-coverage change)

## Current State

Backend (`Club12-Backend/API.Tests`, ~64 tests) covers: SmokeTests, NotFoundContractTests, AuthControllerLogoutTests, Backup/* suites, and one characterization test `AutomatedMatchGenerationTests.cs` covering ONLY `MatchService.CreateAutomatedMatchesAsync` for knockout/final stages. No unit tests exist for any other Application service.

Frontend (~59+ tests) covers query-key factories, color tokens, routes, axios pipeline, and one full flow (`TeamsPage`/`TeamsTable`/`TeamFormDialog`/`TeamsFilterBar`). No other module/view/context has tests.

## Affected Areas (highest value first)

- `Club12-Backend/Application/Services/MatchService.cs` — `GenerateFixtureAsync` (round-robin double-fixture rotation), `CreateGroupStageMatchesAsync`, `ResolveGroupTeamCountAsync`, `DistributeMatchDates` (0-match/end<start/single-match-midpoint edge cases) are all untested; only knockout/final match counts are characterized today.
- `Club12-Backend/Application/Services/StageService.cs` — `CreateAutomatedStagesAsync` (8/16/32/64 team validation, date chaining, group naming) and `AssignTeamsToStageAsync` (slot-capacity math) — zero coverage.
- `Club12-Backend/API/Controllers/PlayerSanctionController.cs` (lines 121-183) — the sanction **appeal state machine** actually lives here, not in `PlayerSanctionService.cs`: `AppealPlayerSanction` blocks re-appeal while `Pending`; `ResolvePlayerSanctionAppeal` blocks resolving unless `Pending`. Zero tests. Architecture smell worth flagging.
- `Club12-Backend/Application/Services/AuthService.cs` — `GenerateJwtTokenAsync`/`GenerateRefreshToken` (24h expiry, claims, HMAC signing, crypto RNG) — security-sensitive, untested beyond the logout side effect.
- `Club12-Backend/Application/Services/PlayerSanctionService.cs` — `GetExpiredSanctionsAsync` date-math predicate — untested.
- `Club12-Backend/Application/Services/TeamService.cs` — `RegisterTeamsToTournamentAsync` diff/assign/unassign logic — untested.
- `Club12-Backend/Infrastructure/Repositories/ScorerRepository.cs` — `GetPlayerScoresAsync` top-scorer aggregation (EF LINQ query, needs `CustomWebApplicationFactory`-style integration test) — untested.
- `Club12-WebClient/src/views/division/divisionStandings.tsx` — `sortPositions()` pure tie-break function — untested; note that no backend controller/service/repository was found that populates `positions`/`Position`/`PositionsResponse` (`Club12-Backend/Infrastructure/Persistance/Configurations/PositionEntityConfiguration.cs` is an empty stub class) — this looks like an incomplete/dead feature, flagged as a risk not just a gap.
- Pure CRUD services with **no** real logic (low priority, do not lead with these): `DivisionService`, `TournamentService`, `VenueService`, `PlayerService`, `PlayerStatisticService`, `BlogPostService`, `ScorerService` (thin wrapper).
- `IdentityUserManagementService` / `IdentityAuthenticationService` (`Club12-Backend/Infrastructure/Identity/`) — not read in depth this pass; needs a follow-up look before finalizing scope.

## Approaches

1. **Single mega test-coverage PR** — write tests for everything in one change.
   - Pros: one pass, no coordination overhead.
   - Cons: guaranteed to blow the review budget (session precedent already splits everything into batches); mixes low-value CRUD tests with high-value logic tests, diluting reviewer focus.
   - Effort: High.

2. **Three prioritized batches (recommended)** — Batch A: MatchService fixture/group-generation + StageService automated-stage/assign-team tests. Batch B: sanction appeal state machine + AuthService JWT generation. Batch C: PlayerSanctionService date-math, TeamService register-to-tournament, ScorerRepository integration test, DivisionStandings pure-function test.
   - Pros: matches the session's established chained-PR pattern; front-loads highest business-risk logic; keeps each PR reviewable.
   - Cons: requires 3 separate sdd-propose/apply cycles.
   - Effort: Medium per batch.

## Recommendation

Go with the 3-batch split, starting with Batch A (MatchService + StageService automated generation) since it has the highest business risk (fixture/knockout generation directly drives tournament outcomes) and is a natural extension of the existing `AutomatedMatchGenerationTests.cs` characterization pattern. Do not spend budget writing tests for pure CRUD pass-through services (DivisionService, TournamentService, VenueService, PlayerService, PlayerStatisticService, BlogPostService) — they have no conditional logic to break.

One open design question for sdd-propose: should the sanction appeal state machine be tested in place inside `PlayerSanctionController`, or extracted into `PlayerSanctionService` first (cleaner architecture, easier unit testing, but adds refactor risk)? Default: test in place for Batch B, defer extraction as a separate future structural change.

## Risks

- Appeal state machine lives in the controller, not a service — testing in place is faster but may need rewriting if a later architecture cleanup extracts it.
- `positions`/`Position` standings feature appears backend-incomplete — testing `sortPositions` alone is fine (cheap, pure function), but don't scope broader "standings" test work around a feature that may not work end-to-end without a product decision first.
- `IdentityUserManagementService`/`IdentityAuthenticationService` not yet explored in depth — could add a 4th batch.

## Ready for Proposal

Yes — for Batch A. Recommend running `sdd-propose` scoped to MatchService/StageService automated-generation edge cases first, with Batches B and C queued as separate follow-up proposals.
