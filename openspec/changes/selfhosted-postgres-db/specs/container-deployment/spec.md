# Container Deployment — Delta Spec

## Purpose

Extend the compose topology with a first-party, network-private Postgres
service so the backend talks to its database over the internal bridge network
instead of a remote managed host. This removes "a bundled Postgres container"
from the base spec's Out-of-Scope list for the database dimension only; TLS
termination, CI/CD, and multi-replica backend remain out of scope.

## ADDED Requirements

### Requirement: Compose Includes a Network-Private Postgres Service

The repo-root `docker-compose.yml` MUST define a `db` service running an
official `postgres` image whose major version matches the
`postgresql-client-*` package installed in the backend `Dockerfile`. The
service MUST persist data to a volume that survives container recreation (a
named volume or a host bind mount outside the container's root filesystem),
MUST attach to the shared `club12` network, and MUST NOT publish any port to
the host. Its `POSTGRES_USER`, `POSTGRES_PASSWORD`, and `POSTGRES_DB` MUST come
from the gitignored `.env`, never inline literals.

#### Scenario: Database service is defined and private

- GIVEN the repo-root `docker-compose.yml`
- WHEN the `db` service is inspected
- THEN it uses an official `postgres` image with a pinned major version
- AND it mounts a persistent volume (named volume or host bind mount) at the
  Postgres data directory
- AND it is attached to the `club12` network
- AND it publishes no `ports:` entry to the host

#### Scenario: Database credentials come from .env

- GIVEN `docker-compose.yml` and `.env.example`
- WHEN the `db` service environment is inspected
- THEN `POSTGRES_USER`, `POSTGRES_PASSWORD`, and `POSTGRES_DB` are sourced from
  `.env` (variable substitution or `env_file`), with no literal secret in
  `docker-compose.yml`
- AND `.env.example` lists all three keys with placeholder values

#### Scenario: Client tool version parity

- GIVEN the backend `Dockerfile` installs `postgresql-client-<N>`
- WHEN the `db` service image tag is read
- THEN its major version is `<N>`

### Requirement: Database Service Declares a Healthcheck

The `db` service MUST declare a healthcheck that verifies the server accepts
connections (e.g. `pg_isready`).

#### Scenario: Orchestrator can observe database health

- GIVEN the `db` service definition
- WHEN its `healthcheck` is read
- THEN it runs a command that succeeds only when Postgres is accepting
  connections

### Requirement: Backend Starts Only After the Database Is Healthy

The `backend` service MUST declare `depends_on` on `db` with
`condition: service_healthy`, so the backend's startup migration and seed run
against a reachable database.

#### Scenario: Backend waits for the database

- GIVEN `docker compose up` is run from a clean state
- WHEN the services start
- THEN `backend` does not start until the `db` healthcheck reports healthy

### Requirement: Backend Connects to the Database Over the Internal Network Without TLS

The backend's `ConnectionStrings__DbConnection` MUST address the database by
its compose service name (`Host=db`) and MUST NOT require TLS (`SSL Mode` is
`Disable` or `Prefer`), which is safe only because the `db` service publishes
no host port. `.env.example` MUST document this connection-string shape as a
placeholder, not the previous remote-managed-host shape.

#### Scenario: Connection string targets the internal service

- GIVEN `.env.example`
- WHEN the `ConnectionStrings__DbConnection` placeholder is read
- THEN its host is the `db` service name
- AND it does not require TLS
- AND it contains no real credential

## MODIFIED Requirements

### Requirement: Compose Wiring Sources Secrets Only From a Gitignored .env

The repo-root `docker-compose.yml` MUST place all services on a shared
network, MUST source the backend's environment variables via `env_file` from a
`.env` file, MUST source the `db` service's `POSTGRES_*` credentials from that
same gitignored `.env`, MUST NOT hardcode any secret value inline in
`docker-compose.yml`, and the repo's `.gitignore` MUST exclude `.env`. A
`.env.example` MUST document every required key — backend and database — with
a placeholder (no real value).

#### Scenario: Services share a network

- GIVEN `docker compose up` is run
- WHEN all services are started
- THEN `backend`, `frontend`, and `db` are attached to a common compose
  network and the backend can reach `db` by service name

#### Scenario: No secret is committed

- GIVEN the repository's tracked files
- WHEN `docker-compose.yml` and `.gitignore` are inspected
- THEN `docker-compose.yml` contains no literal secret value
- AND `.gitignore` excludes `.env`
- AND `.env.example` lists every key required by `docker-compose.yml`
  (backend `env_file` keys and the `db` `POSTGRES_*` keys) with a placeholder,
  not a real value

## Non-Goals

- TLS between `backend` and `db`, or exposing `db` outside the compose network.
- PgBouncer / an external connection pooler.
- HA Postgres, replication, or managed failover.
- The `pg_dump` / `postgresql-client` image-exclusion requirement in the base
  spec — its contradiction with the current `Dockerfile` is owned by the
  `database-backup-restore` change, not this one.
- Automating the one-time data restore in the compose stack (operator runbook).
