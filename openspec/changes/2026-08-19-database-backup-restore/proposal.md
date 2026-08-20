# Proposal: Database Backup & Restore (Administración de datos)

**Touches: both** (backend `Club12-Backend`, frontend `Club12-WebClient`, plus repo-root deployment).

## Intent

Backups are a contracted functional requirement, but the capability shipped by
`2026-08-15-scheduled-database-backups` is switched off in every environment this repo describes:
`Backup:Enabled=false` in `appsettings.json` and `appsettings.Development.json`,
`StorageTarget=Local` with **no volume mounted** for the `backend` service in `docker-compose.yml`,
no `postgresql-client` in the backend image, and no HTTP or UI surface at all. Restore does not
exist anywhere in the codebase. In practice **no backup is ever taken, none is visible, and none
can be restored** — the archived proposal deferred both restore and an on-demand endpoint as
"possible later slice". This change delivers that slice: backups taken, catalogued, listed,
deleted, restored, and persisted on the team's own server.

## Scope — 4 ordered phases

### In Scope

1. **Backup generation.** Enable the scheduled job for real, *and* add an Admin-only on-demand
   "Generar respaldo" trigger — scheduled and manual coexist, not either/or. Every backup, from
   either source, writes a durable queryable record in a **new database entity**: `Fecha`
   (created at), `Peso` (file size), `Forma de creación` (enum `Manual | Job`), and the storage
   `path`. This catalog — not `IBackupStorage.ListAsync()`, which returns only name + provider
   timestamp — is the source of truth for the phase-3 table.
2. **Restore.** Admin-only restore of a catalogued backup **directly over the live database behind
   a maintenance-mode window** (cut traffic / app briefly offline → `pg_restore` or equivalent
   against the live DB → app back). Destructive; requires explicit confirmation. The
   parallel-DB-plus-swap alternative was considered and **explicitly rejected** as unnecessary
   complexity at this project's scale.
3. **View.** Rename the existing Admin "Test" tab/panel to **"Administración de datos"**, holding
   two cards:
   - **"Base de datos"** (primary): buttons **"Borrar los datos"** (the existing wipe action,
     relabeled) and **"Generar respaldo"** (phase 1). Same card holds a table following this
     project's existing table pattern, columns exactly **"Fecha", "Peso", "Forma de creación",
     "Actions"**, sourced from the phase-1 entity. "Actions" carries two icons matching existing
     icon usage: **trash** (delete backup = `IBackupStorage.DeleteAsync` + remove its record) and
     **restore** (phase-2 flow). **Both delete and restore MUST show a confirmation modal**,
     reusing the established `confirmDialog.ts` / SweetAlert2 `confirmDelete` / `confirmAction`
     pattern.
   - **"Test"** (secondary, bottom): the **"Cargar Datos de prueba"** seed button relocated here,
     behavior unchanged.
4. **Server storage.** Persist backups on the app's **own self-hosted Debian server** — a proper
   Docker named volume / bind mount for the `backend` service pointed at a persistent host path —
   replacing today's unmounted `Local` target (which would write into the container's ephemeral
   filesystem and lose every backup on redeploy). **Not Supabase Storage**: the live database is
   already Supabase-hosted, so backing up into the same account/plan would couple the backup's
   durability to the single provider it is meant to protect against, and would assume the client
   keeps using Supabase long-term. `SupabaseBackupStorage` remains in the codebase as a legitimate
   config-selectable `IBackupStorage` adapter — just not the default/production target.

### Out of Scope

- Supabase Storage as the production backup target (see rationale above — decided, not open).
- PITR, continuous archiving, incremental/differential backups, off-site replication, encryption
  beyond host filesystem permissions.
- Scheduled/automatic restores, partial or table-level restore, restoring an uploaded foreign dump.
- Automated end-to-end restore verification in CI (the xUnit harness runs SQLite; real
  `pg_dump`/`pg_restore` remain manual staging sign-off, per the existing spec's Non-Goals).
- Any behavioral change to wipe/seed beyond relabelling and relocating them.
- Backup download/export to the operator's machine.

## Capabilities

### New Capabilities

- `backup-catalog`: durable per-backup record (created-at, size, creation method `Manual|Job`,
  storage path) written by both the scheduled job and the manual trigger; queried by the admin UI;
  deleted together with its stored file.
- `database-restore`: Admin-only restore of a catalogued backup over the live database behind an
  explicit maintenance-mode window, with confirmation, failure isolation, and audit logging.
- `admin-data-administration-panel`: the "Administración de datos" Admin view — two cards, the
  backups table with its two row actions, and mandatory confirmation modals for both destructive
  actions.

### Modified Capabilities

- `scheduled-database-backups`: the scheduled path MUST now (a) actually run in deployed
  environments, (b) record a catalog entry per successful backup, (c) coexist with an on-demand
  trigger without overlapping runs, and (d) default to server-volume storage.
- `container-deployment`: backend image MUST ship `postgresql-client` (`pg_dump` **and**
  `pg_restore`/`psql`); `docker-compose.yml` MUST mount a persistent, writable-by-`$APP_UID`
  volume at the configured `Backup:LocalStoragePath`.

## Approach

Extend the existing Clean-Architecture seam rather than rebuilding it. `IDatabaseBackupService` and
`IBackupStorage` stay; the new work is (1) a `BackupRecord` domain entity + EF migration +
repository, written by a single application-layer "create backup" use case that both
`DatabaseBackupHostedService` and the new Admin endpoint call — so scheduled and manual runs share
one code path, one catalog write, and one single-flight guard; (2) a `RestoreAsync` counterpart on
a restore abstraction implemented in `Infrastructure` via `pg_restore`, invoked by an Admin-only
endpoint that flips a maintenance gate before and after; (3) a `BackupController`
(`[Authorize(Roles = Roles.Admin)]`) mirroring the existing `DataMaintenanceController`; (4)
frontend module + view following the feature-module layout, reusing `TableRowActions` and
`confirmDialog.ts`. Storage-target switch is configuration + compose only — no adapter rewrite.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Club12-Backend/Domain` | New | `BackupRecord` entity + `BackupCreationMethod` enum (`Manual`/`Job`) |
| `Club12-Backend/Infrastructure/Data` + migration | New | `DbSet`, EF configuration, migration for the catalog table |
| `Club12-Backend/Application/Backup` | New/Modified | Create-backup use case (shared by job + endpoint), restore use case, `IBackupRestoreService`, catalog repository interface |
| `Club12-Backend/Infrastructure/Backup` | New/Modified | `PgRestoreService`; `PgDumpBackupService` reports size/path for the catalog |
| `Club12-Backend/API/Controllers` | New | `BackupController` — list / create / delete / restore, Admin-only |
| `Club12-Backend/API/BackgroundServices/DatabaseBackupHostedService.cs` | Modified | Delegates to the shared use case so job runs are catalogued |
| `Club12-Backend/API/appsettings*.json` | Modified | `Backup:Enabled=true`, persisted `LocalStoragePath`, restore settings |
| `Club12-Backend/Dockerfile` | Modified | Install `postgresql-client` (`pg_dump` + `pg_restore`) |
| `docker-compose.yml`, `.env.example`, `DEPLOYMENT.md` | Modified | Named volume for backups + documented host path and rationale |
| `Club12-WebClient/src/views/panel/TestDataPage.tsx` | Modified | Becomes the "Administración de datos" page with the two cards |
| `Club12-WebClient/src/modules/backup/*` | New | Service, types, TanStack Query hooks/keys |
| `Club12-WebClient/src/views/core/components/SidebarLayout.tsx`, `modules/core/constants/appRoutes.ts`, `App.tsx` | Modified | Tab label + route rename |
| `Club12-Backend/API.Tests`, `Club12-WebClient` tests | New | Catalog/use-case/authorization + UI/confirmation tests (strict TDD) |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| `pg_restore` against Supabase-hosted Postgres fails on roles/extensions/ownership | High | Dump/restore flags tuned (`--no-owner --no-privileges`, `--clean --if-exists`); validated once manually in staging before the feature is exposed |
| Restore destroys current data if the operator picks the wrong row | Med | Admin-only + confirmation modal naming the backup's `Fecha`; take an automatic pre-restore backup first |
| App writes during restore corrupt the result | Med | Maintenance gate rejects traffic for the whole window; single-flight guard blocks concurrent restore/backup |
| Catalog drifts from actual storage (file deleted out-of-band) | Med | Treat catalog as authoritative for listing; mark/flag missing files on restore attempt rather than crashing |
| Container runs as non-root `$APP_UID` — mounted volume not writable | Med | Named volume with explicit ownership; startup writes a probe and logs an actionable error |
| Dumps contain personal data now sitting on the host filesystem | Med | Restricted host directory permissions; documented in `DEPLOYMENT.md`; endpoints Admin-only |
| Four phases exceed the 800-line review budget in one PR | High | Slice per phase into chained PRs at the `sdd-tasks` stage |
| `container-deployment` main spec is not archived yet (change still active) | Med | Delta targets the active change's spec; sequence archive before or alongside this change |

## Rollback Plan

Per phase, revertible independently. Phase 4/1 config: set `Backup:Enabled=false` and drop the
compose volume — no data loss, only backups stop. Phase 2: remove the restore endpoint registration;
`PgRestoreService` becomes dead code, nothing persisted changes. Phase 3: revert the frontend
commit — the previous Test panel returns intact. The only irreversible-by-config item is the catalog
table, which ships with a standard EF `Down()` migration and holds metadata only, never application
data. Reverting the whole change restores today's exact behavior (feature present, switched off).

## Dependencies

- `postgresql-client` in the backend runtime image, major version compatible with the Supabase
  Postgres server (explicitly excluded by `docker-deployment-setup`; this change reverses that).
- A persistent, writable path on the self-hosted Debian host, plus enough free disk for
  `RetentionCount` dumps.
- ~~The `admin-test-data-tools` work (branch `add-mock-test`, wipe/seed + Test panel) must be merged
  first — phase 3 reshapes exactly those files.~~ **Resolved 2026-08-19**: `add-mock-test` merged
  into `develop` (fast-forward, commit `e9510d1`). Phase 3 can proceed against current `develop`.
- Manual staging sign-off of one real dump→restore cycle before the restore action is enabled.

## Success Criteria

- [ ] A scheduled backup runs in a deployed environment and creates both a file on the server volume and a catalog record with correct `Fecha`, `Peso`, `Forma de creación = Job`, and `path`.
- [ ] "Generar respaldo" produces the same artifacts with `Forma de creación = Manual`, without duplicating or skipping the scheduled run.
- [ ] Backups survive a container restart and a full redeploy.
- [ ] The table lists exactly `Fecha`, `Peso`, `Forma de creación`, `Actions`; both row actions require confirmation before executing.
- [ ] Deleting a row removes both the stored file and its record; restore rebuilds the database from the selected backup and the app returns to service.
- [ ] All four endpoints reject non-Admin callers.
- [ ] "Cargar Datos de prueba" works unchanged from the "Test" card; wipe works unchanged from "Base de datos".
- [ ] Backend and frontend suites pass; new logic is TDD-covered (excluding the documented `pg_dump`/`pg_restore` manual non-goals).

## Proposal question round — resolved 2026-08-19

1. **Maintenance window mechanics:** a manual "exit maintenance" escape hatch is required for a
   stuck window, in addition to auto-clear on success/failure. **Decided: keep the working
   assumption (process-level flag, non-`/health` endpoints answer `503`, frontend banner) and add
   the manual escape hatch** as a hard requirement, not an open question — carry into design.
2. **Pre-restore safety backup: confirmed — yes, always.** Every restore takes an automatic backup
   of the current state immediately before proceeding, recorded as `Forma de creación = Job`, even
   if it pushes the catalog past `RetentionCount`. Accepted tradeoff: restore takes roughly twice as
   long.
3. **Retention vs. manual backups: confirmed — shared pool.** Keep-last-`RetentionCount` (default 7)
   applies to the combined set of scheduled and manual backups; a manual backup can prune a
   scheduled one and vice versa. No separate cap for manual backups.
4. **Empty/failed states:** kept as originally assumed — failed backup attempts are **not**
   catalogued (only successful backups appear in the table); failures are visible in logs only. Not
   reopened.
5. **Disk-full / oversized dumps:** kept as originally assumed — a failed backup logs and no-ops
   without an operator-facing notification (no email/alert channel exists in this project). Not
   reopened.
