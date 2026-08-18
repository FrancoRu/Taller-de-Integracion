# Design: Docker deployment setup (backend + frontend)

## Technical Approach

Two independent images, one compose stack. Backend: `sdk:8.0` build → `aspnet:8.0` runtime, published from
`API/API.csproj` only (context `Club12-Backend/`, so `API.Tests` is never copied into any layer). Frontend:
`node:24` build (pnpm 11.22.0, `build/` output) → `nginx:alpine` runtime that serves the SPA and reverse-proxies
`/api` to the backend over the compose network. Backend is **not** published to the host — the browser only ever
talks to Nginx, which keeps every API call same-origin and makes CORS irrelevant for this deployment. Health
wiring follows the existing `StartupExtensions` convention (Program.cs stays a thin chain).

**Scope correction (post-design review):** `routes.ts` originally hardcoded
`` `https://localhost:${VITE_BACKEND_PORT}/api` `` — scheme *and* host, not just the port. Since the
`docker-compose.yml` is the production deployment mechanism, that string would never resolve for any real
user's browser (`localhost` resolves to the user's own machine, not the server). The fix is a relative
`apiUrl: '/api'`, which resolves against whatever origin served the page — working through the Nginx proxy
on `localhost`, an IP, or a real domain alike, with no build-time host/port baked into the bundle. This also
removes the need to pass `VITE_BACKEND_PORT` as a Docker build ARG (Decision #5 below is superseded) and
requires a small `vite.config.ts` addition so the non-Docker `pnpm dev` workflow still reaches the backend.

## Architecture Decisions

| # | Decision | Chosen | Rejected | Rationale |
|---|---|---|---|---|
| 1 | Readiness DB check | `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` + `AddDbContextCheck<ApplicationDBContext>` | `AspNetCore.HealthChecks.NpgSql` (3rd-party); hand-rolled `IHealthCheck` | First-party, matches the EF/Npgsql stack already in `Infrastructure.csproj`; `CanConnectAsync` is exactly the probe needed, zero custom code |
| 2 | `HEALTHCHECK` transport | `apt-get install curl` in the runtime stage | Compose-only healthcheck; chiseled image | `aspnet:8.0` (bookworm-slim) ships **no** curl/wget; proposal requires a `HEALTHCHECK` in the Dockerfile. ~2 MB, single cleaned layer |
| 3 | Publish scope | `dotnet publish API/API.csproj` | `dotnet publish Solution/Club12.sln` | Publishing the solution would drag `API.Tests` (xUnit + factories) into the runtime image |
| 4 | Backend host exposure | `expose: 8080` only, no `ports:` | Publish 8080 to host | Nginx is the only client; smaller attack surface; forces the same-origin path |
| 5 | Frontend API origin | **Superseded** — `apiUrl` is a relative `/api` in `routes.ts`; no `VITE_BACKEND_PORT` build ARG needed. `vite.config.ts` gains a `server.proxy` for `/api` (dev-server only, using the existing `VITE_BACKEND_PORT` env var) so `pnpm dev` keeps working. `.env` still excluded from the Docker build context (carries a real `VITE_SUPABASE` value unrelated to this fix) | Original: `VITE_BACKEND_PORT` as a build `ARG` baking an absolute URL | The absolute-URL approach only ever resolved for a browser on the same machine as the server (`localhost`); it silently broke for every real user once compose became the production mechanism. The relative URL removes the host coupling entirely and needs no build-time argument |
| 6 | Compose readiness gate | Backend compose healthcheck hits `/health/ready`; Dockerfile `HEALTHCHECK` hits `/health` | Same endpoint both places | Liveness restarts must not depend on Supabase; `depends_on: condition: service_healthy` should mean "DB reachable". Compose's healthcheck intentionally overrides the image's |
| 7 | State | No volumes, `restart: unless-stopped` | Log volume | Serilog is console-only (`appsettings.json`), `Backup:Enabled=false`, DB is Supabase-hosted — both containers are stateless |

## Data Flow

    browser ──https://localhost:${FRONTEND_PORT}──► [frontend: nginx:80]
                                                      │  /            → try_files → /index.html (SPA)
                                                      │  /api/...     → proxy_pass
                                                      ▼
                                              [backend: Kestrel:8080]  (compose network `club12`, no host port)
                                                      │
                                                      ▼
                                            Supabase Postgres + Supabase Storage (external)

    startup: ExecuteMigrationsAndSeedAsync() ──► Kestrel listens ──► /health 200 ──► /health/ready 200 (DB OK)

Migrations run **before** Kestrel binds, so probe `start_period` must cover migrate+seed.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Club12-Backend/Dockerfile` | Create | Multi-stage build; context is `Club12-Backend/` |
| `Club12-Backend/.dockerignore` | Create | Keeps `bin/`, `obj/`, `API.Tests/`, dev secrets out of the context |
| `Club12-Backend/API/Program.cs` | Modify | Add `.AddHealthChecksConfig()` to the service chain and `app.MapHealthCheckEndpoints();` next to `MapControllers()` |
| `Club12-Backend/API/Utils/StartupExtensions.cs` | Modify | Add the two extension methods (project convention: Program.cs holds no inline wiring) |
| `Club12-Backend/API/API.csproj` | Modify | `+ Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` `8.0.30` (align with the other 8.0.30 refs; if that patch is unpublished, use the highest published `8.0.*`) |
| `Club12-WebClient/src/modules/core/constants/routes.ts` | Modify | `apiUrl` → relative `/api` |
| `Club12-WebClient/vite.config.ts` | Modify | Add `server.proxy` for `/api` (dev-server only) |
| `Club12-WebClient/Dockerfile` | Create | node 24 build → `nginx:alpine` runtime |
| `Club12-WebClient/.dockerignore` | Create | Excludes `node_modules`, `build`, `.env*`, test artifacts |
| `Club12-WebClient/nginx.conf` | Create | **Server-block fragment**, copied to `/etc/nginx/conf.d/default.conf` (not a full `nginx.conf`) |
| `docker-compose.yml` | Create | Repo root; the production deployment mechanism |
| `.env.example` | Create | Repo root; every key, placeholders only |
| `.gitignore` | Verify | `.env` is **already** ignored (line 452) and `.env.example` is not matched by that pattern — only add a clarifying comment; no functional change needed |

## Interfaces / Contracts

**Backend Dockerfile (context `Club12-Backend/`)**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY API/API.csproj API/
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
RUN dotnet restore API/API.csproj
COPY API/ API/
COPY Application/ Application/
COPY Domain/ Domain/
COPY Infrastructure/ Infrastructure/
RUN dotnet publish API/API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "API.dll"]
```

`API.Tests/` and `Solution/` are never `COPY`ed. `$APP_UID` (1654) is predefined by the .NET 8 image; `apt-get`
must run before `USER`.

**Health wiring** (`StartupExtensions.cs`, called from `Program.cs`)

```csharp
public static IServiceCollection AddHealthChecksConfig(this IServiceCollection services)
{
    services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddDbContextCheck<ApplicationDBContext>("db", tags: ["ready"]);
    return services;
}

public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
{
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live"),
    }).AllowAnonymous();

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
    }).AllowAnonymous();

    return app;
}
```

Usings: `Microsoft.AspNetCore.Diagnostics.HealthChecks`, `Microsoft.Extensions.Diagnostics.HealthChecks`,
`Infrastructure.Persistance`. No fallback authorization policy exists, so both endpoints are anonymous already;
`.AllowAnonymous()` is defensive. `MustChangePasswordMiddleware` only rejects requests carrying a
`MustChangePassword=true` claim, so unauthenticated probes pass through it untouched.

**Frontend Dockerfile (context `Club12-WebClient/`)**

```dockerfile
FROM node:24-bookworm-slim AS build
WORKDIR /src
RUN corepack enable
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
RUN corepack prepare pnpm@11.22.0 --activate && pnpm install --frozen-lockfile
COPY . .
RUN pnpm build          # tsc && vite build → build/ (apiUrl is relative, no build ARG needed)

FROM nginx:alpine AS runtime
COPY --from=build /src/build /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

`bookworm-slim` (not alpine) for the builder: `@vitejs/plugin-react-swc` needs native SWC bindings, and glibc
builds are the safe default. `tsconfig.json` has `include: ["src"]`, so excluding `e2e/` from the context is safe.
`pnpm-workspace.yaml` MUST be copied alongside `package.json`/`pnpm-lock.yaml` — it holds the `allowBuilds`
config pnpm 11 requires; without it `pnpm install --frozen-lockfile` hard-fails with `ERR_PNPM_IGNORED_BUILDS`
(found during `sdd-apply`, corrected here).

**`Club12-WebClient/src/modules/core/constants/routes.ts`** (relative API URL)

```ts
const routes = {
  apiUrl: '/api',
  // ...unchanged below
```

**`Club12-WebClient/vite.config.ts`** (dev-server proxy, keeps `pnpm dev` working without Docker)

```ts
server: {
  port: parseInt(env.VITE_PORT) || 5173,
  proxy: {
    '/api': {
      target: `https://localhost:${env.VITE_BACKEND_PORT}`,
      changeOrigin: true,
      secure: false, // accept the .NET dev HTTPS certificate
    },
  },
},
```

**`Club12-WebClient/nginx.conf`**

```nginx
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    location /api/ {
        proxy_pass http://backend:8080;   # no URI part → original /api/... path is forwarded verbatim
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 60s;
    }

    location /assets/ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    location = /index.html { add_header Cache-Control "no-cache"; }

    location / { try_files $uri $uri/ /index.html; }
}
```

`backend` MUST equal the compose service name — renaming the service requires editing this file.

**`docker-compose.yml`** (repo root)

```yaml
name: club12

services:
  backend:
    build: { context: ./Club12-Backend }
    image: club12-backend:latest
    env_file: .env
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    expose: ["8080"]
    networks: [club12]
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-fsS", "http://localhost:8080/health/ready"]
      interval: 30s
      timeout: 5s
      retries: 5
      start_period: 90s

  frontend:
    build:
      context: ./Club12-WebClient
    image: club12-frontend:latest
    depends_on:
      backend: { condition: service_healthy }
    ports: ["${FRONTEND_PORT:-5001}:80"]
    networks: [club12]
    restart: unless-stopped

networks:
  club12: { driver: bridge }
```

The same root `.env` serves two roles: Compose auto-loads it for `${FRONTEND_PORT}` interpolation, and
`env_file: .env` injects every `__`-nested setting into the backend container.

**`.env.example`** (placeholders only, inline comments)

```dotenv
# Host port the SPA is published on.
FRONTEND_PORT=5001

# Supabase-hosted Postgres (pooler URI). Required — the app throws at startup if missing.
ConnectionStrings__DbConnection=Host=HOST;Port=5432;Database=postgres;Username=USER;Password=CHANGE_ME;SSL Mode=Require;Trust Server Certificate=true

# CORS. Unused in the proxy topology (same-origin) but the array must be non-null.
AllowedOrigins__0=https://localhost:5001

JWT__Key=CHANGE_ME_AT_LEAST_32_CHARS_LONG_SECRET
JWT__Issuer=Club12
JWT__Audience=Club12Client

Smtp__Host=smtp.example.com
Smtp__Port=587
Smtp__Username=CHANGE_ME
Smtp__Password=CHANGE_ME
Smtp__UseSsl=true          # optional, defaults true
Smtp__FromEmail=no-reply@example.com
Smtp__FromName=Club12

SupaBase__ProjectUrl=https://PROJECT.supabase.co
SupaBase__ServiceRole=CHANGE_ME_SERVICE_ROLE_KEY   # bad values hang startup (SupabaseHelper blocks in ctor)
SupaBase__BucketName=club12

AdminUser__Email=admin@example.com
AdminUser__Password=CHANGE_ME_STRONG_PASSWORD      # first-run seeding only

Frontend__MagicLinkUrl=https://localhost:5001/magic-link
Frontend__PasswordResetUrl=https://localhost:5001/reset-password
```

**`.dockerignore` — `Club12-Backend/`**

```
**/bin/
**/obj/
**/.vs/
**/*.user
**/appsettings.*.json     # dev secrets; also matches appsettings.Development.json — negate it below
!API/appsettings.Development.json
API.Tests/
Solution/
.git
Dockerfile
.dockerignore
```

**`.dockerignore` — `Club12-WebClient/`**

```
node_modules
build
dist
coverage
test-results
playwright-report
blob-report
e2e
.env
.env.*
.git
.vscode
*.log
Dockerfile
.dockerignore
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `/health` returns 200 without touching the DB | `API.Tests` `CustomWebApplicationFactory` GET `/health` (anonymous) |
| Unit | `/health/ready` reports the DB check | Factory GET `/health/ready`; assert 200 with a reachable DB, 503 with a bogus `ConnectionStrings__DbConnection` |
| Integration | Images build from a clean checkout | `docker build` for both contexts; assert `API.Tests.dll`/`node_modules` absent, `id -u` ≠ 0 in the backend image |
| E2E | Stack comes up and routes | `docker compose up -d`; backend reports `healthy`; SPA deep link returns 200 `index.html`; `/api/...` reaches Kestrel |
| Static | No hardcoded API host in the bundle | Grep the built `build/` JS output for `localhost` — must not appear as part of an API base URL |

## Threat Matrix

Routing (Nginx `location` blocks) and process integration (`ENTRYPOINT`/`HEALTHCHECK`) are touched; the reference
matrix rows are VCS/PR-oriented and do not apply.

| Boundary | Applicability | Design response |
|---|---|---|
| Documentation-like paths | N/A — no file-classification or executable-content logic |
| Git repository selection | N/A — no `git` invocation in this change |
| Commit state | N/A — no VCS automation |
| Push state | N/A — no VCS automation |
| PR commands | N/A — no PR automation |
| **Nginx path routing** (added) | Applicable | `/api/` uses a URI-less `proxy_pass` so paths forward verbatim (no prefix rewrite / traversal reshaping); `try_files` falls back to `index.html` and never proxies unknown paths. E2E asserts `/api/...` reaches the backend and `/deep/link` returns the SPA |
| **Container probe shell** (added) | Applicable | `HEALTHCHECK` runs a fixed `curl` argv against a hardcoded loopback URL — no interpolation of external input; failure exits non-zero and Docker marks the container unhealthy |

## Migration / Rollout

No data migration. EF migrations keep running at startup under a **single** backend replica (locked in the
proposal). Rollout: create `.env` on the deploy host from `.env.example`, `docker compose build`,
`docker compose up -d`. Rollback: `docker compose down` and revert the PR — most files are new; the modified
files (`Program.cs` health registration, `routes.ts` relative `apiUrl`, `vite.config.ts` dev proxy) are all
additive/backward-compatible with the pre-change dev workflow.

## Open Questions

- [x] **`https://localhost` coupling (was highest risk) — RESOLVED.** User confirmed the compose stack is the
  production mechanism, so `routes.ts` now uses a relative `apiUrl: '/api'` instead of the hardcoded
  `https://localhost:${VITE_BACKEND_PORT}/api`. This resolves the coupling without the larger
  `VITE_API_BASE_URL` rework: the bundle carries no host, so it works behind Nginx on any host/domain the
  frontend container is actually served from. TLS termination in front of that origin is still out of scope
  for this change (unchanged).
- [ ] Confirm `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` `8.0.30` restores; otherwise pin the highest published `8.0.*`.
- [ ] Confirm 90s `start_period` covers migrate + seed against the Supabase pooler on a cold start.
