# Delta for Container Deployment

> Sequencing note (resolved 2026-08-19): `docker-deployment-setup` has been archived —
> `container-deployment`'s main spec now lives at the canonical
> `openspec/specs/container-deployment/spec.md`. This delta targets that location; the
> requirement below matches its current text. No further archive-sequencing dependency remains.

## MODIFIED Requirements

### Requirement: Backend Image Excludes Secrets and Developer Files

The backend image MUST contain only `appsettings.json` and `appsettings.Development.json`; it
MUST NOT contain any per-developer `appsettings.{Name}.json` file.
(Previously: this requirement also prohibited `postgresql-client`/`pg_dump` from being installed;
that exclusion is removed — see "Backend Image Ships PostgreSQL Client for Backup and Restore"
below, which now requires it.)

#### Scenario: No developer secrets baked in

- GIVEN the built backend image
- WHEN its filesystem is inspected for `appsettings.*.json` files
- THEN only `appsettings.json` and `appsettings.Development.json` are present

## ADDED Requirements

### Requirement: Backend Image Ships PostgreSQL Client for Backup and Restore

The backend image MUST install `postgresql-client`, a major version compatible with the target
PostgreSQL server, providing both `pg_dump` and `pg_restore`/`psql` on `PATH` in the final runtime
stage.

#### Scenario: pg_dump and pg_restore are available

- GIVEN the built backend image
- WHEN a `pg_dump` and a `pg_restore` executable lookup is attempted inside the container
- THEN both binaries are found and executable

### Requirement: Compose Mounts a Persistent Backup Storage Volume

The repo-root `docker-compose.yml` MUST mount a named volume or bind mount for the `backend`
service at the configured `Backup:LocalStoragePath`, writable by the container's `$APP_UID`, and
MUST persist across container restarts and redeploys.

#### Scenario: Backup survives container restart

- GIVEN a backup file was written to `Backup:LocalStoragePath` inside a running backend container
- WHEN the container is restarted
- THEN the backup file is still present at the same path

#### Scenario: Volume is writable by the non-root app user

- GIVEN the backend container runs as non-root `$APP_UID`
- WHEN the process writes a backup file to `Backup:LocalStoragePath`
- THEN the write succeeds without a permissions error
