# Delta for Container Deployment

Targets `openspec/changes/docker-deployment-setup/specs/container-deployment/spec.md`.
Both requirements below are new sections of that spec's compose-wiring
behavior; neither edits existing requirement text (the source spec does not
yet say anything about registry sourcing or memory limits), so they are
expressed as ADDED, not MODIFIED, per this project's delta conventions.

## ADDED Requirements

### Requirement: Compose Image References Resolve Against GHCR

The repo-root `docker-compose.yml` MUST reference the backend and frontend
images by their GHCR paths (`ghcr.io/francoru/club12-backend:latest` and
`ghcr.io/francoru/club12-frontend:latest`), not by local/unqualified tags,
so `docker compose pull` resolves against the registry the CI pipeline
publishes to instead of Docker Hub or a local-only tag.

#### Scenario: Pull resolves against GHCR, not a local tag

- GIVEN the repo-root `docker-compose.yml`
- WHEN its `image:` fields are inspected
- THEN the backend service references
  `ghcr.io/francoru/club12-backend:latest`
- AND the frontend service references
  `ghcr.io/francoru/club12-frontend:latest`

#### Scenario: A clean host can pull without a local build

- GIVEN a runner host with no locally-built `club12-backend` or
  `club12-frontend` image
- WHEN `docker compose pull` is run against `docker-compose.yml`
- THEN both images are fetched from GHCR by their fully-qualified references

### Requirement: Compose Services Declare Conservative Memory Limits

Both services in `docker-compose.yml` MUST declare
`deploy.resources.limits.memory`, sized conservatively because the deploy
host has 5.7GB total RAM shared with unrelated services (OpenMU, Nextcloud,
NPM). The backend (a .NET 8 ASP.NET Core API on `aspnet:8.0`) MUST be capped
at `512M`; the frontend (a static SPA served by `nginx:alpine`, no
server-side rendering or app logic) MUST be capped at `128M`. These values
are documented, not auto-tuned, and MAY be revised later based on observed
usage.

#### Scenario: Limits are declared for both services

- GIVEN the repo-root `docker-compose.yml`
- WHEN each service's `deploy.resources.limits` is inspected
- THEN the backend service declares `memory: 512M`
- AND the frontend service declares `memory: 128M`

#### Scenario: A memory-runaway backend cannot starve the shared host

- GIVEN the backend container is running under its `512M` limit
- WHEN its memory usage attempts to exceed that limit
- THEN the container is constrained/OOM-killed by Docker rather than
  consuming memory needed by OpenMU, Nextcloud, or NPM on the same host

## Out of Scope

- Registering or provisioning the self-hosted runners.
- Any change to Dockerfiles, `/health`, `/health/ready`, or other app code.
- A PR-gated test/lint pipeline.
- TLS/HTTPS termination, deploy notifications, multi-environment promotion.
- An immutable `sha-<commit>` tag / audit trail beyond `:latest` and
  `:previous` (owned by the `ci-cd-pipeline` capability, not this one).
- NPM (Nginx Proxy Manager) or Cloudflare DNS wiring.
- CPU limits or auto-tuning of the memory values based on live telemetry.
