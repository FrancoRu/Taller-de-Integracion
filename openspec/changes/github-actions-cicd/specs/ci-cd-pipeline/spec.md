# CI/CD Pipeline Specification

## Purpose

Define the build-and-publish-to-GHCR plus self-hosted-deploy contract for
`Club12-Backend` and `Club12-WebClient`: what triggers a workflow, what the
`build` and `deploy` jobs must guarantee, and how a deploy survives without
touching the production `.env` or the untouched sibling service.

## Requirements

### Requirement: Path-Filtered Workflow Triggers

Each service MUST have its own workflow that triggers only `on: push:
branches: [develop]`, filtered by `paths:` to that service's directory, the
repo-root `docker-compose.yml`, and the workflow file itself.

#### Scenario: Backend-only push runs only the backend workflow

- GIVEN a push to `develop` that only touches files under `Club12-Backend/**`
- WHEN GitHub evaluates workflow triggers
- THEN `deploy-backend.yml` runs
- AND `deploy-frontend.yml` does not run

#### Scenario: Frontend-only push runs only the frontend workflow

- GIVEN a push to `develop` that only touches files under
  `Club12-WebClient/**`
- WHEN GitHub evaluates workflow triggers
- THEN `deploy-frontend.yml` runs
- AND `deploy-backend.yml` does not run

#### Scenario: A compose or workflow-file change runs both workflows

- GIVEN a push to `develop` that touches `docker-compose.yml`
- WHEN GitHub evaluates workflow triggers
- THEN both `deploy-backend.yml` and `deploy-frontend.yml` run

### Requirement: Build Job Publishes to GHCR

The `build` job MUST run on `ubuntu-latest`, MUST declare `permissions:
packages: write`, and MUST build and push its service's image to
`ghcr.io/francoru/club12-backend:latest` or
`ghcr.io/francoru/club12-frontend:latest` respectively.

#### Scenario: A successful build publishes the expected tag

- GIVEN the backend workflow's `build` job runs on `ubuntu-latest`
- WHEN the build step completes
- THEN `ghcr.io/francoru/club12-backend:latest` exists in GHCR with the new
  image content

### Requirement: Deploy Job Runs Only After a Successful Build, on the Correct Runner

The `deploy` job MUST declare `needs: build`, MUST run on the self-hosted
runner labeled `Club-12-back-runner` (backend) or `Club-12-front-runner`
(frontend), and MUST authenticate to GHCR (`docker login ghcr.io`) before
pulling.

#### Scenario: Build failure blocks deploy

- GIVEN the `build` job fails
- WHEN the workflow evaluates the `deploy` job
- THEN `deploy` does not run

#### Scenario: Deploy authenticates before pulling

- GIVEN `build` succeeded
- WHEN the `deploy` job starts
- THEN it logs in to `ghcr.io` before any `docker compose pull` step runs

### Requirement: Deploy Runs From the Fixed Server Compose Path

The `deploy` job MUST copy the checked-out `docker-compose.yml` into
`/home/docker-compose/Club12/docker-compose.yml` and MUST run every `docker
compose` command with that directory as its working directory, never the
ephemeral Actions workspace.

#### Scenario: Compose commands run against the server's persistent directory

- GIVEN the deploy job has copied `docker-compose.yml` into
  `/home/docker-compose/Club12/`
- WHEN `docker compose pull` and `docker compose up` are invoked
- THEN both run with `/home/docker-compose/Club12/` as the working directory

#### Scenario: Production .env survives the deploy

- GIVEN `/home/docker-compose/Club12/.env` exists on the runner host and is
  not tracked in git
- WHEN the deploy job runs, including `actions/checkout`'s default cleaning
  of the Actions workspace
- THEN `/home/docker-compose/Club12/.env` still exists afterward and is
  loaded by `docker compose`

### Requirement: Running Image Is Archived Before Pull

Before pulling the new image, the `deploy` job MUST retag the
currently-present `:latest` image as `:previous`. This step MUST be guarded
so that a first-ever deploy, where no `:latest` image yet exists, does not
fail the workflow.

#### Scenario: A normal deploy archives the previous image

- GIVEN an image already tagged `:latest` is present on the runner host
- WHEN the deploy job runs
- THEN that image is retagged `:previous` before the new image is pulled

#### Scenario: First-ever deploy does not fail on archiving

- GIVEN no image tagged `:latest` exists yet on the runner host
- WHEN the deploy job runs
- THEN the archive step does not fail the workflow
- AND the deploy proceeds to pull and start the new image

### Requirement: Deploy Never Rebuilds Locally

The `deploy` job MUST bring up the service with `docker compose up --no-build`
so a failed or stale pull fails the deploy instead of silently building an
unvalidated image on the runner.

#### Scenario: A failed pull fails the deploy loudly

- GIVEN `docker compose pull` fails to fetch the new image
- WHEN `docker compose up --no-build` is subsequently invoked
- THEN the command fails instead of building the image locally
- AND the workflow run is marked failed

### Requirement: Deploy Is Isolated to Its Own Service

The `deploy` job MUST run `docker compose up` with `--no-deps` and MUST only
start/restart its own service's container.

#### Scenario: Backend deploy does not touch the frontend container

- GIVEN the backend workflow's `deploy` job runs
- WHEN it brings the backend service up with `--no-deps`
- THEN the frontend container is not restarted

#### Scenario: Frontend deploy does not touch the backend container

- GIVEN the frontend workflow's `deploy` job runs
- WHEN it brings the frontend service up with `--no-deps`
- THEN the backend container is not restarted

### Requirement: Post-Deploy Cleanup Prunes Only Dangling Images

After deploy, the `deploy` job MUST prune dangling/untagged images. The
`:latest` and `:previous` tags MUST survive pruning.

#### Scenario: Dangling images are removed, tagged images survive

- GIVEN the deploy job has completed a pull and archive cycle, leaving
  untagged image layers behind
- WHEN `docker image prune -f` runs
- THEN the untagged/dangling images are removed
- AND the images tagged `:latest` and `:previous` still exist

### Requirement: Overlapping Deploys Are Serialized

Each workflow MUST declare a `concurrency` group so that two overlapping
pushes to `develop` cannot deploy out of order.

#### Scenario: A second push waits for the first deploy to finish

- GIVEN a push to `develop` triggers a workflow run that is still in its
  `deploy` job
- WHEN a second push to `develop` triggers the same workflow before the first
  run finishes
- THEN the workflow's `concurrency` group ensures the runs do not deploy out
  of order

## Out of Scope

- Registering or provisioning the self-hosted runners (manual, root-level
  server setup).
- Any change to Dockerfiles, `/health`, `/health/ready`, or other app code.
- A PR-gated test/lint pipeline; the existing xUnit and Vitest suites are not
  wired into CI by this change.
- TLS/HTTPS termination.
- Deploy notifications (Slack/Discord/email).
- Multi-environment promotion (staging, `main`/production deploys).
- An immutable `sha-<commit>` tag / multi-step rollback audit trail beyond
  `:latest` and `:previous`.
- NPM (Nginx Proxy Manager) or Cloudflare DNS wiring for the deployed
  services.
