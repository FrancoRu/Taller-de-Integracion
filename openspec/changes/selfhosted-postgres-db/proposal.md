# Proposal: Self-Hosted Postgres Container (drop remote Supabase DB latency)

**Touches**: Backend only — `docker-compose.yml`, `.env.example`, backend `StartupExtensions` DI, backend `Dockerfile` (comment/parity only), `API.Tests` (compose/config contract). **No EF migration.** **No Application/Domain logic change.** **No frontend change.** Supabase Storage buckets are **not** touched.

## Intent

Every backend request takes 2–5 s. The cause is **network round-trip amplification**, not Postgres itself:

- The production server (`192.168.0.200`, on-prem Docker host) connects to **Supabase-hosted Postgres in AWS `us-east-2` (Ohio)** through the Supavisor pooler. Each EF Core query is one round trip over the public internet — RTT ≈ 120–170 ms.
- Application services issue **many sequential queries per request**. Examples in the current code:
  - list endpoints → `FindAsync` + `CountAsync` = 2 round trips (`GenericRepository`);
  - `TournamentService.EvaluateCompletabilityAsync` → 4+ sequential queries;
  - `CreateFullTournamentAsync` → a loop that inserts divisions/stages one by one, each `AddAsync` doing its own `SaveChangesAsync`.
- 15–30 sequential round trips × ~150 ms ≈ **2–5 s**, matching the observed latency.

Moving Postgres to a **container on the same Docker host as the backend** replaces each ~150 ms internet round trip with a local bridge-network hop (sub-millisecond). The same 20-round-trip request drops from ~3 s to well under 100 ms. This is the single highest-impact change and needs ~one line of connection-string change plus compose/ops wiring — no application rewrite.

## Server Environment (confirmed from the host README, Aug 2026)

| Fact | Value | Consequence for this change |
|------|-------|-----------------------------|
| Host | Debian 13, Intel i3-4130T (2c/4t @ 2.9 GHz), **5.7 GB RAM**, **500 GB HDD (spinning)** | Postgres must be memory-capped and I/O-conservative; it competes with 7 running containers + host services |
| Disk layout | `/` = 31 GB (`franco-vg-root`, OS + Docker images/volumes); `/home` = 420 GB (`franco-vg-home`, container data) | Docker's default volume path is on the **31 GB** partition — the DB volume must land on `/home` (bind mount `/home/docker/club12/db`, matching the host's documented convention) |
| Supabase Postgres version | **PostgreSQL 17.6** (aarch64) — confirmed via `SELECT version();` | `db` image = `postgres:17-alpine`; the backend `Dockerfile`'s `postgresql-client-17` already matches — **no Dockerfile change**. Logical `pg_dump`/`pg_restore` is architecture-independent, so aarch64 → x86-64 server is fine. |
| Existing Postgres on host | `openmu-db` and `nextcloud-db` both run `postgres:15-alpine` | Precedent for the `-alpine` variant; Club12 pins `17-alpine` to match Supabase. |
| Compose project | `/home/docker-compose/Club12/docker-compose.yml`, synced by CI on every deploy; `.env` beside it, `gh-runner:gh-runner`, mode 600, **never touched by CI**, **not backed up** | New `POSTGRES_*` + rewritten connection string are hand-added to the server `.env` once; DB backups are now this host's responsibility |
| Deploy job | runs as `gh-runner` (docker group only), `docker compose up -d --no-deps --no-build backend` | `--no-deps` confirmed — CI will not start `db`; `db` is brought up once at cutover and kept alive by `restart: unless-stopped` |
| Network | `club12_club12` bridge; `club12-backend-1` not published on host; frontend on host `:5001` behind NPM + Cloudflare Tunnel | `db` joins `club12_club12`, also unpublished; no firewall/NPM/tunnel change |
| Current backup precedent | `/home/docker/backup-mu-db.sh`, cron 03:00, `/home/docker/backups/mu/`, keep last 7 | Club12 DB backup can mirror this pattern or use the app's built-in `Backup:` feature writing to the `backup-data` volume |

## Scope

### In Scope — backend

1. **New `db` service in `docker-compose.yml`:**
   - Image **`postgres:17-alpine`** — matches Supabase (PG 17.6) and the backend `Dockerfile`'s `postgresql-client-17`. No Dockerfile change.
   - **Data on `/home`, not the 31 GB root partition:** bind mount `/home/docker/club12/db:/var/lib/postgresql/data`, per the server's documented "persistent data under `/home/docker/<project>/`" rule. (Confirmed by the user.)
   - Attached to the existing `club12` bridge network **only** — no host port published (`5432` is never exposed to the host or LAN; UFW and NPM stay untouched).
   - `pg_isready` healthcheck.
   - Explicit `deploy.resources.limits.memory` (~512 MB target, given 5.7 GB shared with 7 containers) and `restart: unless-stopped`.
   - Conservative Postgres tuning for a small HDD host: `shared_buffers ≈ 128 MB`, `effective_cache_size ≈ 512 MB`, default `random_page_cost` (HDD). Set via `command:` flags or a mounted `postgresql.conf` — `design.md` picks one.
   - Credentials from `.env`: `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` (consumed by the postgres image).
2. **`backend` service** gains `depends_on: { db: { condition: service_healthy } }`.
3. **Connection string** (`ConnectionStrings__DbConnection`, operator-set in the server `.env`) moves from the Supabase pooler shape (`Host=aws-1-...pooler.supabase.com;SSL Mode=Require;Trust Server Certificate=true`) to the in-cluster shape (`Host=db;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>;SSL Mode=Disable`). `SSL Mode=Disable` is acceptable **only** because traffic never leaves the Docker bridge network (see the no-host-port requirement).
4. **`.env.example`** updated: add `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` placeholders; rewrite the `ConnectionStrings__DbConnection` placeholder to the `Host=db;…;SSL Mode=Disable` shape; keep every value a placeholder.
5. **Npgsql resilience hardening** (small, belongs with this change because cutover briefly stresses connections): add `EnableRetryOnFailure()` and an explicit `CommandTimeout` to `UseNpgsql(...)` for **both** `ApplicationDBContext` and `IdentityAppDbContext` in `StartupExtensions`. `UnitOfWork.ExecuteInTransactionAsync` already uses `CreateExecutionStrategy()`, so it is already retry-compatible.
6. **Deploy workflow (`deploy-backend.yml`)**: keep `docker compose up -d --no-deps --no-build backend` as-is. `db` is brought up once at cutover and stays up via `restart: unless-stopped` (the Docker daemon is systemd-enabled, so it survives host reboots). No workflow change needed for the happy path; `design.md` keeps an optional `docker compose up -d db` step as a fast follow if reboots prove flaky.
7. **Backup cron** (new, committed to the repo): a script + documented crontab entry that runs `pg_dump` against the `db` container **every 7 days**, writes to `/home/docker/backups/club12/`, and prunes to a retention count. Mirrors the existing `/home/docker/backup-mu-db.sh` pattern. The crontab install itself is an operator step (documented in `DEPLOYMENT.md`).
8. **Tests:** `container-deployment` capability delta (below). `API.Tests` run on SQLite in-memory via `WebApplicationFactory` and are unaffected by the runtime DB change. New tests: Npgsql-options unit test (retry + timeout on both contexts) and a plain-text `docker-compose.yml` / `.env.example` contract assertion (no new package — string/regex, not a YAML parser).

### In Scope — operations (documented in `design.md` / `DEPLOYMENT.md`, not code)

- The **cutover runbook**: bring up `db` → restore the operator's existing backup copy into it → **then** start `backend` (so its startup `MigrateAsync` + seed does not race the restore). The operator already holds a backup copy on the server; the restore itself is a manual step at cutover time.
- **Installing the backup crontab** entry on the host (the script ships in the repo; the cron registration is a one-time operator action).

### Out of Scope (Non-Goals)

- **Supabase Storage migration.** The `public-images`, `medical-records`, and backup buckets stay on Supabase. `SupabaseHelper` and every storage call site are untouched. (User decision.)
- **Supabase Auth.** Not used — the app has its own ASP.NET Core Identity + JWT. Nothing to migrate.
- **Data-migration tooling.** No object/row migration script. The operator restores a pre-made backup at cutover.
- **`AsNoTracking` sweep, batched `SaveChanges`, DTO projection.** Real wins, but independent of where the DB lives — deferred to a separate performance follow-up change.
- **Redis / any caching layer.** Explicitly rejected in the performance analysis (workaround, hard invalidation in this relational domain, single backend replica).
- **PgBouncer / an external pooler.** Npgsql client-side pooling against a local DB is sufficient for one backend replica.
- **HA Postgres, managed failover, multi-replica backend, TLS between `backend` and `db`.**
- **Managed-Postgres-in-a-nearer-region** alternative (e.g. `sa-east-1`). Considered in `design.md` as a rejected alternative; the user chose self-hosting.

## Capabilities

### Modified Capabilities

- **`container-deployment`** — the compose topology gains a first-party `db` (Postgres) service; "a bundled Postgres container" moves out of the spec's Out-of-Scope list. See `specs/container-deployment/spec.md` for the delta.

### New Capabilities

- None. The change is a deployment-topology and configuration change; it introduces no new application behavior worth its own capability.

### Note on an existing contradiction

`container-deployment` still states the backend image "MUST NOT have `postgresql-client`/`pg_dump` installed", but the backend `Dockerfile` already installs `postgresql-client-17`. That contradiction predates this change and is **not created or resolved here**; this delta only touches the Postgres-service and bundled-container requirements. It should be reconciled by whichever change owns the backup feature's spec.

## Approach

Add one `db` service to the existing single-host compose stack, point the backend at it over the internal network, and keep everything else — images, Identity, Storage, tests — exactly as is. The connection string is the only application-visible change; two EF options (`EnableRetryOnFailure`, `CommandTimeout`) are added opportunistically because they harden the cutover and cost ~4 lines.

The data itself is carried over by an operator-run restore of an existing backup, sequenced before the first `backend` start so EF's startup `MigrateAsync` sees an already-populated, already-migrated schema (a no-op, or applies only newer migrations).

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `docker-compose.yml` | Modified | New `db` service (`postgres:17-alpine`, `/home/docker/club12/db` bind mount, internal `club12` network, `pg_isready` healthcheck, 512 MB mem limit, tuning flags); `backend.depends_on.db: service_healthy` |
| `.env.example` | Modified | Add `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB`; rewrite `ConnectionStrings__DbConnection` placeholder to `Host=db;…;SSL Mode=Disable` |
| `Club12-Backend/API/Utils/StartupExtensions.cs` (`AddDbContextConfig`, `AddIdentityConfig`) | Modified | `UseNpgsql(cs, o => o.EnableRetryOnFailure().CommandTimeout(30))` for both contexts |
| `scripts/backup-club12-db.sh` (new) + crontab doc | Added | Weekly `pg_dump` of the `db` container → `/home/docker/backups/club12/`, retention prune. Mirrors `/home/docker/backup-mu-db.sh` |
| `Club12-Backend/API.Tests` | Added tests | Npgsql-options unit test; plain-text `docker-compose.yml` / `.env.example` contract assertion (no new package). Functional tests unaffected (SQLite in-memory) |
| `DEPLOYMENT.md` | Modified | Cutover runbook, `.env` key checklist, crontab install step |
| `.github/workflows/deploy-backend.yml` | Unchanged | `--no-deps` kept; `db` stays up via `restart: unless-stopped` |
| `Club12-Backend/Dockerfile` | Unchanged | `postgresql-client-17` already matches `postgres:17-alpine` |
| Supabase project | Unchanged | Kept reachable as a best-effort rollback target; Storage buckets keep serving |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Backend startup `MigrateAsync` + seed runs against an empty `db` before the operator restores the backup → PK/constraint conflicts on restore, or a seeded-then-restored mess | High | Cutover runbook (`design.md`): `db` up → restore → `backend` up. Prod default is already `Seed:Enabled=false`. |
| **5.7 GB RAM** shared across 2 Postgres instances + OpenMU + Nextcloud + backend + 2 frontends + host services; a third Postgres can trigger swapping on the HDD | Med–High | `db` mem limit 512 MB, `shared_buffers` 128 MB; check `free -m` headroom before cutover; 5.9 GB swap absorbs spikes but HDD swap is slow — monitor after cutover |
| **31 GB root partition** fills if the DB data lands in Docker's default volume dir | Med | Bind mount to `/home/docker/club12/db` on the 420 GB partition (user-confirmed) |
| Data durability now on the operator — no managed PITR, and `.env` itself was never backed up | Med | Weekly `pg_dump` cron shipped in this change (retention prune); `docker compose down` safety documented (never `-v`); not production, so RPO of ~7 days is acceptable. Offsite copy is a follow-up. |
| Single point of failure on a home server (power, disk, network) | Med | Accepted — not a production app (user). Weekly backup + a config-flip rollback to Supabase while the project stays reachable. |
| aarch64 (Supabase) → x86-64 (server) restore | Low | `pg_dump`/`pg_restore` are logical/architecture-independent; both sides are PG 17. Non-issue, noted for the record. |
| `SSL Mode=Disable` regresses security if `db` is ever exposed | Low | Hard requirement: `db` publishes no host port, `club12` network only. |
| `Supabase` .NET client (`SupabaseHelper`) keeps `AutoConnectRealtime = true` — a websocket to Supabase | Low | Unrelated to the DB move (Storage stays on Supabase). Flagged for a later cleanup. |

## Rollback Plan

- **Code/config:** revert the compose + `StartupExtensions` + `.env.example` commits. In the server `.env`, restore `ConnectionStrings__DbConnection` to the Supabase pooler value. `docker compose up -d backend`. No schema change to undo.
- **Data:** Supabase project stays reachable (no formal grace window — not production; keep it until the cutover is visibly stable). Rollback is a config flip, not a data recovery.
- **Containers:** leave `db` + `/home/docker/club12/db` in place until cutover is confirmed stable, then remove and reclaim the directory.

## Dependencies

- The operator's **existing backup copy** of the Supabase database, present on the server, restorable into `postgres:17`.
- **Supabase = PostgreSQL 17.6** (confirmed) → `db` image `postgres:17-alpine`, no `Dockerfile` change.
- One-time hand-edit of the server `.env` at `/home/docker-compose/Club12/.env`: add `POSTGRES_*`, rewrite `ConnectionStrings__DbConnection`.
- One-time crontab registration on the host for the backup script.
- The in-flight `database-backup-restore` work does **not** touch `docker-compose.yml` / `Dockerfile` / `.env.example` (user-confirmed) → no merge-conflict coordination needed. This change branches from `develop` and opens its own PR to `develop`.
- One-time hand-edit of the server `.env` at `/home/docker-compose/Club12/.env` (CI never touches it): add `POSTGRES_*`, rewrite `ConnectionStrings__DbConnection`.
- Integration branch is `develop`. Strict TDD is active (the DI/config change is thin; contract tests carry it).
- Server facts confirmed 2026-08-30 from the host README (RAM, disk layout, existing containers, deploy flow) — see the Server Environment table.

## Success Criteria

- [ ] `docker compose up` on a clean host brings up `db`, then `backend` (healthy only after `db` is healthy), then `frontend` — with **no external Supabase account required for the database**.
- [ ] `GET /health/ready` returns 200 against the containerized DB.
- [ ] `docker compose ps` shows **no** `0.0.0.0:5432` / published port for `db`.
- [ ] p95 `ElapsedMs` (from `RequestLoggingMiddleware`) for a representative multi-query endpoint drops from seconds to < 200 ms after cutover.
- [ ] Team logos, blog images, and medical-record PDF upload/download still work (Supabase Storage untouched).
- [ ] `dotnet test Club12-Backend/Solution/Club12.sln` and `npm run test --prefix Club12-WebClient` pass.
- [ ] `db` survives a backend-only redeploy (`--no-deps`) and a host reboot via `restart: unless-stopped`.
- [ ] The weekly `pg_dump` cron produces a dump and a test restore of it into a scratch DB succeeds.
- [ ] Rollback verified: flipping `ConnectionStrings__DbConnection` back to Supabase and restarting `backend` restores service.

## Proposal Question Round — Resolved (2026-08-30)

1. **Supabase Postgres version** → PostgreSQL **17.6**. `db` = `postgres:17-alpine`; `Dockerfile` unchanged (`postgresql-client-17` matches).
2. **Ordering vs `database-backup-restore`** → that work does not touch `docker-compose.yml` / `Dockerfile` / `.env.example`. This change creates its **own branch off `develop`** and opens a PR **to `develop`**. No coordination needed.
3. **Rollback window** → not a production app; no formal window. Keep the Supabase project reachable until the cutover is visibly stable, then decommission.
4. **DB data placement** → bind mount `/home/docker/club12/db` (confirmed).
5. **Backup mechanism** → a **cron every 7 days** running `pg_dump` automatically. Script ships in the repo; crontab install is an operator step. (Not the app `Backup:` feature.)
