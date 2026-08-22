# Tasks: Database Backup & Restore (Administración de datos)

## Review Workload Forecast

Session `review_budget_lines` = **800** (not the skill-default 400). Risk levels below are assessed
against 800; the literal guard line still reads "400-line budget risk" for downstream matching —
treat its value as risk-vs-configured-budget, not risk-vs-400.

| Field | Value |
|-------|-------|
| Estimated changed lines (whole proposal) | ~2,530 (sum of 6 work units) |
| 400/800-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR1 → PR2 → PR3 → PR4 → PR5 → PR6 (6 work units, 2 per proposal-phase for phases 1–2) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending — user decision required |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

Proposal phases 1 (backup generation) and 2 (restore) are each individually near/over budget, so
both are split into two work units apiece. Phase 3 (view) and phase 4 (server storage) stay single
units.

### Suggested Work Units

| Unit | Goal | Likely PR | Est. lines | Risk | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|-----------|------|----------------------|-----------------|-------------------|
| 1 | Catalog entity + EF mapping + migration (`BackupRecord`, `IBackupCatalog`) | PR1 | ~295 | Medium | `dotnet test --filter FullyQualifiedName~EfBackupCatalog` | N/A — additive migration only, verified via `dotnet ef database update` on a scratch DB | Drop `BackupRecords` table / revert migration; no other code depends on it yet |
| 2 | Shared `IBackupOperationsService` (create/delete) + hosted-service refactor + `BackupController` GET/POST/DELETE | PR2 | ~639 | High | `dotnet test --filter FullyQualifiedName~BackupOperationsService\|BackupController` | Manual: `POST /api/backups` against local Postgres, confirm one catalog row | Revert PR2 diff; catalog table (PR1) stays, unused |
| 3 | `IDatabaseRestoreService` (`psql` adapter) + `IMaintenanceModeState` + `OpenReadAsync` on storage adapters | PR3 | ~368 | Medium | `dotnet test --filter FullyQualifiedName~PsqlDatabaseRestoreService\|MaintenanceModeState` | N/A — adapter has no route yet; exercised only by PR3's own unit tests | Revert PR3 diff; nothing references these types outside their own tests yet |
| 4 | Restore wiring: `BackupOperationsService.RestoreBackupAsync`, `MaintenanceModeMiddleware`, `MaintenanceController`, restore endpoint | PR4 | ~541 | High | `dotnet test --filter FullyQualifiedName~Restore\|Maintenance` | Manual staging: full restore round-trip against a Supabase copy per design's rehearsal step | Revert PR4 diff; `/api/backups` create/list/delete (PR2) keeps working, no restore route |
| 5 | Frontend: `DataAdministrationPage`, `BackupsTable`, backup module (service/hook/types), route/label rename | PR5 | ~633 | High | `pnpm test -- backup` | Manual: `pnpm dev`, click through Base de datos card | Revert PR5 diff; `TestDataPage` restored from git history if needed |
| 6 | Deployment: Dockerfile `postgresql-client-17`, compose volume + memory limit, `.env.example` | PR6 | ~55 | Low | N/A (infra-only) | Manual: `pg_dump --version` / `psql --version` inside container; `POST /api/backups` then `docker compose restart backend`, confirm file survives | `docker-compose.yml` volumes key removal + image redeploy to previous tag |

Ask the user which chain strategy to use before `sdd-apply`: **stacked-to-main** (each PR merges to
main in order) vs **Feature Branch Chain** (PR1 targets a tracker branch, PR2→PR1, PR3→PR2, …, only
the tracker merges to main) vs **size:exception** (single PR, maintainer-approved). Given 6 units and
a High-risk PR2/PR4/PR5, `size:exception` is not recommended.

## Phase 1: Backup Catalog Foundation (PR1)

- [x] 1.1 RED: `EfBackupCatalogTests` — `AddAsync`/`GetByIdAsync`/`ListNewestFirstAsync`/`RemoveAsync` against in-memory/sqlite context (backup-catalog#Catalog-Powers-the-Admin-Backup-Listing)
- [x] 1.2 GREEN: `Domain/Enums/BackupOrigin.cs`, `Domain/Entities/Models/BackupRecord.cs` (`EntityBase`, `StoragePath`/`SizeBytes`/`Origin`)
- [x] 1.3 GREEN: `Application/Interfaces/Backup/IBackupCatalog.cs`, `Infrastructure/Persistance/Configurations/BackupRecordEntityConfiguration.cs` (`.HasConversion<string>()` on `Origin`)
- [x] 1.4 GREEN: `Infrastructure/Persistance/EfBackupCatalog.cs`; add `DbSet<BackupRecord>` to `ApplicationDBContext`/`IClub12DBContext`; `EntityConstants.Tables.BackupRecord`
- [x] 1.5 GREEN: generate `Infrastructure/Migrations/<ts>_AddBackupRecordTable.cs` (additive only); run tests

## Phase 2: Shared Backup Use Case & Manual/Scheduled Wiring (PR2)

- [x] 2.1 RED: `BackupOperationLockTests` — second `WaitAsync(TimeSpan.Zero)` while held returns false (backup-catalog#Single-Shared-Write-Path)
- [x] 2.2 GREEN: `Application/Backup/BackupOperationLock.cs`
- [x] 2.3 RED: `BackupOperationsServiceTests` — concurrent `CreateBackupAsync` → second call `Busy`, no second catalog row
- [x] 2.4 RED: same suite — failed dump (`IDatabaseBackupService` throws) → no `BackupRecord` written (backup-catalog#Failed-Backups-Are-Not-Catalogued)
- [x] 2.5 RED: same suite — `DeleteBackupAsync` with storage file missing → catalog row still removed, warning logged (backup-catalog#Delete-Removes-Both-Stored-File-and-Catalog-Record)
- [x] 2.6 RED: retention test extension — shared Manual+Job pool prunes correctly, tie-break preserved (scheduled-database-backups#Keep-Last-N-Retention-Pruning)
- [x] 2.7 GREEN: `Application/Interfaces/Backup/{IBackupOperationsService,BackupOperationResult}.cs` (Create+Delete only), `Application/DTOs/Backup/Response/BackupRecordResponse.cs`
- [x] 2.8 GREEN: `Application/Backup/BackupOperationsService.cs` — `CreateBackupCoreAsync` + `CreateBackupAsync`/`DeleteBackupAsync`
- [x] 2.9 GREEN: `Infrastructure/Backup/PgDumpBackupService.cs` — add `--clean --if-exists --no-owner --no-privileges`
- [x] 2.10 REFACTOR: `API/BackgroundServices/DatabaseBackupHostedService.cs` — ctor takes `IServiceScopeFactory`; tick resolves `IBackupOperationsService` in a scope; remove `Interlocked` flag
- [x] 2.11 RED: `BackupControllerTests` — GET/POST/DELETE map outcomes to 200/409/404/500; non-Admin → 401/403
- [x] 2.12 GREEN: `API/Controllers/BackupController.cs` (GET/POST/DELETE only)
- [x] 2.13 GREEN: `API/Utils/StartupExtensions.cs` — register scoped `IBackupCatalog`/`IBackupOperationsService`, singleton `BackupOperationLock`

## Phase 3: Restore Service & Psql Adapter (PR3)

- [x] 3.1 RED: `PsqlDatabaseRestoreServiceTests` — asserts exact arg vector via fake `IProcessRunner`; a `StoragePath`/temp path containing `;`/`--` is not reinterpreted (threat: subprocess argument injection)
- [x] 3.2 RED: same suite — non-zero exit / missing `psql` binary → `BackupExecutionException` (threat: subprocess missing/failing binary)
- [x] 3.3 GREEN: `Application/Interfaces/Backup/IDatabaseRestoreService.cs`, `Infrastructure/Backup/PsqlDatabaseRestoreService.cs`; `BackupOptions.PsqlPath`, `ConfigurationKeys.Backup.PsqlPath`
- [x] 3.4 RED: `MaintenanceModeStateTests` — `Enter`/`Exit`, `IsActive`/`Reason`/`EnteredAtUtc` transitions
- [x] 3.5 GREEN: `Application/Interfaces/Backup/IMaintenanceModeState.cs`, `Application/Backup/MaintenanceModeState.cs` (singleton)
- [x] 3.6 RED: `LocalDirectoryBackupStorageTests`/`SupabaseBackupStorageTests` — `OpenReadAsync` re-validates via `ResolveSafePath`; a catalog row with `../../etc/passwd` → `ArgumentException`, no read (threat: storage path traversal)
- [x] 3.7 GREEN: `IBackupStorage.OpenReadAsync` (additive); implement in `LocalDirectoryBackupStorage`/`SupabaseBackupStorage`

## Phase 4: Restore Wiring — Use Case, Controller, Middleware (PR4)

- [x] 4.1 RED: `BackupOperationsServiceTests` — restore takes safety backup with `Origin=Job`, `applyRetention:false`, even past `RetentionCount` (database-restore#Automatic-Pre-Restore-Safety-Backup)
- [x] 4.2 RED: same suite — restore failure (`IDatabaseRestoreService` throws) → maintenance exited, temp file deleted, no host crash (database-restore#Restore-Failure-Is-Logged-and-Isolated; threat: temp-file handling)
- [x] 4.3 GREEN: extend `IBackupOperationsService`/`BackupOperationsService.RestoreBackupAsync` — `Enter()` → `CreateBackupCoreAsync(Job, applyRetention:false)` → `OpenReadAsync` → temp file → `RestoreAsync` → `finally` cleanup + `Exit()`
- [x] 4.4 RED: `MaintenanceModeMiddlewareTests` — `/api/backups`, `/swagger`, an unmatched route all → 503 while active; `/health*` and `/api/maintenance` pass through (threat: routing gate bypass)
- [x] 4.5 RED: `MaintenanceControllerTests` — anonymous/non-Admin `DELETE /api/maintenance` → 401/403 while active (database-restore#Maintenance-Mode-Window; threat: escape-hatch abuse)
- [x] 4.6 GREEN: `API/Utils/Middlewares/MaintenanceModeMiddleware.cs`; register after `UseCors()`, before `UseAuthentication()` in `Program.cs`
- [x] 4.7 GREEN: `API/Controllers/MaintenanceController.cs` (`GET`/`DELETE api/maintenance`), `Application/DTOs/Backup/Response/MaintenanceStatusResponse.cs`
- [x] 4.8 RED: `BackupControllerTests` — `POST api/backups/{id}/restore` route accepts only route `Guid`, no body binding (threat: restore of foreign/uploaded dumps); requires explicit confirmation is a frontend concern (Phase 5)
- [x] 4.9 RED: concurrency test — concurrent restore requests → exactly one runs, others `409` (threat: DoS via repeated restore; database-restore#Restore-Executes-Directly-Against-the-Live-Database)
- [x] 4.10 GREEN: `BackupController.cs` restore endpoint; `StartupExtensions.cs` singletons `IMaintenanceModeState`/`IDatabaseRestoreService`

## Phase 5: Admin Data Administration Panel (PR5)

- [ ] 5.1 RED: `backup.hook.test.ts` — `useBackups()` fetch/create/delete/restore state transitions
- [ ] 5.2 GREEN: `src/modules/backup/type/backup.d.ts`, `service/backup.service.ts`, `hook/backup.hook.ts`, `utils/backupFormat.ts`
- [ ] 5.3 RED: `BackupsTable.test.tsx` — columns Fecha/Peso/Forma de creación/Acciones render; cancel on delete/restore confirm → service not called (admin-data-administration-panel#Confirmation-Required-for-Delete-and-Restore)
- [ ] 5.4 GREEN: `src/views/panel/components/BackupsTable.tsx` — `GridColDef`, `buildActionsColumn`, `confirmDelete`/`confirmAction` wiring
- [ ] 5.5 RED: `DataAdministrationPage.test.tsx` — two-card layout, Admin-only guard (admin-data-administration-panel#Panel-Renamed-and-Restructured-Into-Two-Cards; #Panel-and-Its-Actions-Are-Admin-Only)
- [ ] 5.6 GREEN: `src/views/panel/DataAdministrationPage.tsx` (replaces `TestDataPage.tsx`); delete `TestDataPage.tsx`
- [ ] 5.7 GREEN: `routes.ts` (`backups`, `maintenance`), `httpStatus.ts` (`Conflict`, `ServiceUnavailable`), `appRoutes.ts` (`panelDataAdministration`, path unchanged), `App.tsx`/`SidebarLayout.tsx` label+icon
- [ ] 5.8 RED: maintenance-banner test — `onStatusCode(503, ...)` flips global banner
- [ ] 5.9 GREEN: register 503 handler in `axiosUtils` handler registry

## Phase 6: Server Storage Deployment Config (PR6)

- [ ] 6.1 `Club12-Backend/Dockerfile` — add PGDG apt repo + `postgresql-client-17`; `mkdir -p /app/backups && chown $APP_UID:$APP_UID /app/backups` before `USER $APP_UID`
- [ ] 6.2 `docker-compose.yml` — `backend.volumes: [backup-data:/app/backups]`; top-level `volumes: {backup-data:}`; memory limit `512m` → `1g`
- [ ] 6.3 `.env.example` — `Backup__Enabled`, `Backup__IntervalHours`, `Backup__RetentionCount`, `Backup__StorageTarget`, `Backup__LocalStoragePath=/app/backups`, `Backup__PgDumpPath`, `Backup__PsqlPath`
- [ ] 6.4 Manual verify: `pg_dump --version`/`psql --version` inside container match server major (17); `POST /api/backups` then `docker compose restart backend` confirms file persists
- [ ] 6.5 Follow design's phased rollout order in production: image-only (`Backup__Enabled=false`) → volume → `Backup__Enabled=true` → restore rehearsal in staging before first prod restore

## Key Learnings

1. Design's own phase-1/phase-2 (backup generation, restore) each individually approach or exceed the
   configured 800-line review budget, so both split into two work units apiece — six total instead of four.
2. The session's cached `review_budget_lines=800` overrides the skill's literal-guard default of 400;
   the required guard text still reads "400-line budget risk" for downstream matching but is evaluated
   against 800 here.
3. Threat-matrix rows map cleanly onto phases 3–4 (restore-adjacent) since every Applicable row concerns
   `psql` subprocess execution, storage path resolution, or the maintenance-mode HTTP gate.
4. PR1 (catalog) has no route or controller yet, so its runtime harness is EF-migration verification
   only — it is fully inert until PR2 wires it into `BackupOperationsService`.
5. Restore's safety-backup-before-restore requirement (`applyRetention:false`, `Origin=Job`) is a single
   `BackupOperationsService` code path shared with the existing `CreateBackupCoreAsync`, so its RED
   tests belong in Phase 4 (restore wiring), not Phase 2 (create/delete).
