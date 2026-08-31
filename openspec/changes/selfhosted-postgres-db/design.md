# Design: Self-Hosted Postgres Container

## Technical Approach

Add a single `db` (Postgres) service to the existing single-host `docker-compose.yml`
stack and point the backend at it over the internal `club12_club12` bridge network.
No application logic changes: the only code touched is the two `UseNpgsql(...)`
registrations in `StartupExtensions` (adding `EnableRetryOnFailure` + `CommandTimeout`).
Clean Architecture layering is untouched — this is a deployment-topology and
configuration change that lives entirely in `API` composition root + infra files.

Data is carried over by an **operator-run restore** of an existing backup, sequenced
**before** the first `backend` start so EF Core's startup `MigrateAsync()` +
`IdentitySeeder`/`DataSeeder` see an already-populated, already-migrated schema.

The schema is portable to a vanilla `postgres` image: a full migration scan shows
**no `CREATE EXTENSION`, no `citext`/`uuid-ossp`/`unaccent`**, and the single use of
`gen_random_uuid()` (`20260817082125_AddPlayerTeamRegistrationTable`) is built-in on
PG13+. Nothing Supabase-specific is relied on at the database level.

## Architecture Decisions

| # | Decision | Alternatives rejected | Rationale |
|---|----------|-----------------------|-----------|
| 1 | Postgres as a compose service on the same host as the backend | Managed Postgres in `sa-east-1` (São Paulo); keep Supabase + add PgBouncer/tuning | User chose self-hosting. Local bridge hop is sub-ms vs ~150 ms/round trip to `us-east-2`; that round-trip count is the entire bottleneck. Managed-nearer-region would cut RTT to ~30 ms but still 20× a local hop and still a monthly cost. |
| 2 | `db` image = `postgres:17-alpine` | `postgres:17` (Debian); `postgres:15-alpine` (host precedent); always-latest | Supabase is PG **17.6**. `-alpine` matches `openmu-db`/`nextcloud-db` and is ~5× smaller (the `/` partition is 31 GB). `Dockerfile` already ships `postgresql-client-17` — no change. `pg_dump`/`pg_restore` are logical, so the Supabase aarch64 → server x86-64 move is fine. |
| 3 | DB data on a **bind mount** `/home/docker/club12/db` (user-confirmed) | Default named volume | Docker's volume dir is on the 31 GB `/` partition; the DB belongs on the 420 GB `/home` partition. The host README's own rule is "persistent data under `/home/docker/<project>/`". Bind mount is explicit about where bytes land. |
| 4 | `db` **not published** on any host port; `SSL Mode=Disable` in the connection string | Publish `5432` for external tools; require TLS on the internal hop | Traffic never leaves the compose bridge. No published port ⇒ no UFW rule, no exposure, and TLS on a localhost-equivalent hop is pure overhead. External DB access, if ever needed, is `docker compose exec db psql`. |
| 5 | `db` mem limit ~512 MB, `shared_buffers` ~128 MB, `effective_cache_size` ~512 MB, default `random_page_cost` | Postgres defaults (no limit); aggressive tuning | 5.7 GB RAM already carries 2 Postgres + OpenMU + Nextcloud + backend + 2 frontends. A cap protects the host; conservative `shared_buffers` suits a small HDD box; `random_page_cost` stays at 4 because the disk is spinning. |
| 6 | `CommandTimeout(30)` on both `UseNpgsql` registrations. **No `EnableRetryOnFailure`** (was added, then removed in the follow-up — see below). | Retry-on-failure; leave defaults | A bounded timeout turns a stuck query into a fast failure. Retry was reverted: `NpgsqlRetryingExecutionStrategy` rejects the raw `BeginTransactionAsync` in `DataMaintenanceService` (only `UnitOfWork` wraps its transaction in `CreateExecutionStrategy`), and a local container DB has no transient network faults for retry to absorb. Not caught in CI because `CustomWebApplicationFactory` swaps in SQLite. |
| 7 | Cutover data move = **operator restore of a pre-made backup**, not a code path or a compose init step | `pg_dump \| psql` piped in a one-shot container; EF `MigrateAsync` on empty DB + re-seed | Preserves real data (teams, tournaments, Identity users, history). Keeping it out of code keeps the compose stack idempotent and dev-safe. Sequencing (restore → then backend) avoids the `MigrateAsync`/seed race. |
| 8 | `AsNoTracking` sweep, batched `SaveChanges`, DTO projection **excluded** | Bundle the query fixes here | Real wins but orthogonal to DB location and much larger blast radius. Separate performance follow-up after this lands and latency is re-measured. |
| 9 | No Redis / caching layer | Cache-aside on all GETs | Rejected in the analysis: write path stays slow, invalidation is hard in this relational domain, single backend replica. Revisit only if specific public endpoints stay hot post-cutover (then `HybridCache`, not Redis). |

## The `db` Service (illustrative — final form in `docker-compose.yml`)

```yaml
  db:
    image: postgres:17-alpine        # matches Supabase 17.6 + Dockerfile postgresql-client-17
    env_file: .env                   # POSTGRES_USER / POSTGRES_PASSWORD / POSTGRES_DB
    command:
      - "postgres"
      - "-c"
      - "shared_buffers=128MB"
      - "-c"
      - "effective_cache_size=512MB"
    volumes:
      - /home/docker/club12/db:/var/lib/postgresql/data
    networks: [club12]
    expose: ["5432"]                 # internal only — NO ports:
    restart: unless-stopped
    deploy:
      resources:
        limits:
          memory: 512m
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U $${POSTGRES_USER} -d $${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s

  backend:
    depends_on:
      db:
        condition: service_healthy
    # ...existing config unchanged...

volumes:
  backup-data: {}
  # no pgdata: {} — DB uses the /home bind mount (decision 3)
```

Local dev note: developers who run `docker compose up` on a laptop have no
`/home/docker/club12/db`. Docker auto-creates the bind-mount source directory on first
`up` (as root), so dev still works; the data just lands at that absolute path on the
dev machine too. If that proves annoying, a `docker-compose.override.yml` (gitignored,
already the local-dev pattern) can swap the `db` volume line for a named volume without
touching the committed file.

## Connection String

| | Value (shape, placeholders) |
|---|---|
| Before (`.env` on server, Supabase pooler) | `Host=aws-1-us-east-2.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<ref>;Password=<pw>;SSL Mode=Require;Trust Server Certificate=true` |
| After (`.env` on server, internal) | `Host=db;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>;SSL Mode=Disable` |

`.env.example` is updated to the "after" shape plus the three `POSTGRES_*` keys.

## Npgsql Options (`StartupExtensions.cs`)

```csharp
// AddDbContextConfig + AddIdentityConfig — same options both contexts
options.UseNpgsql(connectionString, npgsql =>
{
    npgsql.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorCodesToAdd: null);
    npgsql.CommandTimeout(30);
});
```

Rationale: a local DB should answer in milliseconds; a 30 s command timeout turns a
stuck query into a fast failure instead of a hung request. Retry covers container
restart / brief unavailability during deploys.

## Cutover Runbook (operator, one-time)

```
                 ┌─────────────────────────────────────────────────────────┐
                 │  PRECONDITIONS                                            │
                 │  • server .env has POSTGRES_* + new ConnectionStrings__…  │
                 │  • backup copy of the Supabase DB present on the host     │
                 │  • Supabase PG 17 == db image postgres:17-alpine          │
                 └─────────────────────────────────────────────────────────┘
Operator                     db container                 backend container
   │                              │                              │
   │ docker compose up -d db      │                              │
   │─────────────────────────────▶│  init empty PGDATA            │
   │                              │  healthcheck → healthy        │
   │ (backend NOT started yet — deploy job uses --no-deps)        │
   │                              │                              │
   │ restore backup into db       │                              │
   │  pg_restore/psql  ──────────▶│  schema + data loaded         │
   │  (via `docker compose exec -T db …` or a one-shot client)    │
   │                              │                              │
   │ docker compose up -d backend │                              │
   │────────────────────────────────────────────────────────────▶│
   │                              │  MigrateAsync() → no-op / applies only newer migrations
   │                              │  IdentitySeeder → admin exists, no-op
   │                              │  DataSeeder → Seed:Enabled=false in prod, skipped
   │                              │  /health/ready → 200          │
   │ verify: latency, storage, login, a multi-query endpoint      │
   │ keep Supabase project reachable until cutover is stable      │
   │ enable Backup:Enabled=true (app's built-in scheduled backup)  │
```

If `MigrateAsync` finds the restored schema **behind** the code's migration set, it
applies the delta — expected and fine. If it finds it **ahead**, the deploy image is
stale; roll the image forward, not the DB back.

## Deploy Workflow (`deploy-backend.yml`)

The deploy job runs `docker compose up -d --no-deps --no-build backend` as `gh-runner`.
`--no-deps` means it will not touch `db` — which is what we want:

- `db` is brought up **once** during cutover and kept alive by `restart: unless-stopped`.
  The Docker daemon is systemd-enabled, so `db` comes back after a host reboot without CI.
- **No workflow change** for the happy path.
- *Optional fast follow* (not in this change): add `docker compose up -d db` before the
  backend restart so CI self-heals if `db` is ever removed. One line, idempotent.
- Dropping `--no-deps` (would also pull `frontend`) is rejected.

## Rejected Alternatives

| Alternative | Why not |
|---|---|
| Managed Postgres in `sa-east-1` (Neon/Railway/RDS) | Cuts RTT to ~30 ms without ops burden, but still ~20× a local hop, still a recurring cost, and the user chose self-hosting. Kept as the documented fallback if the home host proves too fragile. |
| Keep Supabase pooler + `EnableRetryOnFailure` + `AsNoTracking` + batch saves | Helps, but cannot beat physics: 20 sequential round trips × 150 ms is still 3 s. The query fixes are worth doing *after* the move. |
| Redis / `IMemoryCache` cache-aside on all GETs | Write path stays slow; invalidation across `Tournament→Division→Stage→Match→standings` is error-prone; one backend replica needs no shared cache. |
| `pg_dump \| psql` piped inside a compose one-shot at cutover | Works but couples data movement to the stack definition and risks running on every `up`. Operator-run restore is cleaner and auditable. |

## Rollback

1. In the server `.env`, restore `ConnectionStrings__DbConnection` to the Supabase
   pooler value (the project stays reachable until cutover is confirmed stable).
2. `docker compose up -d backend`.
3. Revert the `docker-compose.yml` / `StartupExtensions.cs` / `.env.example` commits
   when convenient — no schema migration to undo.
4. Leave `db` + the bind-mount data in place until cutover is confirmed stable, then
   remove and reclaim `/home/docker/club12/db`.

## Testing Strategy

- `API.Tests` run on SQLite in-memory (`WebApplicationFactory`) — no runtime DB
  dependency, unaffected.
- Strict TDD applies to the thin code change: `NpgsqlOptionsTests` asserts
  `CommandTimeout == 30` and `CreateExecutionStrategy().RetriesOnFailure == false` on
  both contexts.
- A `docker-compose.yml` / `.env.example` contract test. `API.Tests` has **no YAML
  parser** and adding `YamlDotNet` for this is not worth it — assert with plain-text /
  regex reads of the repo-root files: `db` service block present, no `ports:` under
  `db`, `depends_on` wires `db` with `condition: service_healthy`, `.env.example` has
  the three `POSTGRES_*` keys and a `Host=db` / `SSL Mode=Disable` connection string.
  There are no existing deployment contract tests to mirror; this is a new lightweight
  file (e.g. `DeploymentContractTests.cs`).
- Manual post-cutover verification (not CI-automatable): latency delta via
  `RequestLoggingMiddleware` `ElapsedMs`, image/PDF upload still works, admin login,
  one heavy endpoint (`GET /api/tournaments/{idOrSlug}/completability`), one weekly
  backup cycle + a test `pg_restore` into a scratch DB.

## Post-Merge Follow-Up (fix/selfhosted-postgres-followups)

The 2026-08-30 cutover surfaced three issues, fixed in a follow-up PR:

1. **`EnableRetryOnFailure` broke `DataMaintenanceService`.** The retrying execution
   strategy rejects `db.Database.BeginTransactionAsync(...)` unless the whole unit runs
   inside `CreateExecutionStrategy().ExecuteAsync(...)`. `UnitOfWork` does that;
   `DataMaintenanceService.WipeSampleDataAsync` / `SeedSampleDataAsync` do not, so
   `POST /api/data-maintenance/wipe` threw `InvalidOperationException`. SQLite in the
   test host masked it. **Fix:** drop `EnableRetryOnFailure`, keep `CommandTimeout(30)`
   (decision 6). A local DB has no transient faults for retry to absorb anyway.
2. **Frontend nginx 502s after every backend deploy.** `Club12-WebClient/nginx.conf`
   uses `proxy_pass http://backend:8080` (literal host, no `resolver`), so nginx caches
   the backend container IP at start-up. `deploy-backend.yml` recreates the backend
   with a new IP → every `/api/*` is an instant 502 until the frontend restarts.
   **Fix:** a `docker compose restart frontend` step in `deploy-backend.yml`, and the
   same step documented in the manual cutover runbook (`DEPLOYMENT.md` §7.4).
3. **`pg_dump` schema scope.** `ApplicationDBContext` tables live in schema `Club12`
   (not `public`); `pg_dump`'s `--schema` pattern is lower-cased, so `--schema=Club12`
   silently dumps nothing. The runbook now uses `--schema=public --schema='"Club12"'`.

## Post-Merge Follow-Up 2 (chore/retire-backup-cron)

This change originally shipped a weekly host cron (`scripts/backup-club12-db.sh` +
a `crontab` entry, Sundays 03:00) that ran `pg_dump` against the `db` container and
wrote to `/home/docker/backups/club12/`. Rationale at the time: it matched the
existing `backup-mu-db.sh` precedent, needed no `.env` toggles, and kept backup
cadence independent of the app process. The app's built-in `Backup:` feature
(interval-based `IHostedService`) was considered and explicitly dismissed as "not
chosen."

That decision is reverted here. The host cron turned out to be a second, disconnected
backup system: it never inserted a row into `BackupRecord`, so its dumps never showed
up in the admin panel, and restoring one required SSH-ing into the server and running
`pg_restore` by hand — no click-to-restore.

The backend already had a complete backup system doing the same job better:
`DatabaseBackupHostedService` runs `pg_dump` as a subprocess on its own schedule,
records every run in `BackupRecord` (visible in the panel, tagged "Programado" for
automatic runs / "Manual" for on-demand ones), and supports one-click restore from the
panel (`POST /api/backups/{id}/restore`, with an automatic safety backup taken first)
— no SSH needed. Retired:

- `scripts/backup-club12-db.sh` (deleted).
- The host `crontab` entry (removed by the operator).

Enabled instead, in the production `.env` (already done by the operator):

- `Backup__Enabled=true` — turns on the existing scheduled-backup hosted service.

Why: running two independent, disconnected backup mechanisms adds operational
confusion for no benefit — the app's own feature already satisfies the real goal
behind the original cron, which was being able to restore a backup without SSH
access to the server. See `DEPLOYMENT.md` §7.5 for the updated operator runbook.
