# Proposal: Fix HTTP error-contract bugs (400→404 not-found + sendGet 401 pipeline)

Touches: **backend AND frontend** (coordinated, one change).

## Intent

Two confirmed behavior bugs in the BE↔FE error-handling contract, re-verified against current code:

- **Bug 1 (backend):** ~24 "not found" cases across 8 controllers return `BadRequest` (400) instead of `NotFound` (404) — wrong HTTP semantics. They also return **bare strings**, diverging from the `ProblemDetails` shape `GlobalExceptionHandler` emits (the audit's "two disconnected error shapes" finding). FE `isBadRequestResponse` never matches bare strings today.
- **Bug 2 (frontend):** `sendGet` has its own catch that never calls `throwError`, so `triggerStatusCodeHandlers`/`handleUnauthorizedToken` (401→`/token-invalido` redirect) never fire on GET — the majority of read traffic loses session-expiry handling.

This is the first behavior slice justifying the test-scaffolding investment.

## Scope

### In Scope
- Change ~24 not-found `BadRequest`→`NotFound` in `{BlogPost,Division,Match,Player,Team,Tournament,Venue,PlayerStatistic}Controller.cs`, **plus 3 in `PlayerSanctionController.cs`** (same bug, missed by audit).
- Return a `ProblemDetails`-consistent body (title/detail/status/traceId) matching `GlobalExceptionHandler`; update each `[ProducesResponseType]` to `Status404NotFound`.
- Fix `sendGet` to route through shared `sendRequest`/`throwError` pipeline.
- Backend integration tests: each fixed endpoint returns 404 for a missing entity.
- Frontend tests: `sendGet` fires 401 redirect; not-found surfaces as 404.

### Out of Scope
- Genuine 400s kept: image/logo validation (BlogPost, Team), business rules (Match already-started, PlayerSanction appeal states).
- Other audit findings (param naming, dead code, magic colors, i18n, TeamsPage decomposition, `Logout` Infrastructure leak).
- FE `statusCode`→`status` field-name mismatch in `error.context.tsx` (note only; may fold in if trivial).

## Capabilities

### New Capabilities
- `api-not-found-semantics`: controllers return 404 with ProblemDetails-consistent shape for missing entities.
- `frontend-http-error-pipeline`: all HTTP verbs (incl. GET) route through the shared error/401-redirect pipeline.

### Modified Capabilities
- None (no existing spec covers these behaviors).

## Approach

Introduce a small controller helper (or `Problem(statusCode:404,...)`) so 404 bodies match `GlobalExceptionHandler`. Rewrite `sendGet` to delegate to `sendRequest<T>('GET', ...)`, deleting its bespoke catch. Cover both with the now-available xUnit/Vitest scaffolding.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `API/Controllers/*.cs` (9 files) | Modified | 400→404 + ProblemDetails body + ProducesResponseType |
| `Club12-WebClient/src/modules/core/utils/axiosUtils.ts` | Modified | `sendGet` routes through pipeline |
| backend integration tests | New | 404-on-missing per endpoint |
| frontend axiosUtils tests | New | 401 redirect + 404 handling |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| 400→404 breaks FE branching | **Low** | Verified: no FE code branches on 400 for not-found; `error.context` is status-agnostic |
| Player POST "no Team with id" (create-time FK) miscoded | Med | Decision needed — keep 400/422 (invalid input) vs 404; default: keep as 400 |
| ~27 endpoints + tests exceed 800-line budget | Med | Parameterize tests; else split BE-fix / FE-fix PRs (no ordering constraint) |

## Rollback Plan

Revert the change branch. BE and FE commits are independent (no atomic dependency), so either side can be reverted alone without breaking the other.

## Dependencies

- Test scaffolding (xUnit backend, Vitest frontend) from prior slices — landed/landing on `develop`.

## Success Criteria

- [ ] All not-found paths in the 9 controllers return 404 with ProblemDetails-consistent body.
- [ ] Genuine 400 validation/business-rule paths unchanged.
- [ ] `sendGet` triggers 401→`/token-invalido` redirect (test-proven).
- [ ] Backend + frontend tests green; both builds pass.

## Proposal question round

Assumptions needing user review (executor cannot ask interactively):
1. Include `PlayerSanctionController`'s 3 not-found cases (same bug, outside audit list)? **Assumed yes.**
2. `Player` POST referencing a missing `TeamId` — keep 400/422 (invalid input) or make 404? **Assumed keep 400.**
3. Deliver single-PR (preferred, if ≤800 lines) or split BE/FE? Since there's **no atomic breaking dependency**, split is safe if budget is exceeded. **Assumed single-PR, fallback split.**
