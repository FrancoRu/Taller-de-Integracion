# Tasks: GitHub Actions CI/CD — build to GHCR, deploy to self-hosted runners

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~450 (2 workflows ~118 ln each = 236, compose diff ~12, DEPLOYMENT.md ~200, README.md +1) |
| Session review budget | 800 lines (session preflight, not the skill's generic 400-line default) |
| Budget risk (vs. 800) | Low — ~450 comfortably under budget |
| Chained PRs recommended (generic 400-line heuristic) | Yes, but overridden — see resolution below |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | n/a — single PR |

Decision needed before apply: Resolved — user confirmed single PR (2026-08-18), since ~450 lines is well
within the session's actual 800-line budget. The generic "exceeds 400" trigger fired on the skill's
default threshold, not this session's configured one; asked per `ask-on-risk`, user chose to proceed
as a single PR rather than split infra/docs across two chained PRs.

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Compose GHCR refs + memory limits + both workflow YAMLs (~248 ln) | PR 1 | `docker compose -f docker-compose.yml config` | `actionlint .github/workflows/*.yml` if installed, else YAML `safe_load` per file; N/A for a real runner (none exist yet) | Revert PR restores local-tag compose + removes both workflow files, zero container impact |
| 2 | `DEPLOYMENT.md` (incl. post-merge manual checklist) + one `README.md` link line (~201 ln) | PR 2 | N/A — doc-only, no executable content | N/A — nothing to run | Revert PR removes the file and the link line only |

## Phase 1: Compose (`docker-compose.yml`)

- [x] 1.1 Apply the design.md diff: `backend.image` → `ghcr.io/francoru/club12-backend:latest`, `frontend.image` → `ghcr.io/francoru/club12-frontend:latest`; keep `build:` blocks as-is.
- [x] 1.2 Add `deploy.resources.limits.memory: 512m` to `backend`, `128m` to `frontend`, placed exactly where the diff shows (after `restart:`, before `healthcheck:`/`ports:`).
- [x] 1.3 Validate: `docker compose -f docker-compose.yml config` parses without error and echoes both GHCR image refs.
- [x] 1.4 Confirm `.env.example`-driven interpolation (e.g. `${FRONTEND_PORT:-5001}`) still resolves in the `config` output.

## Phase 2: `.github/workflows/deploy-backend.yml`

- [x] 2.1 Create the file with the exact contract from design.md (`on.push.paths`, `concurrency`, `env`, `build` job, `deploy` job, all 6 `deploy` steps).
- [x] 2.2 Line-check: `working-directory` on archive/pull/restart steps is the literal string `/home/docker-compose/Club12`, never `${{ env.DEPLOY_DIR }}`.
- [x] 2.3 Line-check: no `|| true` anywhere; archive step uses the `if docker image inspect ...` guard.
- [x] 2.4 Line-check: `docker compose up` step includes `--no-deps --no-build`; prune step is `docker image prune -f` (no `-a`, no `--volumes`).
- [x] 2.5 Line-check: workflow-level `concurrency.group: deploy-backend-${{ github.ref }}` present; `deploy` job's `concurrency.group: club12-deploy-${{ github.ref }}` present.

## Phase 3: `.github/workflows/deploy-frontend.yml`

- [x] 3.1 Create as the mirror of Phase 2, substituting the 6 values in design.md's table (`name`, `paths`, workflow `concurrency.group`, `env.IMAGE`, `env.SERVICE`, build `context`, `deploy.runs-on`).
- [x] 3.2 Confirm `deploy` job's shared `concurrency.group` stays `club12-deploy-${{ github.ref }}` (same as backend, intentional).
- [x] 3.3 Repeat the line-checks from 2.2–2.4 against this file.

## Phase 4: Documentation

- [x] 4.1 Create root `DEPLOYMENT.md` per design.md's 6-section outline (prerequisites, runner registration, `.env` bootstrap, GHCR visibility, rollback, local dev) in Spanish prose with verbatim command blocks; fill the repo slug from `git remote get-url origin`.
- [x] 4.2 Add a "Checklist post-merge" section to `DEPLOYMENT.md` (not a task this batch completes): register both runners with exact labels, create real `.env`, push a small `develop` change, confirm path filters/build/deploy/`:previous`/prune all behave as designed.
- [x] 4.3 Add one link line to `DEPLOYMENT.md` under "Cómo correrlo" in `README.md`.

## Phase 5: Static Verification

- [x] 5.1 YAML-parse both workflow files (`python -c "import yaml,sys; yaml.safe_load(open(sys.argv[1]))"` or an available linter); confirm 0 syntax errors.
- [x] 5.2 Run `actionlint` on both files if installed; if unavailable, note it and skip — not a blocker.
- [x] 5.3 Grep both files for `${{ github.event.` inside any `run:` body — must find none.
- [x] 5.4 Re-run `docker compose -f docker-compose.yml config` after Phase 1 edits to confirm no regression from later phases.
