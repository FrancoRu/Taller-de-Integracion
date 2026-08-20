# Service Health Endpoint Specification

## Purpose

Define two unauthenticated backend HTTP endpoints — `/health` (liveness) and
`/health/ready` (readiness) — that container orchestrators use as build/runtime
probes. Liveness reports the process is up without touching any dependency;
readiness additionally verifies database connectivity so a misconfigured
`ConnectionStrings__DbConnection` surfaces as "not ready" instead of silently
"healthy".

## Requirements

### Requirement: Liveness Endpoint Never Touches Dependencies

The system MUST expose `GET /health` that returns HTTP 200 whenever the
process can handle a request, without performing any database call, external
network call, or other dependency check.

#### Scenario: Process is up

- GIVEN the backend process has started and is accepting requests
- WHEN a client sends `GET /health`
- THEN the response status is 200
- AND no database connection or external dependency is contacted while
  handling the request

#### Scenario: Database is unreachable

- GIVEN the configured `ConnectionStrings__DbConnection` points at an
  unreachable or misconfigured database
- WHEN a client sends `GET /health`
- THEN the response status is still 200 because liveness does not check the
  database

### Requirement: Readiness Endpoint Checks Database Connectivity

The system MUST expose `GET /health/ready` that performs a real database
connectivity check (EF Core/Npgsql, using `ConnectionStrings__DbConnection`)
and returns HTTP 200 only when that check succeeds, or HTTP 503 when it fails.

#### Scenario: Database is reachable

- GIVEN the configured database connection is reachable and accepts queries
- WHEN a client sends `GET /health/ready`
- THEN the response status is 200

#### Scenario: Database is unreachable

- GIVEN the configured database connection cannot be established (wrong
  credentials, host unreachable, or connection string missing/invalid)
- WHEN a client sends `GET /health/ready`
- THEN the response status is 503

### Requirement: Readiness Degradation Never Crashes the Process

A failed database check inside `/health/ready` MUST be caught and reported as
a 503 response; it MUST NOT throw an unhandled exception, crash the process,
or prevent other requests (including `/health`) from being served.

#### Scenario: Repeated readiness failures do not affect the process

- GIVEN the database is unreachable for an extended period
- WHEN `GET /health/ready` is called repeatedly
- THEN every call returns 503
- AND the process remains running and continues serving `/health` with 200
- AND no unhandled exception is logged as a process-level crash

### Requirement: Both Endpoints Are Unauthenticated

The system MUST allow anonymous access to `/health` and `/health/ready`; no
authentication token or cookie is required to call either endpoint.

#### Scenario: Anonymous request succeeds

- GIVEN no `Authorization` header or auth cookie is sent
- WHEN a client sends `GET /health` or `GET /health/ready`
- THEN the request is not rejected with 401 or 403 for lack of credentials
- AND the response reflects the actual liveness/readiness state (200 or 503)
