# Container Deployment Specification

## Purpose

Define the build and runtime contract for the backend and frontend container
images and their `docker-compose.yml` wiring, so both services build
reproducibly from a clean checkout and run as the production deployment
mechanism without embedding secrets or developer-only files.

## Requirements

### Requirement: Backend Image Is a Non-Root Multi-Stage Build

The backend image MUST be produced by a multi-stage build (an SDK 8.0 build
stage compiling/publishing the app, copied into an `aspnet:8.0` runtime
stage), MUST listen on port 8080, and MUST run its final container process as
a non-root user.

#### Scenario: Clean build produces a runnable image

- GIVEN a clean checkout of `Club12-Backend`
- WHEN `docker build` runs against its Dockerfile
- THEN the build succeeds
- AND the resulting image's final stage is based on `aspnet:8.0`, not the SDK
  image

#### Scenario: Container runs unprivileged

- GIVEN the backend image is started
- WHEN the entrypoint process is inspected
- THEN it runs as a non-root user
- AND the process listens on port 8080

### Requirement: Backend Image Declares a HEALTHCHECK Against /health

The backend Dockerfile MUST declare a `HEALTHCHECK` instruction that probes
`/health`.

#### Scenario: Orchestrator can observe container health

- GIVEN the backend image's Dockerfile
- WHEN the `HEALTHCHECK` instruction is read
- THEN it targets the `/health` endpoint on the container's listening port

### Requirement: Backend Image Excludes Secrets and Developer Files

The backend image MUST contain only `appsettings.json` and
`appsettings.Development.json`; it MUST NOT contain any per-developer
`appsettings.{Name}.json` file, and MUST NOT have `postgresql-client`/`pg_dump`
installed.

#### Scenario: No developer secrets baked in

- GIVEN the built backend image
- WHEN its filesystem is inspected for `appsettings.*.json` files
- THEN only `appsettings.json` and `appsettings.Development.json` are present

#### Scenario: No pg_dump binary present

- GIVEN the built backend image
- WHEN a `pg_dump` executable lookup is attempted inside the container
- THEN no such binary is found

### Requirement: Frontend Image Serves a Built SPA via Nginx

The frontend image MUST be produced by a multi-stage build (a node 24 stage
running `pnpm install --frozen-lockfile` and `pnpm build`, copied into an
`nginx:alpine` runtime stage) that serves the `build/` output directory.

#### Scenario: Clean build produces a static-serving image

- GIVEN a clean checkout of `Club12-WebClient`
- WHEN `docker build` runs against its Dockerfile
- THEN the build succeeds using `pnpm install --frozen-lockfile` and
  `pnpm build`
- AND the final stage is `nginx:alpine` serving the `build/` output

### Requirement: Frontend Nginx Supports SPA Fallback and API Proxy

The frontend's Nginx configuration MUST serve `index.html` for any unknown
path (SPA client-side routing fallback) and MUST reverse-proxy requests under
`/api/*` to the backend service by its container/service name.

#### Scenario: Deep link resolves client-side

- GIVEN the frontend container is running
- WHEN a request is made for a path unknown to Nginx (e.g. a client-side
  route)
- THEN Nginx responds with `index.html` instead of a 404

#### Scenario: API calls reach the backend

- GIVEN both services are running on the shared compose network
- WHEN a request is made to `/api/*` against the frontend container
- THEN Nginx proxies it to the backend service by its compose service name

### Requirement: Frontend Calls the API via a Relative Same-Origin Path

The built frontend bundle MUST request the API via a relative path (`/api`),
not an absolute URL with a hardcoded host. The local Vite dev server MUST
proxy `/api` to the local backend so `pnpm dev` (non-Docker) continues to work
without code changes to the request layer.

#### Scenario: Production build has no hardcoded API host

- GIVEN the frontend production build output
- WHEN its JavaScript bundle is inspected for the API base URL
- THEN it references a relative path, not an absolute URL containing a
  hostname

#### Scenario: API calls resolve on any origin the frontend is served from

- GIVEN the frontend container is served behind Nginx on any host or domain
- WHEN a client-side request is made to the API
- THEN it resolves against the same origin the page was loaded from, and
  Nginx's `/api/*` proxy forwards it to the backend

#### Scenario: Local dev server still reaches the backend

- GIVEN a developer runs `pnpm dev` without Docker
- WHEN the frontend makes a request to `/api/*`
- THEN Vite's dev-server proxy forwards it to the local backend on
  `VITE_BACKEND_PORT`

### Requirement: Both Projects Have a Build-Context .dockerignore

Both `Club12-Backend` and `Club12-WebClient` MUST have a `.dockerignore` file
excluding `bin/`, `obj/`, `node_modules/`, `.git/`, and any secret files
(including per-developer `appsettings.{Name}.json`).

#### Scenario: Build context excludes noise and secrets

- GIVEN either project's `.dockerignore`
- WHEN the docker build context is assembled
- THEN `bin/`, `obj/`, `node_modules/`, `.git/`, and per-developer secret
  files are excluded from it

### Requirement: Compose Wiring Sources Secrets Only From a Gitignored .env

The repo-root `docker-compose.yml` MUST place both services on a shared
network, MUST source the backend's environment variables via `env_file` from
a `.env` file, MUST NOT hardcode any secret value inline in
`docker-compose.yml`, and the repo's `.gitignore` MUST exclude `.env`. A
`.env.example` MUST document every required key with a placeholder (no real
value).

#### Scenario: Services share a network

- GIVEN `docker compose up` is run
- WHEN both services are started
- THEN they are attached to a common compose network and can reach each other
  by service name

#### Scenario: No secret is committed

- GIVEN the repository's tracked files
- WHEN `docker-compose.yml` and `.gitignore` are inspected
- THEN `docker-compose.yml` contains no literal secret value
- AND `.gitignore` excludes `.env`
- AND `.env.example` lists every key required by `docker-compose.yml`'s
  `env_file` with a placeholder, not a real value

## Out of Scope

- A full `VITE_API_BASE_URL` build-time variable / multi-target-host system
  (the relative-URL requirement above already covers the same-origin case
  this deployment needs).
- `pg_dump` / `postgresql-client` installation (covered above only as an
  exclusion requirement, not as a feature).
- CI/CD pipelines, TLS termination, a bundled Postgres container, and
  multi-replica backend deployment.
