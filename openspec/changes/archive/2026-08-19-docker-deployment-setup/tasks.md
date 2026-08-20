# Tasks: Docker deployment setup (backend + frontend)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~250-280 (additions, mostly new files) |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Full docker-deployment-setup change (health endpoints + both Dockerfiles + compose + relative API URL) | PR 1 (single) | `dotnet test Club12-Backend/Solution/Club12.sln --filter FullyQualifiedName~HealthEndpointsTests` and `pnpm --dir Club12-WebClient test -- routes.test.ts` | `docker compose up -d` at repo root, then `curl`/browser against `http://localhost:${FRONTEND_PORT:-5001}` | All new files (Dockerfiles, nginx.conf, compose, .env.example) are additive-only; `Program.cs`/`StartupExtensions.cs`/`routes.ts`/`vite.config.ts` diffs are backward-compatible — `git revert` restores pre-change behavior |

## Phase 1: Backend Health Endpoints (test-first)

- [x] 1.1 RED: `Club12-Backend/API.Tests/HealthEndpointsTests.cs` — `GET /health` returns 200 (uses `CustomWebApplicationFactory`)
- [x] 1.2 RED: same file — `/health` request completes without any DB call even with the factory's SQLite connection closed/unreachable
- [x] 1.3 RED: same file — `GET /health/ready` returns 200 when the DB check succeeds (default factory)
- [x] 1.4 RED: same file — `GET /health/ready` returns 503 when the DB check fails (dispose/break the SQLite connection before the call)
- [x] 1.5 RED: same file — both endpoints return non-401/403 with no `Authorization` header
- [x] 1.6 GREEN: `Club12-Backend/API/API.csproj` — add `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` `8.0.30` (fallback to highest published `8.0.*` if unavailable)
- [x] 1.7 GREEN: `Club12-Backend/API/Utils/StartupExtensions.cs` — add `AddHealthChecksConfig()` and `MapHealthCheckEndpoints()` per design.md contract
- [x] 1.8 GREEN: `Club12-Backend/API/Program.cs` — call `.AddHealthChecksConfig()` in the service chain and `app.MapHealthCheckEndpoints();` next to `MapControllers()`
- [x] 1.9 Run `dotnet test` for `API.Tests` — confirm 1.1-1.5 pass, no regressions

## Phase 2: Backend Dockerfile

- [x] 2.1 Create `Club12-Backend/Dockerfile` per design.md's exact multi-stage contract (sdk:8.0 → aspnet:8.0, curl install, non-root `$APP_UID`, `HEALTHCHECK` on `/health`)
- [x] 2.2 Create `Club12-Backend/.dockerignore` per design.md contract (`bin/`, `obj/`, `API.Tests/`, `Solution/`, `appsettings.*.json`) — deviation: added `!API/appsettings.Development.json` negation (see Deviations section)

## Phase 3: Frontend relative API URL + dev proxy

- [x] 3.1 RED: `Club12-WebClient/src/modules/core/constants/routes.ts` test — tighten `routes.test.ts` assertion from `toContain('/api')` to `toBe('/api')`
- [x] 3.2 GREEN: `Club12-WebClient/src/modules/core/constants/routes.ts` — change `apiUrl` to relative `'/api'`
- [x] 3.3 Static (no existing test for `vite.config.ts`): add `server.proxy['/api']` targeting `https://localhost:${env.VITE_BACKEND_PORT}` with `changeOrigin: true, secure: false` per design.md
- [x] 3.4 Run `pnpm test -- routes.test.ts` and full frontend unit suite — confirm pass (161/161); `vite.config.ts` type-correctness confirmed via `pnpm build`'s `tsc` step in Phase 6 — live `pnpm dev` reachability not exercised standalone (would require a running local backend + trusted dev cert outside this batch's scope)

## Phase 4: Frontend Dockerfile + Nginx

- [x] 4.1 Create `Club12-WebClient/Dockerfile` per design.md (node:24-bookworm-slim + pnpm 11.22.0 → nginx:alpine, serves `build/`)
- [x] 4.2 Create `Club12-WebClient/nginx.conf` per design.md (SPA `try_files` fallback, `/api/` `proxy_pass http://backend:8080`)
- [x] 4.3 Create `Club12-WebClient/.dockerignore` per design.md (`node_modules`, `build`, `e2e`, `.env*`, test artifacts)

## Phase 5: Compose + env + gitignore

- [x] 5.1 Create repo-root `docker-compose.yml` per design.md (both services, shared `club12` network, backend `expose`-only, `/health/ready` compose healthcheck, `depends_on: condition: service_healthy`)
- [x] 5.2 Create repo-root `.env.example` per design.md (every key, placeholders only, inline comments)
- [x] 5.3 Verified repo-root `.gitignore` excludes `.env` (`git check-ignore -v .env` → matched at line 456) and does NOT exclude `.env.example` (`git check-ignore -v .env.example` → exit 1, not ignored); added a clarifying comment above the block

## Remediation (post-verify)

sdd-verify flagged 2 CRITICAL findings (2 of 20 spec scenarios had no covering test/live verification). This section closes both, surgically, without touching any other already-verified work.

- [x] R.1 Add `HealthReady_RepeatedFailures_KeepReturning503_WithoutAffectingLiveness` to `Club12-Backend/API.Tests/HealthEndpointsTests.cs` — covers service-health-endpoint scenario "Repeated readiness failures do not affect the process": calls `/health/ready` 3x against a broken DB (reusing `BreakDatabaseConnection()`), asserting 503 every time, interleaved with `/health` calls on the same host instance asserting 200 — proving the process stays alive and responsive throughout. Test passed immediately (no production code change needed; ASP.NET Core's HealthCheckMiddleware already catches exceptions from check delegates by framework design) — 224/224 backend tests passing.
- [x] R.2 Live-verify container-deployment scenario "Local dev server still reaches the backend": started the real backend (`dotnet run --launch-profile Franco`, `https://localhost:5001`) and the real Vite dev server (`pnpm dev --port 3001`, reading `VITE_BACKEND_PORT=5001` from `.env`), then issued a real `curl http://localhost:3001/api/divisions/` — got `HTTP 200` with the exact same JSON body as a direct call to the backend (real Supabase-backed data), and a `HEAD` request showed `server: Kestrel`, confirming the request actually reached the .NET backend through Vite's dev proxy. Both dev servers killed and ports confirmed free afterward; no stray files left in the repo.

## Phase 6: Integration / E2E Verification

- [x] 6.1 Integration: `docker build` `Club12-Backend/` — succeeds; final stage is `aspnet:8.0` (verified: no SDK dir, `Config.User=1654`); no `API.Tests.dll` (verified absent); `appsettings.json` + `appsettings.Development.json` present, no `appsettings.Franco.json`; `id -u` = 1654 ≠ 0; no `pg_dump`; `curl` present; `HEALTHCHECK` targets `/health`; port 8080 exposed
- [x] 6.2 Integration: `docker build` `Club12-WebClient/` — succeeds (after fixing a Dockerfile bug — see Deviations); no `node_modules` in final image; served from `nginx:alpine` (Alpine 3.24.1, `/usr/sbin/nginx`), `build/` copied to `/usr/share/nginx/html`
- [x] 6.3 E2E: `docker compose up -d` at repo root (against a populated `.env`) — backend container reports `healthy` (verified: `club12-backend-1 ... Up 20 seconds (healthy)`; real EF Core migrations ran against a temp local Postgres substituted for Supabase — see Deviations)
- [x] 6.4 E2E: request an unknown SPA path against the frontend port — returns 200 `index.html` (SPA fallback) — verified: `curl http://localhost:5001/some/deep/client-route` → `HTTP 200` with `<!doctype html>...<title>Club 12</title>` body
- [x] 6.5 E2E: request `/api/...` against the frontend port — reaches the backend (Kestrel) through the Nginx proxy — verified: `curl http://localhost:5001/api/divisions/` → `HTTP 200` `{"items":[],"page":1,"pageSize":100,"totalCount":0}` (real backend response, not an Nginx 404)
- [x] 6.6 Static: grep the built `Club12-WebClient/build/` JS output for `localhost` — must not appear as part of an API base URL — verified against the actual shipped image (`docker cp` extraction): only 2 unrelated `http://localhost` string literals from third-party libs remain; the old `https://localhost:5001/api` string is gone. Note: the pre-existing host-side `Club12-WebClient/build/` directory is stale/git-ignored and was NOT used for this check (Docker context excludes it via `.dockerignore`)
- [x] 6.7 `docker compose down` — cleanup
