# Tasks: Self-Hosted Postgres Container

## Implementation status (2026-08-30)

Implemented in worktree branch `feat/selfhosted-postgres-db` (off `origin/develop`), **not committed** — awaiting user go-ahead.

- ✅ Phase 0–6 (code + config + script + docs). Strict TDD: RED shown (8 failing), then GREEN.
- ✅ `dotnet test Club12-Backend/Solution/Club12.sln` → **737 passed, 0 failed**.
- ✅ `dotnet build -c Release` → 0 warnings. `docker compose config` → valid, `db` private, backend waits on `db` health.
- ⏳ Phase 7 = manual operator verification, runs at cutover (see `DEPLOYMENT.md` §7).
- Files: `docker-compose.yml`, `.env.example`, `Club12-Backend/API/Utils/StartupExtensions.cs`, `scripts/backup-club12-db.sh` (new, +x), `Club12-Backend/API.Tests/DeploymentContractTests.cs` (new), `Club12-Backend/API.Tests/NpgsqlOptionsTests.cs` (new), `DEPLOYMENT.md`. No `Dockerfile` change (client-17 already matches).

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~230 authored (`docker-compose.yml` ~30, `.env.example` ~8, `StartupExtensions.cs` ~12, `scripts/backup-club12-db.sh` ~40, tests ~110, `DEPLOYMENT.md` ~45) |
| 400-line budget risk | Low |
| 1500-line budget (session) | Fits comfortably in one PR |
| Chained PRs recommended | No |
| Delivery strategy | ask-on-risk → single PR (no risk flagged) |
| Chain strategy | n/a |

### Suggested Work Units

| Unit | Goal | Focused test command | Runtime harness | Rollback boundary |
|------|------|----------------------|-----------------|-------------------|
| 1 | Compose `db` service + `.env.example` + Npgsql options + contract/unit tests | `dotnet test Club12-Backend/Solution/Club12.sln --filter "FullyQualifiedName~Deployment\|FullyQualifiedName~Npgsql"` | `docker compose config`; `docker compose up` on a scratch host | Revert `docker-compose.yml` + `StartupExtensions.cs`; `.env` flip back to Supabase |
| 2 | Backup cron script + `DEPLOYMENT.md` runbook | shellcheck `scripts/backup-club12-db.sh` | Manual runbook + one dump/restore dry-run | Revert docs + script commit; remove crontab line |

## Pre-Apply Checklist — all resolved (2026-08-30)

- [x] 0.1 **Supabase = PostgreSQL 17.6** → `db` image `postgres:17-alpine`; `Dockerfile` unchanged (`postgresql-client-17` already matches); cutover `pg_dump` = v17.
- [x] 0.2 **DB data** → bind mount `/home/docker/club12/db:/var/lib/postgresql/data`.
- [x] 0.3 **No merge coordination** — the in-flight `database-backup-restore` work does not touch `docker-compose.yml` / `Dockerfile` / `.env.example`.
- [x] 0.4 **Rollback** — not production; keep Supabase reachable until stable, no formal window.
- [x] 0.5 **Backup** → weekly `pg_dump` cron (script committed, crontab installed by operator). Not the app `Backup:` feature.
- [x] 0.6 **Delivery** — single PR. This change **branches from `develop`** and opens its PR **to `develop`** (do NOT build on `feat/medical-records-storage-eligibility`).

---

## Phase 0: Branch

- [ ] 0.1 From up-to-date `develop`, create `feat/selfhosted-postgres-db` (or similar). All subsequent commits land there.

## Phase 1: Compose — `db` service (RED → GREEN)

- [ ] 1.1 RED — `API.Tests/DeploymentContractTests.cs` (plain-text reads of the repo-root files, no YAML parser): assert `docker-compose.yml` defines a `db:` service using `postgres:17-alpine`.
- [ ] 1.2 RED — assert the `db` service block has **no `ports:`** entry (only `expose`).
- [ ] 1.3 RED — assert `backend` declares `depends_on:` on `db` with `condition: service_healthy`.
- [ ] 1.4 RED — assert `db` declares a `healthcheck` and a `deploy.resources.limits.memory`.
- [ ] 1.5 GREEN — add the `db` service to `docker-compose.yml` per `design.md`: `image: postgres:17-alpine`, `env_file: .env`, `command` with `shared_buffers=128MB` / `effective_cache_size=512MB`, `volumes: [/home/docker/club12/db:/var/lib/postgresql/data]`, `networks: [club12]`, `expose: ["5432"]`, `restart: unless-stopped`, `deploy.resources.limits.memory: 512m`, `pg_isready` healthcheck. Add `backend.depends_on.db`.
- [ ] 1.6 GREEN — `docker compose config` parses clean; `docker compose config` output shows no published `5432`. Tests 1.1–1.4 green.

## Phase 2: Environment contract (RED → GREEN)

- [ ] 2.1 RED — `DeploymentContractTests.cs`: `.env.example` contains `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` and a `ConnectionStrings__DbConnection` with `Host=db` and `SSL Mode=Disable` (or `Prefer`), all placeholder values.
- [ ] 2.2 RED — assert `.gitignore` still excludes `.env`.
- [ ] 2.3 GREEN — update `.env.example`: add the three `POSTGRES_*` keys (placeholders); rewrite the `ConnectionStrings__DbConnection` placeholder to `Host=db;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>;SSL Mode=Disable`. Tests green.

## Phase 3: Npgsql resilience options (RED → GREEN)

- [ ] 3.1 RED — `API.Tests/NpgsqlOptionsTests.cs`: build the service provider from `AddDbContextConfig` (and `AddIdentityConfig`) with a Postgres connection string; resolve `DbContextOptions<ApplicationDBContext>` / `DbContextOptions<IdentityAppDbContext>`; assert the `NpgsqlOptionsExtension` has retry enabled (`IsRetryingExecutionStrategy` / non-null `ExecutionStrategyFactory`) and `CommandTimeout == 30`.
- [ ] 3.2 GREEN — in `StartupExtensions.AddDbContextConfig` and `AddIdentityConfig`, pass `npgsql => { npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null); npgsql.CommandTimeout(30); }` to both `UseNpgsql` calls.
- [ ] 3.3 GREEN — `UnitOfWork.ExecuteInTransactionAsync` unchanged (its `CreateExecutionStrategy()` is already retry-safe); confirm it still compiles.
- [ ] 3.4 Full suite green: `dotnet test Club12-Backend/Solution/Club12.sln`.

## Phase 4: Delta spec sync

- [ ] 4.1 Reconcile `openspec/changes/selfhosted-postgres-db/specs/container-deployment/spec.md` with the shipped `docker-compose.yml` / `.env.example` wording if apply revealed a different shape. No `Dockerfile` change (17 == 17).

## Phase 5: Backup cron script

- [ ] 5.1 Add `scripts/backup-club12-db.sh` per `design.md`: `set -euo pipefail`, read `POSTGRES_*` from `/home/docker-compose/Club12/.env`, `docker compose ... exec -T db pg_dump -U "$POSTGRES_USER" -Fc "$POSTGRES_DB"` → `/home/docker/backups/club12/club12-$(date +%Y%m%d-%H%M).dump`, prune to newest 8, non-zero exit on failure.
- [ ] 5.2 `shellcheck` clean; dry-run against a local `db` container produces a `.dump` and `pg_restore --list` reads it.

## Phase 6: Operator documentation

- [ ] 6.1 `DEPLOYMENT.md` — new section "Migración a Postgres self-hosted": the one-time `.env` edit (`POSTGRES_*` + new `ConnectionStrings__DbConnection`), the cutover runbook (`db` up → restore backup → `backend` up), the "keep Supabase reachable until stable" note.
- [ ] 6.2 `DEPLOYMENT.md` — the crontab install step for `scripts/backup-club12-db.sh` (weekly, e.g. `0 3 * * 0`), the backup dir on `/home`, and a note to test-restore periodically.
- [ ] 6.3 `DEPLOYMENT.md` — `docker compose down` safety: the DB data is a bind mount so it survives `down` and even `down -v`; still warn that `-v` drops named volumes (`backup-data`) and that removing `/home/docker/club12/db` by hand destroys the database.

## Phase 7: Manual post-cutover verification (operator, not CI)

- [ ] 7.1 `docker compose ps` — `db` healthy, no `0.0.0.0:5432`.
- [ ] 7.2 `GET /health/ready` → 200.
- [ ] 7.3 Latency: compare `RequestLoggingMiddleware` `ElapsedMs` for `GET /api/tournaments` and a completability endpoint before/after — expect seconds → <200 ms.
- [ ] 7.4 Storage still works: upload a team logo and a medical-record PDF, download both.
- [ ] 7.5 Admin login + a write (create a blog post) + read it back.
- [ ] 7.6 `free -m` — no sustained swap growth after 24 h.
- [ ] 7.7 Run `scripts/backup-club12-db.sh` once by hand; `pg_restore` the output into a scratch DB (`docker run --rm postgres:17-alpine` + `pg_restore`) successfully.

## Out of Scope (tracked for follow-ups)

- `AsNoTracking` on `GenericRepository` reads; batched `SaveChanges` in seed/create-full loops; DTO projection instead of `Include`-then-map — **separate performance change**, re-measure after cutover.
- Offsite backup copy (rsync to Nextcloud / R2).
- Disabling `AutoConnectRealtime` in `SupabaseHelper` (unused websocket) — small cleanup.
- CI step `docker compose up -d db` (self-healing) — add only if reboots prove flaky.
- Managed-Postgres-in-`sa-east-1` fallback if the home host proves fragile.
