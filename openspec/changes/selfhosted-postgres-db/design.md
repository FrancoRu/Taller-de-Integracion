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
| 6 | Add `EnableRetryOnFailure()` + `CommandTimeout` to both `UseNpgsql` registrations, in this change | Separate change; leave defaults | ~4 lines, directly de-risks the cutover window (container restart, first-boot races). `UnitOfWork.ExecuteInTransactionAsync` already calls `CreateExecutionStrategy()`, so it is already compatible with a retrying strategy. |
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
   │ install the weekly backup crontab entry                      │
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

## Backup: Weekly `pg_dump` Cron (in this change)

Self-hosting makes DB backups this host's responsibility (previously Supabase's). This
change ships a script; the operator installs the crontab entry once.

- **Script** `scripts/backup-club12-db.sh` (committed): runs
  `docker compose -f /home/docker-compose/Club12/docker-compose.yml exec -T db pg_dump
  -U "$POSTGRES_USER" -Fc "$POSTGRES_DB"` → `/home/docker/backups/club12/club12-YYYYmmdd-HHMM.dump`,
  then prunes to the newest N (default 8 ≈ 2 months at weekly cadence). Reads
  `POSTGRES_*` from `/home/docker-compose/Club12/.env`. Exits non-zero on `pg_dump`
  failure so cron mail surfaces it.
- **Crontab** (operator, documented in `DEPLOYMENT.md`), every 7 days:
  `0 3 * * 0  /path/to/scripts/backup-club12-db.sh >> /var/log/club12-db-backup.log 2>&1`
  (Sundays 03:00 — adjust to taste; `*/7` on day-of-month is uneven, prefer a weekday.)
- `/home/docker/backups/club12/` is on the 420 GB `/home` partition. `-Fc` (custom
  format) restores with `pg_restore`.

Not chosen: the app's built-in `Backup:` feature (interval-based `IHostedService`).
It works, but a host cron matches the existing `backup-mu-db.sh` precedent, needs no
`.env` toggles, and keeps backup cadence independent of the app process. The app
feature stays available if the operator later prefers it.

Backups live on the same host/disk as the DB. An offsite copy (rsync to Nextcloud,
Cloudflare R2) is a follow-up, not blocking cutover — acceptable because this is not
a production system.

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
- Strict TDD applies to the thin code change: a test asserting the Npgsql options are
  configured (retry enabled, command timeout set) on both contexts' `DbContextOptions`.
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
