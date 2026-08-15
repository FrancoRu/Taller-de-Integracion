# Proposal: High Test Coverage — Batch B (Sanction Appeal + Auth JWT)

## Intent

Batch B of the `high-test-coverage` effort characterizes two untested,
high-value backend behaviors: the **sanction appeal state machine** (business
logic living in `PlayerSanctionController`, an architecture smell) and
**`AuthService` JWT/refresh-token generation** (security-sensitive, untested
beyond the logout side effect). Pure test addition — lock in current behavior
before any future refactor. No production code changes.

## Scope

### In Scope
- **b1 — Sanction appeal (integration, via `CustomWebApplicationFactory`)**:
  `AppealPlayerSanction` blocked (400) when `AppealStatus == Pending`; allowed
  from `None`/`Rejected`/`Accepted`; 404 when sanction missing; sets Pending +
  clears prior resolution fields. `ResolvePlayerSanctionAppeal` blocked (400)
  unless `Pending`; transitions to `Accepted`/`Rejected` per `resolveRequest.Accepted`;
  404 when missing.
- **b2 — Auth JWT (pure unit test on `AuthService` + in-memory `IConfiguration`)**:
  token carries expected claims (user id, roles); expiry is `UtcNow.AddHours(24)`
  and `TokenResponse.ExpiresIn == TimeSpan.FromHours(24)` (values read from source,
  not assumed); two calls yield different refresh tokens; access token is
  parseable/validatable against the configured signing key, issuer, and audience.

### Out of Scope (Non-Goals)
- **No extraction** of the appeal state machine into `PlayerSanctionService`
  (separate future structural change if the team wants it).
- **No changes** to JWT configuration, expiry, or security posture.
- **No bug fixes** even if found — log as follow-ups only.
- No `InternalsVisibleTo`, no production refactor (Batch A precedent).

## Capabilities

### New Capabilities
- None (test-only change; no spec-level behavior introduced).

### Modified Capabilities
- None (characterizes existing behavior; no requirements change).

## Approach

- **b1**: controller logic is only reachable through HTTP; test through the real
  pipeline with `CustomWebApplicationFactory`, seeding `PlayerSanction` rows per
  scenario and asserting status codes + persisted `AppealStatus`.
- **b2**: `AuthService` depends only on `IConfiguration`; instantiate directly
  with an in-memory config providing `JWT:Key`/`Issuer`/`Audience`. Validate the
  token with `JwtSecurityTokenHandler` + `TokenValidationParameters`.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Club12-Backend/API.Tests/` | New | 2 test files (sanction appeal integration; auth JWT unit) |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Tests couple to controller shape; break if state machine is later extracted | Med | Accepted per non-goals; assert behavior/status, not internals |
| Refresh-token randomness only provable by inequality, not "true" randomness | Low | Assert two calls differ; document limitation |

## Rollback Plan

Delete the two new test files; no production code touched.

## Dependencies

- Existing `CustomWebApplicationFactory` test harness (`API.Tests`).

## Success Criteria

- [ ] Appeal + resolve state transitions and guard clauses covered (allow/block/404).
- [ ] JWT claims, 24h expiry, refresh-token uniqueness, and signature validity covered.
- [ ] No production code changed; all tests green.
- [ ] Any latent bugs discovered are logged as follow-ups, not fixed here.

## Delivery Note

Estimated ~300–430 authored test lines — fits single-PR under the 800-line
budget. Structured as two independent work units (b1, b2); split into stacked
PRs only if the estimate is exceeded during apply.
