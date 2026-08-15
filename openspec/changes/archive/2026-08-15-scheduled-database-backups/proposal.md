# Proposal: Scheduled Database Backups

Touches: **backend only** (Club12-Backend).

## Intent

"Creación de copias de seguridad" is an explicit documented **functional requirement** and **NFR** (regular DB backups) in both academic informes, yet zero backup/scheduler code exists in the backend (no `IHostedService`, `BackgroundService`, cron, or `pg_dump`). This is the one genuine functional gap. We add a proportionate, self-contained scheduled backup capability so the contracted function is actually delivered and demonstrable in code — not deferred to a managed provider.

## Scope

### In Scope
- A scheduled `BackgroundService` (`IHostedService`) that runs on a configurable interval via `PeriodicTimer`.
- Logical dump of the PostgreSQL database (`pg_dump` against the existing `DbConnection`).
- Off-host storage of dumps by **reusing the Supabase client/credentials already in the codebase** (`SupabaseHelper` pattern, `SupaBase` config section) into a dedicated `backups` path/bucket.
- Retention policy: keep last N dumps, prune oldest (N configurable, default 7).
- New `Backup` config section (Enabled, IntervalHours, RetentionCount, StorageTarget, PgDumpPath); DI registration in `StartupExtensions`.
- Unit tests for retention-pruning and schedule logic; a hosted-service smoke test with mocked dump/storage.

### Out of Scope
- Restore/recovery (no UI, no automated restore, no PITR).
- Multi-region replication, encryption beyond Supabase defaults, backup checksum/restore-verification.
- On-demand backup API endpoint (possible later slice).
- Enterprise backup tooling (Hangfire/Quartz — not currently referenced; `PeriodicTimer` suffices).

## Capabilities

### New Capabilities
- `scheduled-database-backups`: interval-triggered DB dump, durable storage, and keep-last-N retention.

### Modified Capabilities
- None.

## Approach

`DatabaseBackupHostedService` (a `BackgroundService`) orchestrates three injected abstractions so the schedulable/prunable logic stays unit-testable and the environment-bound parts stay mockable:
- `IDatabaseBackupService` → `PgDumpBackupService` (runs `pg_dump` via `Process`, streams output).
- `IBackupStorage` → `SupabaseBackupStorage` (reuses existing Supabase client) with optional `LocalDirectoryBackupStorage`.
- `IBackupRetentionPolicy` → keep-last-N pruning over the storage listing.

Secrets are never hardcoded: DB target reuses `ConnectionStrings:DbConnection`; storage reuses the existing `SupaBase` section — same `IConfiguration` pattern as email/JWT/Supabase today.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Application/Interfaces/Services` | New | Backup/storage/retention interfaces |
| `Application/Services` (or `Infrastructure`) | New | pg_dump + Supabase-storage + retention impls |
| `API/BackgroundServices` | New | `DatabaseBackupHostedService` |
| `API/Utils/StartupExtensions.cs`, `Program.cs` | Modified | DI + `AddHostedService` registration |
| `API/appsettings.json` | Modified | Non-secret `Backup` defaults |
| `API.Tests` | New | Retention/schedule unit tests + smoke test |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| DB is Supabase-managed → app backup partly redundant | Med | Requirement is contracted; free-tier Supabase lacks auto-backups; keep feature lightweight and toggleable |
| `pg_dump` binary absent on host/container | High | Configurable `PgDumpPath`; document as deploy dependency; fail-soft with logged error, no crash |
| pg_dump path untestable in CI (tests use SQLite) | High | Unit-test retention/schedule only; actual dump verified in staging/manual |
| Long dump blocks/overlaps runs | Low | Single-flight guard; skip if previous run active |

## Rollback Plan

Feature is opt-in via `Backup:Enabled=false` (default off until validated). Full revert = remove the `AddHostedService` line + DI registrations; no schema/migration changes, so nothing to unwind in the database.

## Dependencies

- `pg_dump` (postgresql-client) available on the deployment host/image.
- Existing `SupaBase` storage credentials (already configured) — or a writable local/mounted directory.

## Success Criteria

- [ ] With `Backup:Enabled=true`, a dump is produced on the configured interval and stored durably.
- [ ] Only the last N dumps are retained; older ones pruned.
- [ ] No secrets committed; all config via `IConfiguration`/env.
- [ ] Retention and schedule logic covered by passing unit tests.
- [ ] Actual `pg_dump` + upload verified once in staging (manual sign-off documented).

## Proposal question round

Interactive asking was unavailable; these open decisions carry my working assumptions — flag any to correct before spec/design:
1. **Is the production Postgres actually Supabase-managed?** If yes and on a paid tier, daily managed backups already exist and this feature is a *demonstrable-requirement* implementation rather than the primary safety net. Assumption: implement anyway (contracted function; likely free tier).
2. **Storage target:** reuse the existing Supabase bucket (my default, zero new infra) vs. a local/mounted directory. Assumption: Supabase, with local as a config-switchable fallback.
3. **Cadence & retention:** assumption daily interval, keep last 7. Adjust if graders expect a specific schedule.
4. **Default enabled?** Assumption: shipped disabled (`Enabled=false`) until a staging run validates `pg_dump` availability, then enabled per-environment.
