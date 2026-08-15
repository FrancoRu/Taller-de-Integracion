# Design: High Test Coverage — Batch B (Sanction Appeal + Auth JWT)

## Technical Approach

Two independent test-only work units, zero production changes. **b1** characterizes the sanction
appeal state machine (which lives in `PlayerSanctionController.AppealPlayerSanction` /
`ResolvePlayerSanctionAppeal` — an accepted arch smell) through the real HTTP pipeline using the
existing `CustomWebApplicationFactory`, matching the proven pattern in `NotFoundContractTests` and
`AutomatedMatchGenerationTests`. **b2** characterizes `AuthService.GenerateJwtTokenAsync` as a pure
unit test: the class depends only on `IConfiguration`, so it is instantiated directly against an
in-memory config — no host, no DB.

## Architecture Decisions

| Decision | Choice | Rejected alternative | Rationale |
|----------|--------|----------------------|-----------|
| b1 test layer | HTTP integration via `CustomWebApplicationFactory` + `HttpClient` | Direct-controller unit with fake `IPlayerSanctionService` | State-machine logic is only reachable through the pipeline; full dependency graph boots anonymously (routes are un-`[Authorize]`d / `[AllowAnonymous]`); HTTP proves real serialization + persistence. Direct-controller (the `SupabaseDependentControllerNotFoundTests` fallback) is only for controllers that cannot boot — not the case here. |
| b1 assertion depth | Assert HTTP status **and** re-read persisted `AppealStatus` from a fresh DI scope | Trust response body only | The transition is a persistence side effect; reading it back in a second scope proves `UpdatePlayerSanctionAsync` actually committed the new status and cleared/derived fields. |
| b1 seeding | Seed the full required object graph via `ApplicationDBContext` in a scope | Set only FK Guids | `PlayerSanction` has `required Player`/`required Match`; EF-Core SQLite enforces FKs, so Team→Player and Tournament→Division→Stage→Match rows must exist. Mirror `SeedStageAsync`. |
| b2 test layer | Pure unit: `new AuthService(inMemoryConfig)` | `WebApplicationFactory` harness | AuthService needs only `JWT:Key/Issuer/Audience`; a lighter, faster test with no host/DB. |
| b2 JWT verification | `JwtSecurityTokenHandler.ValidateToken` with `TokenValidationParameters` bound to the same key/issuer/audience | Manual base64 decode of segments | Round-trips through the real validation path — proves signature, issuer, audience, and claims in one call. |
| b2 refresh-token check | Assert two calls yield different `RefreshToken` values | Assert entropy/length only | `GenerateRefreshToken` is `private static`, observable solely via `TokenResponse.RefreshToken`; inequality is the only provable property (documented limitation). |

## Data Flow

**b1 (per case):**

    Seed graph (Team→Player, Tournament→Division→Stage→Match, PlayerSanction[status]) via DbContext
         │
    HttpClient PUT /api/player-sanctions/{id}/appeal  (or /appeal/resolve)
         │
    Assert HTTP status (200 / 400 / 404)
         │
    Fresh DI scope → re-query PlayerSanction → assert persisted AppealStatus

**b2:**

    ConfigurationBuilder().AddInMemoryCollection(JWT:Key/Issuer/Audience)
         │
    new AuthService(config).GenerateJwtTokenAsync(claims)
         │
    JwtSecurityTokenHandler.ValidateToken(accessToken, params) → assert claims + expiry (~24h)
    + second call → assert RefreshToken differs + TokenResponse.ExpiresIn == FromHours(24)

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Club12-Backend/API.Tests/PlayerSanctionAppealTests.cs` | Create | b1 integration tests + private `SeedSanctionAsync(db, status)` helper |
| `Club12-Backend/API.Tests/AuthServiceJwtTests.cs` | Create | b2 unit tests + in-memory `IConfiguration` builder helper |

No production files touched. `CustomWebApplicationFactory` already sets `JWT__*` env vars, so b1 needs no config changes.

## Test Coverage (behaviors)

- **Appeal**: 400 when `AppealStatus==Pending`; 200 + persisted `Pending` (with prior resolution cleared) from `None`/`Rejected`/`Accepted`; 404 when missing.
- **Resolve**: 400 unless `Pending`; 200 + persisted `Accepted` (`resolveRequest.Accepted==true`) / `Rejected` (false); 404 when missing.
- **JWT**: expected claims present (NameIdentifier, roles); `Expires ≈ UtcNow.AddHours(24)` and `TokenResponse.ExpiresIn == TimeSpan.FromHours(24)`; two calls yield distinct refresh tokens; access token validates against configured key/issuer/audience.

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary. Pure test additions.

## Delivery Forecast

Estimated ~300–430 authored test lines. b1 and b2 are independently buildable/shippable. Combined they may exceed the 400-line review budget; if so, split into stacked PRs (b1 first, b2 second) — each has autonomous scope, its own verification, and trivial rollback (delete the file). Recommend single PR only if authored total lands ≤ 400. Zero production code changes confirmed.

## Migration / Rollout

No migration required. Rollback = delete the two new test files.

## Open Questions

- [ ] None blocking. `SeedSanctionAsync` object-graph depth (Team + Match chain) is the main effort in b1; resolved by mirroring `SeedStageAsync`.
