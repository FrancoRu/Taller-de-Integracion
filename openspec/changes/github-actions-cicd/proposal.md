# Proposal: GitHub Actions CI/CD — build to GHCR, deploy to self-hosted runners

**Touches: both** (backend + frontend images, plus repo-root orchestration). Builds on the
not-yet-archived `docker-deployment-setup` change.

## Intent

Images exist but nothing builds or ships them. Every deploy is a manual SSH + `docker build` on the
server: slow, unreproducible, and with no way to roll back to the image that was running a minute ago.
This change makes a push to `develop` build the affected service image, publish it, and deploy it on
the private server automatically, keeping exactly one archived prior image for rollback.

## Scope

### In Scope
- `.github/workflows/deploy-backend.yml` and `.github/workflows/deploy-frontend.yml` — one file per
  service, `on: push: branches: [develop]` with `paths:` filters (`Club12-Backend/**` /
  `Club12-WebClient/**`, plus `docker-compose.yml` and the workflow file itself in both, since the
  runner's checked-out compose file is what deploy executes).
- Job 1 `build` (`ubuntu-latest`): buildx build + push `ghcr.io/francoru/club12-{backend,frontend}:latest`
  (GHCR requires a lowercase path, so `FrancoRu` → `francoru`), `permissions: packages: write`.
- Job 2 `deploy` (`needs: build`, `runs-on: [self-hosted, Club-12-back-runner]` /
  `[self-hosted, Club-12-front-runner]`): idempotent `docker login ghcr.io`, **archive** the running
  image (`docker tag …:latest …:previous`, guarded for the first-ever deploy), `docker compose pull`,
  `docker compose up -d --no-deps --no-build <service>`, then `docker image prune -f`.
- `docker-compose.yml` — `image:` fields repointed to the GHCR paths so `pull` resolves against GHCR
  instead of Docker Hub; `deploy.resources.limits.memory` added per the server's house convention
  (documented in its `Readme.md` for every project — the box only has 5.7GB RAM shared with OpenMU,
  Nextcloud, and NPM).
- **Deploy path follows the server's existing project convention** (confirmed from the server's own
  `Readme.md`, not invented): compose files live at `/home/docker-compose/<project>/`, persistent data
  at `/home/docker/<project>/`. `/home/docker-compose/Club12/` already exists. The deploy job's checkout
  is purely a source for the *current* `docker-compose.yml`; it copies that one file into
  `/home/docker-compose/Club12/docker-compose.yml` and then runs every `docker compose` command from
  that fixed directory — never from the ephemeral Actions workspace. The real `.env` lives permanently
  at `/home/docker-compose/Club12/.env` (created once, by hand, never by CI, never in git). Because
  `docker compose` auto-loads `.env` from its own working directory, this needs no `--env-file`
  indirection, and `actions/checkout`'s `git clean -ffdx` never runs anywhere near it — the ephemeral
  workspace and the persistent deploy directory are simply two different paths.
- First-time runner setup documentation (Docker installed, runner user in the `docker` group, runner
  registered with the exact labels above, real `.env` created at `/home/docker-compose/Club12/.env`
  from `.env.example` before the first deploy).

### Out of Scope
- Registering/provisioning the self-hosted runners (manual, needs root) — we ship YAML + docs only.
- Any change to Dockerfiles, `/health`, `/health/ready`, or app code — correct as-is.
- A PR-gated test/lint pipeline. The existing xUnit and Vitest suites are **not** wired into CI by this
  change; deploy-on-push-to-`develop` only.
- TLS/HTTPS termination, deploy notifications (Slack/Discord/email), multi-environment promotion,
  registry retention policies, `main`/production deploys.

## Capabilities

### New Capabilities
- `ci-cd-pipeline`: build-and-publish + self-hosted deploy contract, image archiving, cleanup.

### Modified Capabilities
- `container-deployment`: image source becomes a registry reference (GHCR) instead of a local tag, and
  the runtime secret file moves to a fixed host path. Delta targets the pending spec at
  `openspec/changes/docker-deployment-setup/specs/container-deployment/spec.md`.

## Approach

Split trust and duties: untrusted-but-cheap building on GitHub-hosted runners, deployment on the
machine that actually owns the containers. GHCR is the handoff boundary — the deploy job never sees
source or a build context, it only pulls an immutable artifact, so a deploy can never silently
"succeed" by locally rebuilding something the build job never validated (hence `--no-build`).
`--no-deps` keeps each workflow inside its own service, honoring the `depends_on` wiring without one
workflow restarting the other's container. Archiving is explicit (`:previous` retag *before* pull),
because moving a `latest` tag orphans the old image rather than preserving it. Pruning is limited to
dangling images so `:latest` and `:previous` survive.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `.github/workflows/deploy-backend.yml` | New | Build→GHCR, deploy on `Club-12-back-runner` |
| `.github/workflows/deploy-frontend.yml` | New | Build→GHCR, deploy on `Club-12-front-runner` |
| `docker-compose.yml` | Modified | GHCR `image:` refs; `deploy.resources.limits.memory` per house convention |
| Deployment docs (README section) | New/Modified | Runner prerequisites, `/home/docker-compose/Club12/.env` setup |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| `actions/checkout`'s `git clean -ffdx` deletes the production `.env` | Eliminated | `.env` never lives in the Actions workspace at all — it lives permanently at `/home/docker-compose/Club12/.env`, a path the checkout step never touches. The workspace can be cleaned freely. |
| GHCR package is private by default even for a public repo → `pull` 401/403 | High | Deploy job always `docker login ghcr.io` with a `read:packages` token; docs note optional visibility flip |
| Compose `build:` kept for local dev causes a silent local rebuild if `pull` fails | Med | `up -d --no-build`; deploy fails loudly instead of shipping an unpublished image |
| Runner user lacks `docker` permission → every deploy fails | Med | Documented prerequisite (`docker` group); first run surfaces it immediately |
| Deploying a `docker-compose.yml` change requires the deploy job to overwrite the server's copy | Med | Deploy job always re-copies the repo's `docker-compose.yml` over the server's before running compose — the server's copy is a mirror, not a hand-edited source of truth, so it can never drift silently |
| Only 5.7GB RAM shared with OpenMU/Nextcloud/NPM on the same box | Med | `deploy.resources.limits.memory` set per house convention; sized conservatively for a .NET API + Nginx static site |
| Concurrent pushes deploy out of order | Low | `concurrency` group per workflow, cancel-in-progress off |

## Rollback Plan

Two layers. **Pipeline**: all files are new except `docker-compose.yml`; revert the PR and deployment
returns to the manual `docker build` + `docker compose up` flow with local image tags, with zero
runtime impact on running containers. **Deployment**: on the server, `docker tag <image>:previous
<image>:latest && docker compose up -d --no-deps --no-build <service>` restores the immediately prior
image without GitHub involvement.

## Dependencies

- Two self-hosted runners registered with labels `Club-12-back-runner` / `Club-12-front-runner`, both
  on the existing private server (192.168.0.200), Docker Engine + Compose v2 installed, runner user in
  the `docker` group. **User-provisioned** (already has `/home/ghrunner/actions-runner`).
- `/home/docker-compose/Club12/.env` created by hand from `.env.example` before the first deploy —
  the directory already exists (matches the server's established per-project convention).
- GHCR write access via the workflow `GITHUB_TOKEN`; read access available to the deploy job.

## Success Criteria

- [ ] A push to `develop` touching only `Club12-Backend/**` runs the backend workflow and not the frontend one (and vice versa).
- [ ] `build` publishes `ghcr.io/francoru/club12-<service>:latest`; `deploy` runs only after it succeeds.
- [ ] After a deploy, `<image>:previous` points at the image that was running before, and `<image>:latest` at the new one.
- [ ] The running container serves the new code; the untouched service's container is not restarted.
- [ ] `.env` still exists on the runner host after a deploy, and the service picks up its values.
- [ ] `docker images` shows no growing pile of dangling images; `:latest` and `:previous` survive pruning.
- [ ] A first-ever deploy (no pre-existing `:latest`) succeeds without failing the archive step.

## Proposal question round — all resolved

1. **Same host?** ✅ Confirmed — both runner labels run on the same private server
   (`192.168.0.200`, `franco`), sharing the `club12` Docker network and one compose project directory
   at `/home/docker-compose/Club12/`. No redesign needed.
2. **`.env` path** ✅ Confirmed as `/home/docker-compose/Club12/.env`, matching the server's own
   documented per-project convention (not the earlier placeholder `/opt/club12/.env`).
3. **Keep `build:` in compose** ✅ Confirmed — keep it, with `--no-build` enforced on every deploy run.
4. **Tagging** ✅ Confirmed — `latest` + `previous` only, no immutable `sha-<commit>` tag. Rollback is
   one step deep; no further audit trail. Accepted as sufficient for now.
