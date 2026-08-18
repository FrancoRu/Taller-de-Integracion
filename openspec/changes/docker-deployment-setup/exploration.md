# Exploration: Docker deployment setup (backend + frontend Dockerfiles)

## Current State

### Backend — `Club12-Backend/` (.NET 8, Clean Architecture: API/Application/Domain/Infrastructure, solution at `Club12-Backend/Solution/Club12.sln`)

- Entry point `Club12-Backend/API/Program.cs` has **no explicit Kestrel port config** (no `UseUrls`/`ASPNETCORE_URLS`/Kestrel section). In the official `mcr.microsoft.com/dotnet/aspnet:8.0` image this defaults to port **8080** via the image's built-in `ASPNETCORE_HTTP_PORTS`. Dev-only `launchSettings.json` uses 5001/5194 (irrelevant to Docker).
- **No health check endpoint exists anywhere** (no `AddHealthChecks`, no `MapHealthChecks`, no `/health` route). A Dockerfile `HEALTHCHECK` needs either a new endpoint or to be deferred/flagged.
- Swagger UI only enabled when `!env.IsProduction()` — disabled by default in a `Production`-environment container.
- Runs EF Core migrations + identity/admin seeding automatically at startup (`ExecuteMigrationsAndSeedAsync`) — no separate migration job needed.
- **DB provider: PostgreSQL via Npgsql** (`Npgsql.EntityFrameworkCore.PostgreSQL` in `Infrastructure.csproj`), not SQL Server. Dev connection string points at an external **Supabase-hosted Postgres pooler** — the app does not need to bundle its own Postgres container.
- Required runtime config, all env-var-driven (confirmed exact `__`-nested names via `Club12-Backend/API.Tests/CustomWebApplicationFactory.cs`, which sets them before host boot):
  - `ConnectionStrings__DbConnection` (required, throws if missing)
  - `AllowedOrigins__0`, `AllowedOrigins__1`, ... (CORS default policy, `AllowAnyHeader().AllowAnyMethod()`)
  - `JWT__Key`, `JWT__Issuer`, `JWT__Audience` (required)
  - `Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password` (required), `Smtp__UseSsl` (optional, default true), `Smtp__FromEmail`/`Smtp__FromName` (optional)
  - `SupaBase__ProjectUrl`, `SupaBase__ServiceRole`, `SupaBase__BucketName` — used by `Infrastructure/Storage/SupabaseHelper.cs`, a DI **singleton** whose constructor does a blocking `_client.InitializeAsync().Wait()` — bad/missing values will likely fail at first resolution (effectively startup).
  - `AdminUser__Email`, `AdminUser__Password` (first-run admin seeding)
  - `Frontend__MagicLinkUrl`, `Frontend__PasswordResetUrl` (must point at the real deployed frontend URL)
  - `Backup:*` — defaults ship in `appsettings.json` (`Enabled: false`), optional.
- **Native OS dependency risk**: `Infrastructure/Backup/PgDumpBackupService.cs` shells out to the `pg_dump` binary (`Backup:PgDumpPath`, default bare `pg_dump` on PATH). Not required unless `Backup:Enabled=true` in prod — if so, image needs `postgresql-client` via apt. No other native/image libs found (no EPPlus/ImageSharp/SkiaSharp/System.Drawing/PdfSharp/wkhtmltopdf) — uploads go straight to Supabase Storage.
- No `.dockerignore` anywhere in the repo (verified). Per-developer `appsettings.{Name}.json` files (Franco/Facundo/Tomas) are gitignored and contain real secrets — must never be baked into an image; only `appsettings.json`/`appsettings.Development.json` (no secrets) belong in the image.
- No CI workflows, no docker-compose/K8s/Azure/AWS config anywhere in the repo — confirmed greenfield.

### Frontend — `Club12-WebClient/` (Vite 8.2.1, React 19.2.8, TS ~6.0.3, pnpm 11.22.0, node >=24 per `package.json`)

- Build script `"build": "tsc && vite build"`. Output dir is **`build/`** (not Vite's default `dist/`), set explicitly in `vite.config.ts`. No `base` override (defaults to `/`).
- **Confirmed SPA client-side routing**: `src/main.tsx` uses `BrowserRouter` — Nginx (or any static server) config MUST use SPA fallback (`try_files $uri /index.html;`).
- `.env` present with `VITE_PORT=3001`, `VITE_BACKEND_PORT=5001`, `VITE_SUPABASE=<anon key>`. No `.env.example`/`.env.production` exists.
- **Blocking finding**: `src/modules/core/constants/routes.ts` hardcodes `` apiUrl: `https://localhost:${import.meta.env.VITE_BACKEND_PORT}/api` `` — only the **port** is env-driven; the **hostname is hardcoded to `localhost`**. As-is, a production build cannot point at a real backend host. This needs either a code change (a full `VITE_API_BASE_URL`) or a reverse-proxy design (frontend Nginx proxies `/api` to the backend container so `localhost` still resolves from the browser). This is a prerequisite, not a pure-Dockerfile concern — flagged explicitly to the user before design.
- Vite `VITE_*` vars are baked at build time (`import.meta.env`), not read at container runtime — any per-environment value needs either a Docker build-arg per image, or a runtime env-injection shim (e.g., `window.__ENV__`).

### General

- CORS `AllowedOrigins` must include the frontend's deployed container/domain via `AllowedOrigins__N` env vars per environment.
- `openspec/config.yaml` stack description is confirmed **stale**: says "React 18 + Vite 7 + React Router 6" but actual is React 19.2.8 / Vite 8.2.1 / react-router-dom 7.18.2 / TypeScript ~6.0.3; also references `npm run test/build` though the project uses pnpm. Not this change's job to fix, but Docker build commands should use `pnpm install --frozen-lockfile` + `pnpm build`.

## Affected Areas

- `Club12-Backend/API/API.csproj`, `Club12-Backend/Solution/Club12.sln` — multi-stage `dotnet publish` references
- `Club12-Backend/API/appsettings.json`, `appsettings.Development.json` — safe to bake in (no secrets)
- `Club12-Backend/Infrastructure/Backup/PgDumpBackupService.cs` — determines whether `postgresql-client` apt package is needed
- `Club12-WebClient/vite.config.ts` — build outDir `build/`
- `Club12-WebClient/src/modules/core/constants/routes.ts` — hardcoded `localhost` API host (blocking issue)
- `Club12-WebClient/src/main.tsx` — confirms SPA fallback routing requirement
- `Club12-WebClient/package.json` — pnpm 11.22.0 / node >=24 pin for builder stage
- No `.dockerignore` exists for either project — must be created

## Approaches

1. **Two independent Dockerfiles + Nginx reverse-proxy for the frontend** — frontend Nginx serves the static SPA and proxies `/api/*` to the backend container by service name; browser still calls `https://localhost:PORT/api` unmodified only in local docker-compose (same-origin via proxy), avoiding the hardcoded-`localhost` blocker for compose-based deploys.
   - Pros: no frontend code change required; works cleanly with docker-compose; single origin avoids CORS entirely for compose deployments.
   - Cons: doesn't solve a non-proxied deployment (e.g., frontend and backend on separate domains/CDN); still needs `VITE_BACKEND_PORT` to match the proxy's listen port.
   - Effort: Medium

2. **Fix `routes.ts` to read a full `VITE_API_BASE_URL`** — small code change so the frontend genuinely targets any backend host at build time (or via runtime shim).
   - Pros: unblocks real multi-host/multi-domain deployments, not just docker-compose; more correct long-term.
   - Cons: requires a source code change, tests, and review — beyond "Dockerfile only" scope requested; needs its own proposal/spec item.
   - Effort: Low-Medium (small diff, but is app code, not infra)

3. **Do both** — proxy approach for compose/local parity, plus fix `routes.ts` for real independent-host deployments.
   - Pros: most robust, future-proof.
   - Cons: larger review scope; two change surfaces (Dockerfiles + one frontend source file).
   - Effort: Medium-High

## Recommendation

Proceed with the Dockerfile/Nginx design using Approach 1 as the default (matches "Dockerfiles for deployment" scope), but explicitly surface the `routes.ts` hardcoded-`localhost` limitation to the user as a decision point before `sdd-propose` locks scope — it determines whether this stays a pure-infra change or needs a companion frontend code fix.

## Risks

1. Frontend API base URL hardcodes `localhost` — blocks any non-proxied production deployment until addressed.
2. Backend has no health check endpoint — HEALTHCHECK design needs either a new endpoint or a documented workaround.
3. `SupabaseHelper` singleton blocks synchronously in its constructor — misconfigured `SupaBase__*` env vars likely crash/hang startup.
4. Vite bakes `VITE_*` at build time — needs a build-arg-per-environment or runtime-injection decision.
5. `pg_dump` native dependency only needed if `Backup:Enabled=true` is planned for prod — decide upfront.
6. No `.dockerignore` exists yet — risk of secrets (`appsettings.{Name}.json`) or `bin/`/`obj/`/`node_modules/` leaking into build context/image layers.

## Ready for Proposal

Yes.
