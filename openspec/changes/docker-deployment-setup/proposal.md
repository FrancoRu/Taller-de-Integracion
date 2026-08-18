# Proposal: Docker deployment setup (backend + frontend)

**Touches: both** (backend `Club12-Backend`, frontend `Club12-WebClient`, plus repo-root orchestration).

## Intent

The repo has no container packaging at all — no Dockerfile, no `.dockerignore`, no compose file, no CI.
Deploying today means manual `dotnet publish` + `pnpm build` + hand-configured hosts, which the team
cannot reproduce reliably. This change delivers a reproducible, portable image per service so the app
can be deployed as containers.

## Scope

### In Scope
- `Club12-Backend/Dockerfile` — multi-stage (`sdk:8.0` build → `aspnet:8.0` runtime), restore-layer
  caching via `.csproj` copies, non-root final user, port 8080, `HEALTHCHECK` hitting `/health`.
- `Club12-Backend/API` — two health endpoints (`AddHealthChecks()` / `MapHealthChecks`), anonymous:
  - `/health` — liveness only, no dependency checks (process is up).
  - `/health/ready` — readiness, adds a DB connectivity check (EF Core/Npgsql) so misconfigured
    `ConnectionStrings__DbConnection` / Supabase-hosted Postgres surfaces as unready, not silently healthy.
- `Club12-WebClient/Dockerfile` — multi-stage (node 24 + pnpm 11 `--frozen-lockfile` + `pnpm build`
  → `nginx:alpine`), serving `build/` (not `dist/`).
- `Club12-WebClient/nginx.conf` — SPA fallback (`try_files $uri /index.html;`) + `/api` `proxy_pass`
  to the backend container by service name.
- `.dockerignore` for both projects (`bin/`, `obj/`, `node_modules/`, `.git/`, `appsettings.{Name}.json`).
- Repo-root `.gitignore` entry for `.env` (the file holding real production secrets never gets committed).
- `Club12-WebClient/src/modules/core/constants/routes.ts` — `apiUrl` changes from the hardcoded
  `https://localhost:${VITE_BACKEND_PORT}/api` to the relative `/api`, so the built bundle calls
  whatever origin served it (works through the Nginx proxy on any host/domain, not just `localhost`).
- `Club12-WebClient/vite.config.ts` — add a dev-server `server.proxy` for `/api` → the local backend
  (using the existing `VITE_BACKEND_PORT` env var), so `pnpm dev` keeps working without Docker.
- Repo-root `docker-compose.yml` — **this is the production deployment mechanism**, not a local-only
  reference. Both services on a shared network; all secrets/config (`ConnectionStrings__DbConnection`,
  `AllowedOrigins__N`, `JWT__*`, `Smtp__*`, `SupaBase__*`, `AdminUser__*`, `Frontend__*`) are sourced via
  `env_file` from a gitignored `.env` on the deploy host — never hardcoded or committed. A `.env.example`
  documenting every required key (no real values) ships in the repo.

### Out of Scope
- A full `VITE_API_BASE_URL` build-time variable / multi-target-host system (rejected in favor of the
  simpler relative-URL fix above, which already solves the same-origin case this deployment needs).
- `pg_dump` / `postgresql-client` in the image — `Backup:Enabled` stays `false`.
- CI/CD pipelines, registry publishing, K8s manifests, TLS termination, a bundled Postgres container
  (DB stays Supabase-hosted), and fixing the stale stack description in `openspec/config.yaml`.

## Capabilities

### New Capabilities
- `service-health-endpoint`: backend exposes unauthenticated liveness (`/health`) and readiness
  (`/health/ready`, DB connectivity check) endpoints for container probes.
- `container-deployment`: build + runtime contract for both service images and their compose wiring.

### Modified Capabilities
- None.

## Approach

Exploration Approach 1 (two independent Dockerfiles + frontend Nginx reverse proxy), with a minimal
correction: the frontend's hardcoded `https://localhost:<port>/api` is replaced by a relative `/api`
so requests always resolve against whatever origin actually served the page. Nginx serves the SPA and
proxies `/api` to the backend on the shared compose network — same-origin, no CORS, and no coupling to
the literal hostname `localhost` or to any build-time port. Backend images stay config-free — every
secret arrives as an env var at runtime.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Club12-Backend/Dockerfile`, `.dockerignore` | New | Multi-stage build, non-root, HEALTHCHECK |
| `Club12-Backend/API/Program.cs` | Modified | Register + map `/health` (liveness) and `/health/ready` (DB check) |
| `Club12-WebClient/src/modules/core/constants/routes.ts` | Modified | `apiUrl` → relative `/api` |
| `Club12-WebClient/vite.config.ts` | Modified | Add dev-server proxy for `/api` so `pnpm dev` keeps working |
| `Club12-WebClient/Dockerfile`, `.dockerignore`, `nginx.conf` | New | Build + Nginx runtime, SPA fallback, `/api` proxy |
| `docker-compose.yml`, `.env.example` (repo root) | New | Production orchestration; secrets via `env_file` from a gitignored `.env` |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| `pnpm dev` (non-Docker local dev) breaks once `apiUrl` is relative | Med | Add a `server.proxy` for `/api` in `vite.config.ts` targeting the local backend via `VITE_BACKEND_PORT` |
| `SupabaseHelper` singleton blocks in its constructor — bad `SupaBase__*` crashes/hangs startup | Med | Compose documents all required vars; `/health/ready`'s DB check surfaces the unready state even though `/health` (liveness) still answers |
| Secrets (`appsettings.{Name}.json`) leak into an image layer | Low | Explicit `.dockerignore` entries |
| EF migrations run automatically at startup against shared Supabase DB | Med | Confirmed acceptable: backend runs as a single replica for now; multi-replica support (splitting migrations out of startup) is explicitly out of scope |

## Rollback Plan

Most artifacts are new files. Three existing files are modified: `Program.cs` (additive `/health` +
`/health/ready` registration), `routes.ts` (`apiUrl` → `/api`), and `vite.config.ts` (dev-server proxy
addition). Revert the PR: deleting the Dockerfiles/compose/nginx config restores the previous
manual-deploy workflow; reverting the three modified files restores the original (dev-only-correct)
behavior with zero other runtime impact.

## Dependencies

- Docker Engine + Compose v2 on the deploy host.
- Reachable Supabase Postgres and Supabase Storage credentials at runtime.

## Success Criteria

- [ ] `docker build` succeeds for both projects from a clean checkout.
- [ ] `docker compose up` starts both services; backend reports healthy via `/health` and ready via
      `/health/ready` (DB check).
- [ ] Frontend served at its port loads, deep links resolve (SPA fallback), `/api` calls reach the backend.
- [ ] No secret file or `bin/`/`obj/`/`node_modules/` present in either image.
- [ ] Backend image runs as a non-root user.
- [ ] `.env` is gitignored; `.env.example` documents every required key with no real values.
