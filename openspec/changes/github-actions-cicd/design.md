# Design: GitHub Actions CI/CD — build to GHCR, deploy to self-hosted runners

## Technical Approach

Two mirrored workflows, one per service, each with two jobs. `build` runs on `ubuntu-latest`, does a
buildx build of the service's context and pushes `ghcr.io/francoru/club12-<service>:latest`. `deploy`
runs on the matching self-hosted runner label, syncs the repo's `docker-compose.yml` into the server's
fixed project directory `/home/docker-compose/Club12/`, archives the currently-running image as
`:previous`, pulls, and restarts only its own service with `--no-deps --no-build`. GHCR is the trust
boundary: the deploy host never sees source or a build context.

`docker-compose.yml` gains GHCR `image:` references (so `pull` resolves against GHCR instead of Docker
Hub) and `deploy.resources.limits.memory` per the server's house convention. `build:` stays as the
local-dev fallback; `--no-build` guarantees CI never silently uses it.

## Architecture Decisions

| # | Decision | Chosen | Rejected | Rationale |
|---|---|---|---|---|
| 1 | Archive guard | `if docker image inspect "$IMAGE:latest" >/dev/null 2>&1; then docker tag …; fi` | `docker tag … \|\| true` | `\|\| true` swallows **every** failure — daemon down, permission denied, disk full — and reports success, so a broken archive silently costs the rollback image. The `if` guard distinguishes exactly one expected condition (first-ever deploy, no local `:latest`) from real failures, which still abort the step under the runner's `bash -e`. Cost is one extra line. |
| 2 | Cross-workflow deploy serialization | Workflow-level `concurrency` per workflow **plus** a job-level `concurrency` on `deploy` with a shared group `club12-deploy-${{ github.ref }}` | Workflow-level group only | Both runners are on the **same host** and the **same compose project**. A commit touching both services fires both workflows; two simultaneous `docker compose up` runs on one project can race on `club12` network creation and on the shared compose file write. The shared job-level group serializes only the two deploy jobs, leaving both builds parallel. No deadlock: the groups are disjoint at workflow level. |
| 3 | Registry login | `docker/login-action@v3` in both jobs | Raw `docker login … --password-stdin` in `run:` | Same action in both jobs; keeps the token out of `argv` and out of the runner's shell history. It is idempotent — it rewrites the runner user's `~/.docker/config.json` each run — which is exactly the required behavior. |
| 4 | Backend memory limit | `512m` | `256m` (too tight), `1g` (wasteful on a 5.7 GB shared box) | ASP.NET Core 8 + EF Core + Identity + Serilog at league-sized traffic sits around 150–250 MB RSS. .NET 8 reads the container cgroup limit and sets its GC hard limit to **75 % of it** (384 MB here), leaving real headroom over steady state without letting a leak starve OpenMU/Nextcloud/NPM. |
| 5 | Frontend memory limit | `128m` | `64m` | `nginx:alpine` serving pre-built static assets runs at ~10–20 MB RSS; 128 m absorbs worker spikes and page cache while costing almost nothing. |
| 6 | Build cache | `cache-from/cache-to: type=gha` | No cache | Cold `dotnet restore` and `pnpm install --frozen-lockfile` dominate build time. `type=gha` needs no extra permissions beyond the run token. **Drop these two lines if the Actions cache misbehaves** — they are a pure optimization, not a correctness dependency. |
| 7 | Manifest shape | `provenance: false` | buildx default (attestation manifest) | The default emits an OCI image index with an `unknown/unknown` attestation entry that clutters the GHCR package view and complicates single-arch pulls. This deployment is single-platform `linux/amd64`; a plain manifest is simpler to reason about. |
| 8 | Deploy checkout | Full `actions/checkout@v4` with `persist-credentials: false` | `sparse-checkout: docker-compose.yml` | Sparse checkout saves a few seconds on a LAN runner but adds a config surface that can silently produce an empty workspace. `persist-credentials: false` is kept because the deploy job never pushes and the workspace persists between runs on a self-hosted runner. |
| 9 | Manual re-run | `workflow_dispatch:` added to both `on:` blocks | Push-only | The first deploy after creating `.env` by hand, and any redeploy that follows a server-side fix, have no commit to push. One line; remove it if push-only triggering is preferred. |
| 10 | Deployment doc location | New root-level `DEPLOYMENT.md`, linked from `README.md` | `docs/deployment.md`; a `README.md` section; `openspec/changes/.../setup-instructions.md` | The repo already keeps long-form docs as root-level markdown linked from the README (`MANUAL_USUARIO.md`, README line 158) — no `docs/` directory exists. A README section would bury an ops runbook inside a 160-line architecture overview aimed at the course evaluator. An `openspec/changes/` file disappears at archive time, but the runbook must outlive the change. |
| 11 | Doc language | Spanish prose, verbatim command blocks | English | `README.md` and `MANUAL_USUARIO.md` are Spanish; a deployment runbook is a repo document, not an SDD artifact. Commands, paths, and label strings stay verbatim. |

## Data Flow

    push → develop  (paths: Club12-Backend/**)
        │
        ├─ job build   [ubuntu-latest]
        │     checkout → buildx → login ghcr.io → build+push
        │                                    └──► ghcr.io/francoru/club12-backend:latest
        │
        └─ job deploy  [self-hosted, Club-12-back-runner]   (needs: build)
              checkout ──► $GITHUB_WORKSPACE/docker-compose.yml   (ephemeral, git-cleaned)
                              │ install -m 0644
                              ▼
              /home/docker-compose/Club12/          ← fixed, persistent, cwd for every compose call
                  ├── docker-compose.yml            ← mirror, overwritten every deploy
                  └── .env                          ← hand-created once, NEVER touched by CI
                              │
              tag :latest→:previous ─► compose pull backend ─► compose up -d --no-deps --no-build backend
                              │
                              └─► docker image prune -f   (dangling only; :latest and :previous are tagged)

The two paths never intersect: `actions/checkout`'s `git clean -ffdx` runs in `$GITHUB_WORKSPACE`,
`.env` lives in `/home/docker-compose/Club12/`. Compose auto-loads `.env` from its own working
directory, so no `--env-file` indirection is needed.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `.github/workflows/deploy-backend.yml` | Create | build→GHCR + deploy on `Club-12-back-runner` |
| `.github/workflows/deploy-frontend.yml` | Create | Mirror for `Club12-WebClient` / `Club-12-front-runner` |
| `docker-compose.yml` | Modify | GHCR `image:` refs; `deploy.resources.limits.memory` on both services |
| `DEPLOYMENT.md` | Create | Server prerequisites, runner registration, `.env` bootstrap, rollback |
| `README.md` | Modify | One link line to `DEPLOYMENT.md` under "Cómo correrlo" |

## Interfaces / Contracts

### `.github/workflows/deploy-backend.yml`

```yaml
name: Deploy backend

on:
  push:
    branches: [develop]
    paths:
      - 'Club12-Backend/**'
      - 'docker-compose.yml'
      - '.github/workflows/deploy-backend.yml'
  workflow_dispatch:

concurrency:
  group: deploy-backend-${{ github.ref }}
  cancel-in-progress: false

env:
  IMAGE: ghcr.io/francoru/club12-backend
  SERVICE: backend
  DEPLOY_DIR: /home/docker-compose/Club12

jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    permissions:
      contents: read
      packages: write
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Set up Buildx
        uses: docker/setup-buildx-action@v3

      - name: Log in to GHCR
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build and push
        uses: docker/build-push-action@v6
        with:
          context: ./Club12-Backend
          push: true
          provenance: false
          tags: ${{ env.IMAGE }}:latest
          cache-from: type=gha
          cache-to: type=gha,mode=max

  deploy:
    needs: build
    runs-on: [self-hosted, Club-12-back-runner]
    timeout-minutes: 15
    permissions:
      contents: read
      packages: read
    concurrency:
      group: club12-deploy-${{ github.ref }}
      cancel-in-progress: false
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          persist-credentials: false

      - name: Log in to GHCR
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Sync compose file to the server project directory
        run: |
          set -euo pipefail
          if [ ! -d "$DEPLOY_DIR" ]; then
            echo "::error::$DEPLOY_DIR does not exist on this runner host. Create it before deploying."
            exit 1
          fi
          if [ ! -f "$DEPLOY_DIR/.env" ]; then
            echo "::error::$DEPLOY_DIR/.env is missing. Create it once by hand from .env.example (see DEPLOYMENT.md)."
            exit 1
          fi
          install -m 0644 docker-compose.yml "$DEPLOY_DIR/docker-compose.yml"
          echo "Synced docker-compose.yml -> $DEPLOY_DIR/docker-compose.yml"

      - name: Archive the currently running image as :previous
        working-directory: /home/docker-compose/Club12
        run: |
          set -euo pipefail
          if docker image inspect "$IMAGE:latest" >/dev/null 2>&1; then
            docker tag "$IMAGE:latest" "$IMAGE:previous"
            echo "Archived $IMAGE:latest -> $IMAGE:previous"
          else
            echo "No local $IMAGE:latest yet (first deploy) - nothing to archive."
          fi

      - name: Pull the new image
        working-directory: /home/docker-compose/Club12
        run: |
          set -euo pipefail
          docker compose pull "$SERVICE"

      - name: Restart the service
        working-directory: /home/docker-compose/Club12
        run: |
          set -euo pipefail
          docker compose up -d --no-deps --no-build "$SERVICE"
          docker compose ps "$SERVICE"

      - name: Prune dangling images
        run: |
          set -euo pipefail
          docker image prune -f
```

`working-directory` is a literal path because GitHub does not expand `env` context inside that key.
`$DEPLOY_DIR` is still used inside `run:` blocks, where it is a real shell variable.

### `.github/workflows/deploy-frontend.yml`

Byte-identical to the above except for these six values:

| Key | Value |
|---|---|
| `name` | `Deploy frontend` |
| `on.push.paths` | `Club12-WebClient/**`, `docker-compose.yml`, `.github/workflows/deploy-frontend.yml` |
| workflow `concurrency.group` | `deploy-frontend-${{ github.ref }}` |
| `env.IMAGE` | `ghcr.io/francoru/club12-frontend` |
| `env.SERVICE` | `frontend` |
| `build` step `context` | `./Club12-WebClient` |
| `deploy.runs-on` | `[self-hosted, Club-12-front-runner]` |

The `deploy` job's `concurrency.group` stays `club12-deploy-${{ github.ref }}` — shared on purpose
(Decision #2). `env.DEPLOY_DIR` and every `working-directory` stay `/home/docker-compose/Club12`.

### `docker-compose.yml` diff

```diff
 services:
   backend:
     build: { context: ./Club12-Backend }
-    image: club12-backend:latest
+    image: ghcr.io/francoru/club12-backend:latest
     env_file: .env
     environment:
       ASPNETCORE_ENVIRONMENT: Production
     expose: ["8080"]
     networks: [club12]
     restart: unless-stopped
+    deploy:
+      resources:
+        limits:
+          memory: 512m
     healthcheck:
       test: ["CMD", "curl", "-fsS", "http://localhost:8080/health/ready"]
       interval: 30s
       timeout: 5s
       retries: 5
       start_period: 90s
 
   frontend:
     build:
       context: ./Club12-WebClient
-    image: club12-frontend:latest
+    image: ghcr.io/francoru/club12-frontend:latest
     depends_on:
       backend: { condition: service_healthy }
     ports: ["${FRONTEND_PORT:-5001}:80"]
     networks: [club12]
     restart: unless-stopped
+    deploy:
+      resources:
+        limits:
+          memory: 128m
```

Everything else — `name: club12`, `env_file`, `expose`, the healthcheck, `depends_on`, `networks` — is
unchanged. Compose v2 applies `deploy.resources.limits` outside Swarm, which is why the house
convention uses it.

### `DEPLOYMENT.md` outline

1. **Prerequisites** — Debian host, Docker Engine + Compose v2 (`docker compose version`), runner user
   in the `docker` group (`sudo usermod -aG docker ghrunner`, then re-login; verify with
   `docker ps` as that user).
2. **Runner registration (manual, by hand, once per runner).** Get a token from
   `https://github.com/FrancoRu/<repo>/settings/actions/runners/new`, then in
   `/home/ghrunner/actions-runner`:

   ```bash
   ./config.sh --url https://github.com/FrancoRu/<repo> \
               --token <REGISTRATION_TOKEN> \
               --name club12-back \
               --labels Club-12-back-runner \
               --work _work
   sudo ./svc.sh install ghrunner && sudo ./svc.sh start
   ```

   Repeat in a **second, separate** runner directory with `--name club12-front --labels
   Club-12-front-runner`. Two `runs-on` labels require two registered runners; one runner cannot hold
   two jobs at once. `self-hosted` is added automatically — do **not** pass it in `--labels`. Label
   strings are case-sensitive and must match the workflows exactly.
3. **One-time `.env` bootstrap** — `cp .env.example /home/docker-compose/Club12/.env`, fill every
   `CHANGE_ME`, `chmod 600`. CI never creates, reads, or overwrites this file; a missing `.env` fails
   the deploy loudly at the sync step.
4. **GHCR visibility** — packages are private by default even on a public repo. The deploy job's
   `docker login` covers this; the alternative is flipping the package to public in the GHCR UI.
5. **Rollback** — on the server:
   `cd /home/docker-compose/Club12 && docker tag ghcr.io/francoru/club12-backend:previous ghcr.io/francoru/club12-backend:latest && docker compose up -d --no-deps --no-build backend`
6. **Local development** — unchanged: `docker compose build && docker compose up -d` from the repo root
   still works because `build:` is retained.

## Testing Strategy

No unit-testable code is produced; verification is operational and matches the proposal's success
criteria.

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Static | Both workflows are valid | `actionlint` (or GitHub's own parse on push); YAML lint |
| Static | Compose file is valid with the new keys | `docker compose -f docker-compose.yml config` — asserts the GHCR refs and that `deploy.resources.limits.memory` survives |
| Integration | Path filters isolate services | Push a backend-only commit to `develop`; assert the frontend workflow does **not** run, and vice versa |
| Integration | Archive guard survives a first deploy | On a host with no local `:latest`, the archive step logs "nothing to archive" and exits 0 |
| E2E | Deploy lands | After a run, `docker compose ps` shows the service recreated and healthy; the untouched service's container ID is unchanged |
| E2E | Rollback restores | Retag `:previous`→`:latest`, `up -d`, confirm the prior build is serving |
| E2E | Secrets survive | `/home/docker-compose/Club12/.env` still present and unmodified (`stat`) after a deploy |

## Threat Matrix

Shell commands and process integration are added (workflow `run:` blocks driving `docker`). The
reference matrix rows are VCS/PR-oriented; two rows apply, three do not.

| Boundary | Applicability | Design response | Planned RED test |
|---|---|---|---|
| Documentation-like paths | N/A — no file classification or executable-content logic in this change | — | — |
| Git repository selection | Applicable | `actions/checkout` owns the only clone; every `docker compose` invocation is pinned to a literal `working-directory: /home/docker-compose/Club12` and never runs in `$GITHUB_WORKSPACE`. No `git -C` and no relative-path repository selection exists. | Assert compose runs resolve `/home/docker-compose/Club12/.env`, not a workspace `.env` |
| Commit state | N/A — the pipeline never stages, commits, or amends | — | — |
| Push state | N/A — no `git push`; `persist-credentials: false` removes even the ability | — | — |
| PR commands | N/A — no `gh pr` or PR automation | — | — |
| **Shell command composition** (added) | Applicable | Every `run:` block starts with `set -euo pipefail`. All interpolated values (`$IMAGE`, `$SERVICE`, `$DEPLOY_DIR`) come from workflow-authored `env:`, never from event payload, branch names, or PR titles; all are double-quoted. No `${{ }}` expression is interpolated into a shell body. | Assert no `${{ github.event.* }}` appears inside any `run:` body |
| **Failure masking** (added) | Applicable | No `\|\| true` anywhere. The one tolerated condition (missing local `:latest`) is an explicit `if docker image inspect` guard; every other docker failure aborts the job. | First-deploy run exits 0; a forced `docker tag` failure fails the job |
| **Destructive prune scope** (added) | Applicable | `docker image prune -f` (dangling only, no `-a`, no `--volumes`). `docker image prune` never removes an image referenced by an existing container, and `:latest`/`:previous` are tagged, so nothing belonging to OpenMU/Nextcloud/NPM that is in use can be collected. | After a deploy, assert the other projects' containers are still running and their images still present |

## Migration / Rollout

No data migration. Order matters: **register both runners and create
`/home/docker-compose/Club12/.env` before merging to `develop`**, otherwise the first deploy job queues
forever (no matching runner) or fails at the sync guard (no `.env`). The build job is harmless either
way — it only publishes to GHCR.

Rollback of the pipeline: revert the PR. All files are new except `docker-compose.yml`, whose only
changes are the `image:` refs and the memory limits; running containers are unaffected until the next
deploy. Rollback of a bad deploy: the `:previous` retag documented in `DEPLOYMENT.md`.

## Open Questions

- [ ] Repository slug for the `--url` in `DEPLOYMENT.md` — `https://github.com/FrancoRu/<repo>` needs the
  actual repo name filled in at apply time (read it from `git remote get-url origin`).
- [ ] First backend deploy will pull a fresh image with no layer overlap against the manually built
  local one; confirm the server has room for both `:latest` and `:previous` (~450 MB total for the
  aspnet runtime plus app layers). If disk is tight, drop `:previous` for the backend and keep
  rollback via a GHCR re-pull.
