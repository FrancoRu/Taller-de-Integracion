# Design: Database Backup & Restore (Administración de datos)

## Technical Approach

Extend the existing backup seam rather than rebuild it. The ports
(`IDatabaseBackupService`, `IBackupStorage`, `IBackupRetentionPolicy`, `IProcessRunner`) and their
adapters stay as-is. Four additions carry the change:

1. A **catalog** (`BackupRecord` entity + EF migration + `IBackupCatalog` port) that becomes the
   listing source of truth instead of `IBackupStorage.ListAsync()`.
2. A **shared scoped use case** (`IBackupOperationsService`) that both `DatabaseBackupHostedService`
   and the new `BackupController` call, serialized by one process-wide `SemaphoreSlim`.
3. A **restore path** (`IDatabaseRestoreService` → `psql`) wrapped in a maintenance-mode window
   enforced by an ASP.NET middleware.
4. **Deployment**: PostgreSQL client binaries in the image + a persistent named volume.

Layering follows the archived `2026-08-15-scheduled-database-backups` design exactly: ports in
`Application/Interfaces/Backup`, pure/in-memory logic in `Application/Backup`, OS/DB adapters in
`Infrastructure/Backup`, entity in `Domain/Entities/Models`, EF config + repository in
`Infrastructure/Persistance`, HTTP + middleware in `API`.

## Architecture Decisions

### Decision: `BackupRecord` is a normal `EntityBase` domain entity, not an Infrastructure-only table

| Option | Tradeoff | Decision |
|---|---|---|
| `Domain/Entities/Models/BackupRecord : EntityBase` + `BaseEntityConfiguration<BackupRecord>` | Inherits `Id`/`DateCreated`/`CreatedBy` audit shape and the `IX_*_CreatedAt` index for free; `Fecha` = `DateCreated` (no duplicate timestamp column) | **Chosen** |
| Standalone POCO with its own PK/timestamps outside `EntityBase` | Avoids an unused `UpdatedBy`, but breaks the one convention every other table follows and loses the shared config base | Rejected |
| No table — keep listing off `IBackupStorage.ListAsync()` | Cannot express `Forma de creación`; provider timestamps are unreliable | Rejected (spec forbids) |

`Origin` is persisted as a string via `.HasConversion<string>()`, matching `Match.Type`,
`Stage.StageType`, `PlayerStatistic.Type`, and `PlayerSanction.AppealStatus`.

### Decision: In-process `SemaphoreSlim` singleton as the single-flight guard

Verified against `docker-compose.yml`: the `backend` service has **no `deploy.replicas` and no
`deploy.mode`** — only `deploy.resources.limits.memory`. It is a single container, single process,
behind one frontend proxy. A `SemaphoreSlim(1,1)` held by a singleton `BackupOperationLock` is
therefore sufficient and correct.

| Option | Tradeoff | Decision |
|---|---|---|
| Singleton `SemaphoreSlim(1,1)`, `WaitAsync(TimeSpan.Zero)` → busy | Correct for single instance; zero new infrastructure; trivially unit-testable | **Chosen** |
| Postgres advisory lock (`pg_advisory_lock`) | Survives multi-replica, but restore drops/recreates the very connection holding it | Rejected |
| Keep the existing per-instance `Interlocked` flag in the hosted service | Does not cover the HTTP endpoint — the two paths could run concurrently | Rejected |

**Constraint to record:** if the deployment ever scales past one replica, this guard silently stops
working. `docker-compose.yml` MUST NOT gain `deploy.replicas > 1` without replacing this mechanism.

The existing `Interlocked _isRunning` flag in `DatabaseBackupHostedService` is removed — the
semaphore subsumes it, and keeping both would produce two competing skip paths.

### Decision: Keep plain-SQL dumps; restore with `psql`, not `pg_restore`

`PgDumpBackupService` captures `pg_dump` stdout as a **string** (`ProcessResult.StdOut`) and returns
a UTF-8 `MemoryStream`. That is plain-SQL format. `pg_restore` only accepts custom/tar archives, so
using it would require reworking `ProcessRunner` to stream binary stdout — a rewrite of shipped,
tested phase-1 code.

| Option | Tradeoff | Decision |
|---|---|---|
| `pg_dump` plain SQL (unchanged) + `psql -v ON_ERROR_STOP=1 -f <tmp>` | Zero change to `ProcessRunner`/`IBackupStorage` stream contracts; `ON_ERROR_STOP=1` gives a non-zero exit on the first error | **Chosen** |
| Switch to `pg_dump -Fc` + `pg_restore` | "Correct" tool, but forces binary-safe stdout in `ProcessRunner` and re-tests all of phase 1 | Rejected (spec allows "or equivalent") |

Safety flags move onto the **dump** side (where plain format needs them):
`--clean --if-exists --no-owner --no-privileges`. This directly addresses the proposal's High risk
about Supabase-managed roles/ownership.

**Memory:** the whole dump lives in a string plus a `MemoryStream` (~2x) inside a container capped at
`512m`. This change raises the backend limit to `1g` and records streaming-to-file as deferred work.

### Decision: Maintenance gate is middleware placed after `UseCors`, before `UseAuthentication`

| Option | Tradeoff | Decision |
|---|---|---|
| `MaintenanceModeMiddleware` (mirrors `MustChangePasswordMiddleware`) with a path allow-list | Covers *every* request including unmatched routes and Swagger; the escape hatch stays reachable because its path is allow-listed and the controller does its own `[Authorize]` | **Chosen** |
| `IAsyncActionFilter` / `IEndpointFilter` | Only fires for matched MVC endpoints; unmatched routes and static/Swagger paths would still answer normally | Rejected |
| Short-circuit inside `UseAuthorization` | Non-Admin callers would get 401/403 instead of the specified 503 | Rejected |

Placed **after** `UseCors()` so the 503 carries CORS headers, and **before** `UseAuthentication()` so
the gate is not preempted by auth failures. Allow-list: `/health` (covers `/health/ready` via
`StartsWithSegments`) and `/api/maintenance` (the escape hatch).

### Decision: Maintenance state lives in `Application/Backup`, not `Infrastructure`

It is pure in-memory process state with no OS/network I/O — same category as
`KeepLastNRetentionPolicy`, which the archived design already placed in `Application/Backup`.
Registered as a singleton in `AddBackupConfig`.

### Decision: Controllers return an explicit outcome, not exception-mapped status codes

The use case returns `BackupOperationResult(Outcome, Record, Message)`; the controller maps
`Busy → 409`, `NotFound → 404`, `Failed → 500`, `Completed → 200`. This avoids coupling to
`GlobalExceptionHandler`'s mapping and keeps controller tests pure. `Ok(result)` is used rather than
`CreatedAtAction` — matching `DataMaintenanceController` and avoiding the route-value mismatch bug
already fixed once in commit `986cc67`.

### Decision: `Backup:Enabled` gates only the scheduled job

`Program.cs` keeps its `Backup:Enabled` guard around `AddHostedService`. The catalog, the manual
endpoint, delete, and restore register unconditionally. Turning the schedule off must not disable
the operator's ability to take or restore a backup.

## Data Flow

    ── Manual ────────────────────────────────────────────────────────────
    POST /api/backups ─▶ BackupController ─▶ IBackupOperationsService (scoped)
                                                    │
    ── Scheduled ─────────────────────────────────── │
    PeriodicTimer tick ─▶ IServiceScopeFactory ──────┘
                                                    │ acquire BackupOperationLock (0ms) → busy? 409
                                                    ▼
                              IDatabaseBackupService.CreateDumpAsync()  ─▶ pg_dump
                                                    │ stream.Length = Peso
                                                    ▼
                              IBackupStorage.StoreAsync(name, stream)
                                                    ▼
                              IBackupCatalog.AddAsync(BackupRecord)      ← only on success
                                                    ▼
                              retention: catalog (shared Manual+Job pool)
                                → IBackupStorage.DeleteAsync + IBackupCatalog.RemoveAsync

    ── Restore ───────────────────────────────────────────────────────────
    POST /api/backups/{id}/restore ─▶ acquire lock ─▶ IMaintenanceModeState.Enter()
        ─▶ CreateBackupCoreAsync(Job, applyRetention: false)      ← safety backup, always
        ─▶ IBackupStorage.OpenReadAsync(record.StoragePath) → temp file
        ─▶ IDatabaseRestoreService.RestoreAsync(tempPath)   ─▶ psql -f
        ─▶ finally: delete temp file; IMaintenanceModeState.Exit()  ← success OR failure

    While Enter() is active: every request except /health* and /api/maintenance → 503 + Retry-After

## File Changes

### Backend — new

| File | Description |
|---|---|
| `Domain/Entities/Models/BackupRecord.cs` | Catalog entity (`EntityBase`) |
| `Domain/Enums/BackupOrigin.cs` | `Manual` \| `Job` |
| `Application/Interfaces/Backup/IBackupCatalog.cs` | Catalog port |
| `Application/Interfaces/Backup/IBackupOperationsService.cs` | Shared create/delete/restore use case |
| `Application/Interfaces/Backup/IDatabaseRestoreService.cs` | Restore port |
| `Application/Interfaces/Backup/IMaintenanceModeState.cs` | Maintenance flag port |
| `Application/Interfaces/Backup/BackupOperationResult.cs` | `BackupOperationOutcome` + result record |
| `Application/Backup/MaintenanceModeState.cs` | Singleton in-memory flag |
| `Application/Backup/BackupOperationLock.cs` | Singleton `SemaphoreSlim(1,1)` wrapper |
| `Application/Backup/BackupOperationsService.cs` | The shared use case (scoped) |
| `Application/DTOs/Backup/Response/BackupRecordResponse.cs` | API DTO |
| `Application/DTOs/Backup/Response/MaintenanceStatusResponse.cs` | API DTO |
| `Infrastructure/Backup/PsqlDatabaseRestoreService.cs` | `psql -f` adapter |
| `Infrastructure/Persistance/EfBackupCatalog.cs` | `IBackupCatalog` over `ApplicationDBContext` |
| `Infrastructure/Persistance/Configurations/BackupRecordEntityConfiguration.cs` | EF mapping |
| `Infrastructure/Migrations/<ts>_AddBackupRecordTable.cs` | Additive table |
| `API/Controllers/BackupController.cs` | `api/backups` |
| `API/Controllers/MaintenanceController.cs` | `api/maintenance` |
| `API/Utils/Middlewares/MaintenanceModeMiddleware.cs` | 503 gate |

### Backend — modified

| File | Change |
|---|---|
| `Application/Interfaces/Backup/IBackupStorage.cs` | Add `Task<Stream> OpenReadAsync(string name, CancellationToken ct = default)` |
| `Infrastructure/Backup/LocalDirectoryBackupStorage.cs` | Implement `OpenReadAsync` via existing `ResolveSafePath` |
| `Infrastructure/Backup/SupabaseBackupStorage.cs` | Implement `OpenReadAsync` (download) |
| `Infrastructure/Backup/PgDumpBackupService.cs` | Add `--clean --if-exists --no-owner --no-privileges` to the arg vector |
| `Application/Interfaces/Backup/BackupOptions.cs` | Add `public string PsqlPath { get; set; } = "psql";` |
| `Application/Utils/Constants/Configuration/ConfigurationKeys.cs` | Add `Backup.PsqlPath` |
| `API/BackgroundServices/DatabaseBackupHostedService.cs` | Ctor takes `IServiceScopeFactory` + `BackupOptions` + logger; tick resolves `IBackupOperationsService` in a scope; drop `Interlocked` flag and inline dump/store/prune logic |
| `API/Utils/StartupExtensions.cs` | `AddBackupConfig`: singletons `IMaintenanceModeState`, `BackupOperationLock`, `IDatabaseRestoreService`; **scoped** `IBackupCatalog`, `IBackupOperationsService` |
| `API/Program.cs` | `.UseMiddleware<MaintenanceModeMiddleware>()` after `UseCors()` |
| `Infrastructure/Persistance/ApplicationDBContext.cs`, `IClub12DBContext.cs` | `DbSet<BackupRecord> BackupRecords` |
| `Infrastructure/Persistance/EntityConstants.cs` | `Tables.BackupRecord = "BackupRecords"` |
| `API/appsettings.json`, `appsettings.Development.json` | Add `"PsqlPath": "psql"` |

### Frontend

| File | Action | Description |
|---|---|---|
| `src/modules/backup/type/backup.d.ts` | Create | `IBackupRecordResponse`, `IMaintenanceStatusResponse` |
| `src/modules/backup/service/backup.service.ts` | Create | `getBackups`/`createBackup`/`deleteBackup`/`restoreBackup` |
| `src/modules/backup/hook/backup.hook.ts` | Create | `useBackups()` |
| `src/modules/backup/utils/backupFormat.ts` | Create | `formatBytes`, `BACKUP_ORIGIN_LABELS` |
| `src/views/panel/DataAdministrationPage.tsx` | Create (replaces `TestDataPage.tsx`) | Two-card layout |
| `src/views/panel/components/BackupsTable.tsx` | Create | `DataGrid` + `buildActionsColumn` |
| `src/views/panel/TestDataPage.tsx` | Delete | Superseded |
| `src/modules/core/constants/routes.ts` | Modify | `backups: 'backups'`, `maintenance: 'maintenance'` |
| `src/modules/core/constants/httpStatus.ts` | Modify | Add `Conflict: 409`, `ServiceUnavailable: 503` |
| `src/modules/core/constants/appRoutes.ts` | Modify | `panelTest` → `panelDataAdministration`, **path value `/panel/test` unchanged** |
| `src/App.tsx`, `src/views/core/components/SidebarLayout.tsx` | Modify | Label `'Test'` → `'Administración de datos'`, icon `ScienceIcon` → `StorageIcon` |

### Deployment

| File | Change |
|---|---|
| `Club12-Backend/Dockerfile` | PGDG apt repo + `postgresql-client-17`; `mkdir -p /app/backups && chown $APP_UID:$APP_UID /app/backups` **before** `USER $APP_UID` |
| `docker-compose.yml` | `backend.volumes: [backup-data:/app/backups]`; top-level `volumes: {backup-data:}`; memory limit `512m` → `1g` |
| `.env.example` | `Backup__Enabled`, `Backup__IntervalHours`, `Backup__RetentionCount`, `Backup__StorageTarget`, `Backup__LocalStoragePath=/app/backups`, `Backup__PgDumpPath`, `Backup__PsqlPath` |

## Interfaces / Contracts

```csharp
// Domain/Enums/BackupOrigin.cs
public enum BackupOrigin { Manual, Job }

// Domain/Entities/Models/BackupRecord.cs — Fecha == EntityBase.DateCreated
public class BackupRecord : EntityBase
{
    public required string StoragePath { get; set; }   // IBackupStorage key; max 260
    public required long SizeBytes { get; set; }       // Peso
    public required BackupOrigin Origin { get; set; }  // Forma de creación (string in DB)
}

// Application/Interfaces/Backup/IBackupCatalog.cs  (scoped, EF-backed)
public interface IBackupCatalog
{
    Task<IReadOnlyList<BackupRecord>> ListNewestFirstAsync(CancellationToken ct = default);
    Task<BackupRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BackupRecord> AddAsync(BackupRecord record, CancellationToken ct = default);
    Task RemoveAsync(BackupRecord record, CancellationToken ct = default);
}

// Application/Interfaces/Backup/IBackupOperationsService.cs  (scoped; the ONE write path)
public interface IBackupOperationsService
{
    Task<BackupOperationResult> CreateBackupAsync(BackupOrigin origin, CancellationToken ct = default);
    Task<BackupOperationResult> DeleteBackupAsync(Guid id, CancellationToken ct = default);
    Task<BackupOperationResult> RestoreBackupAsync(Guid id, CancellationToken ct = default);
}

public enum BackupOperationOutcome { Completed, Busy, NotFound, Failed }
public sealed record BackupOperationResult(
    BackupOperationOutcome Outcome, BackupRecordResponse? Record, string? Message);

// Application/Interfaces/Backup/IDatabaseRestoreService.cs
public interface IDatabaseRestoreService
{
    /// <summary>Restores from a local plain-SQL dump file. Throws BackupExecutionException on failure.</summary>
    Task RestoreAsync(string dumpFilePath, CancellationToken ct = default);
}

// Application/Interfaces/Backup/IMaintenanceModeState.cs  (singleton)
public interface IMaintenanceModeState
{
    bool IsActive { get; }
    string? Reason { get; }
    DateTimeOffset? EnteredAtUtc { get; }
    void Enter(string reason);
    void Exit();
}

// Application/Interfaces/Backup/IBackupStorage.cs — ADDITIVE
Task<Stream> OpenReadAsync(string name, CancellationToken ct = default);
```

Internal sequencing inside `BackupOperationsService` — the lock is acquired **once** per public call;
`CreateBackupCoreAsync` assumes it is already held, which is what lets restore take its safety backup
without self-deadlocking:

```csharp
private async Task<BackupOperationResult> CreateBackupCoreAsync(
    BackupOrigin origin, bool applyRetention, CancellationToken ct);
// restore: Enter() → CreateBackupCoreAsync(Job, applyRetention: false) → psql → finally Exit()
```

`applyRetention: false` on the safety backup is what implements "catalogued even if this exceeds
`RetentionCount`". Retention itself reads the **catalog** (not `IBackupStorage.ListAsync()`), so the
`Manual` + `Job` pool is inherently shared.

### HTTP contract — mirrors `DataMaintenanceController` exactly

`[ApiController]`, `[Authorize(Roles = Roles.Admin)]`, `CancellationToken ct` parameter,
`ProducesResponseType` for 401/403, `Ok(result)` on success.

| Method | Route | Success | Failure |
|---|---|---|---|
| `GET` | `api/backups` | `200 IReadOnlyList<BackupRecordResponse>` | 401/403 |
| `POST` | `api/backups` | `200 BackupRecordResponse` | `409` busy, `500` failed |
| `DELETE` | `api/backups/{id:guid}` | `204` | `404`, `409` busy |
| `POST` | `api/backups/{id:guid}/restore` | `200 BackupRecordResponse` (the safety backup) | `404`, `409` busy, `500` failed |
| `GET` | `api/maintenance` | `200 MaintenanceStatusResponse` | 401/403 |
| `DELETE` | `api/maintenance` | `204` (escape hatch: force-exit) | 401/403 |

```csharp
public sealed record BackupRecordResponse(
    Guid Id, DateTime CreatedAt, long SizeBytes, string Origin, string StoragePath);
public sealed record MaintenanceStatusResponse(
    bool IsActive, string? Reason, DateTimeOffset? EnteredAtUtc);
```

### Frontend contract

```ts
// modules/backup/type/backup.d.ts
export interface IBackupRecordResponse {
  id: string; createdAt: string; sizeBytes: number;
  origin: 'Manual' | 'Job'; storagePath: string;
}
export interface IMaintenanceStatusResponse {
  isActive: boolean; reason: string | null; enteredAtUtc: string | null;
}

// modules/backup/service/backup.service.ts — sendGet/sendPost/sendDelete, routes.backups
export const backupService = {
  getBackups: async (): Promise<AxiosResponse<IBackupRecordResponse[]>> =>
    await sendGet(routes.backups),
  createBackup: async (): Promise<AxiosResponse<IBackupRecordResponse>> =>
    await sendPost(routes.backups),
  deleteBackup: async (id: string): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.backups}/${id}`),
  restoreBackup: async (id: string): Promise<AxiosResponse<IBackupRecordResponse>> =>
    await sendPost(`${routes.backups}/${id}/restore`),
  exitMaintenance: async (): Promise<AxiosResponse<void>> =>
    await sendDelete(routes.maintenance),
};

// modules/backup/hook/backup.hook.ts — plain state hook, NOT a context provider
export const useBackups = (): {
  backups: IBackupRecordResponse[]; loading: boolean; busy: boolean;
  fetchBackups: () => Promise<void>;
  createBackup: () => Promise<boolean>;
  deleteBackup: (id: string) => Promise<boolean>;
  restoreBackup: (id: string) => Promise<boolean>;
} => { /* useState + useCallback */ };
```

Deviation noted: `useVenue` is a context-consumer hook because `VenuesPage` is embedded in several
views. `useBackups` has exactly one consumer (`BackupsTable`), so a plain state hook avoids a
provider that nothing else would use.

`DataAdministrationPage.tsx` structure — `<Typography variant="h5">Administración de datos</Typography>`
then two `<Card>`s:

- **Card 1 "Base de datos"** — `Stack direction="row"`: `Borrar los datos` (`DeleteSweepIcon`,
  `color="error"`, existing `handleWipe` verbatim) and `Generar respaldo` (`BackupIcon`,
  `variant="contained"`), then `<BackupsTable />`.
- **Card 2 "Test"** (below) — `Cargar Datos de prueba` (`ScienceIcon`, existing `handleSeed`
  verbatim, including the 409 message).

`BackupsTable.tsx` columns via `GridColDef<IBackupRecordResponse>[]`:

| field | headerName | render |
|---|---|---|
| `createdAt` | `Fecha` | `new Date(v).toLocaleString('es-AR')` |
| `sizeBytes` | `Peso` | `formatBytes(v)` |
| `origin` | `Forma de creación` | `BACKUP_ORIGIN_LABELS[v]` → `Manual` / `Programado` |
| — | `Acciones` | `buildActionsColumn([...])` |

Row actions via `TableRowAction<IBackupRecordResponse>[]`, both confirmation-gated:

```ts
{ label: 'Eliminar', color: 'error', icon: <DeleteIcon fontSize="small" />, onClick: handleDelete }
{ label: 'Restaurar', color: 'warning', icon: <RestoreIcon fontSize="small" />, onClick: handleRestore }
```

`handleDelete` → `confirmDelete({ title: '¿Eliminar este respaldo?', ... })`.
`handleRestore` → `confirmAction({ icon: 'warning', title: '¿Restaurar la base desde este respaldo?',
text: 'Se sobrescribe TODA la base con el respaldo del {fecha}. Antes se genera un respaldo
automático del estado actual. El sistema queda en mantenimiento durante la operación.',
confirmButtonText: 'Sí, restaurar' })` — the text names the row's `Fecha`, satisfying
"confirmation naming the target backup's `Fecha`".

Empty state: `localeText={{ noRowsLabel: 'Todavía no hay respaldos generados.' }}` (neutral — failed
attempts are never catalogued).

Maintenance banner: register `onStatusCode(HttpStatus.ServiceUnavailable, ...)` in the existing
`axiosUtils` handler registry to flip a global banner; already-supported extension point, no
interceptor rewrite.

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Unit | Single-flight: second `CreateBackupAsync` while one is in flight → `Busy`, no second catalog row | Fake ports + real `BackupOperationLock` |
| Unit | Failed dump / failed store → **no** `BackupRecord` written | Fake `IDatabaseBackupService` throwing `BackupExecutionException` |
| Unit | Safety backup uses `applyRetention: false` and `Origin = Job` | Spy catalog + spy retention |
| Unit | Restore failure → maintenance cleared, temp file deleted, no host crash | Fake `IDatabaseRestoreService` throwing |
| Unit | Delete with missing file → catalog row still removed, warning logged | Fake storage throwing `FileNotFound` |
| Unit | Retention prunes across the shared Manual+Job pool | Existing `KeepLastNRetentionPolicy` tests + catalog-sourced input |
| Unit | `psql` non-zero exit / missing binary → `BackupExecutionException`, arg vector asserted | Fake `IProcessRunner` |
| Integration | Maintenance active → `/health` 200, `/api/backups` 503, `/api/maintenance` reachable | `CustomWebApplicationFactory` + `IMaintenanceModeState.Enter()` |
| Integration | Non-Admin → 403 on every new endpoint | Existing `AuthorizationGatingTests` pattern |
| Frontend | Cancel on delete/restore confirm → service not called | Vitest + mocked `Swal.fire` (`TeamsPage.test.tsx` pattern) |
| Frontend | Table renders Fecha/Peso/Forma de creación/Acciones | RTL + `findByTestId('RestoreIcon')` |
| Manual/Staging | Real `pg_dump`/`psql` round-trip against a Supabase-hosted PG copy | Sign-off gate before enabling in prod |

## Threat Matrix

Applicable: subprocess execution and HTTP routing. VCS/PR and executable-file-classification rows
are N/A.

| Boundary | Applicability | Design response | Planned RED test |
|---|---|---|---|
| Documentation-like path classification | N/A — no file classification | — | — |
| Git repository selection | N/A — no git invocation | — | — |
| Commit / Push / PR commands | N/A — no VCS automation | — | — |
| Subprocess argument injection (`psql`) | Applicable | Args via `ProcessStartInfo.ArgumentList` only (existing `ProcessRunner`); password via `PGPASSWORD` env, never argv; `PsqlPath` from config, never interpolated | Fake runner asserts exact arg vector; a `StoragePath` containing `;`/`--` is not reinterpreted |
| Subprocess missing/failing binary | Applicable | Non-zero exit or `-1` sentinel → `BackupExecutionException` → logged, maintenance cleared, host survives | Fake runner returns `-1` and non-zero → 500 result, no crash, `IsActive == false` |
| Storage path traversal | Applicable | `BackupRecord.StoragePath` is server-generated (`backup-{ts}-{guid}.sql`) and still re-validated by `LocalDirectoryBackupStorage.ResolveSafePath` on both `OpenReadAsync` and `DeleteAsync` | Seed a catalog row with `../../etc/passwd` → `ArgumentException`, no read |
| Restore of foreign/uploaded dumps | Applicable | No upload endpoint exists; restore takes only a catalog `Guid` and reads via `IBackupStorage`. Explicitly out of scope in the proposal | Endpoint accepts `Guid` only — no path/body input |
| Temp-file handling during restore | Applicable | Temp file under `Path.GetTempPath()`, unique name, deleted in `finally` even on failure | Failure path asserts the temp file no longer exists |
| Routing gate bypass (maintenance 503) | Applicable | Middleware runs before endpoint routing; allow-list is exactly `/health` + `/api/maintenance` via `StartsWithSegments` | `/api/backups`, `/swagger`, and an unmatched route all → 503 while active |
| Escape-hatch abuse (force-exit) | Applicable | `DELETE /api/maintenance` is `[Authorize(Roles = Admin)]`; allow-listing it in the middleware bypasses the 503 gate only, never authentication/authorization | Anonymous and non-Admin `DELETE /api/maintenance` → 401/403 while maintenance is active |
| Denial of service via repeated restore | Applicable | Single-flight lock returns `409` immediately (`WaitAsync(TimeSpan.Zero)`); no queueing, no unbounded work | Concurrent restore requests → exactly one runs, others 409 |

## Migration / Rollout

**Schema.** One additive migration (`AddBackupRecordTable`) creating `Club12."BackupRecords"`. No
existing table is altered, no backfill. It runs automatically via
`app.ExecuteMigrationsAndSeedAsync()` at startup. Forward- and backward-compatible: the previous
image simply ignores the table, so an image rollback needs no down-migration.

**PostgreSQL client major version — confirmed 2026-08-19.** Production Supabase server reports
`PostgreSQL 17.6 on aarch64-unknown-linux-gnu, compiled by gcc (GCC) 13.2.0, 64-bit`. The `aspnet:8.0`
runtime is Debian 12 (bookworm); its stock `postgresql-client` is PG 15, which `pg_dump` refuses to
run against a *newer* server. **Pin `postgresql-client-17`** from the PGDG apt repo in the Dockerfile
(architecture note: PGDG ships x86_64 packages for this base image regardless of the server's own
aarch64 host — the client's CPU arch is independent of the server's and must match the *build* image's
arch, not the server's).

**Phased rollout on the running production deployment** (each phase independently reversible):

1. **Image only.** Deploy the new image with `Backup__Enabled=false` and no volume yet. Verify inside
   the container: `pg_dump --version`, `psql --version`, and that the major matches the server.
   *Rollback:* redeploy the previous image tag. No data effect.
2. **Volume.** Add `backup-data:/app/backups` and raise the memory limit; `docker compose up -d`
   recreates the backend container. Because the Dockerfile `mkdir`s and `chown`s `/app/backups` to
   `$APP_UID` **before** `USER $APP_UID`, Docker seeds the fresh named volume with that ownership, so
   the non-root process can write. *A bind mount would not inherit it* — a host-side
   `chown -R 1654:1654` would be required first. Verify by calling `POST /api/backups` once and
   confirming the file survives `docker compose restart backend`.
   *Rollback:* remove the `volumes:` key and redeploy. `docker compose down` (without `-v`) never
   destroys the named volume, so backups survive a full rollback.
3. **Schedule on.** Set `Backup__Enabled=true` and restart. *Rollback:* flip back to `false` and
   restart — the hosted service is registered only when the flag is true at startup, so this is a
   clean on/off with no partial state. Manual backup and restore keep working either way.
4. **Restore rehearsal.** Exercise a full restore in staging against a copy of the production
   database before the first production restore. The pre-restore safety backup is the rollback for a
   bad restore: restore it back.

**Stuck maintenance window.** If the process is killed mid-restore, `IMaintenanceModeState` is
in-memory and resets to inactive on the next start — a container restart is itself an escape hatch.
`DELETE /api/maintenance` covers the case where the process is alive but the window did not clear.

## Open Questions

- [x] ~~Confirm the production Supabase PostgreSQL server major version~~ **Resolved 2026-08-19: PG
      17.6.** Pin `postgresql-client-17` in the Dockerfile.
- [ ] Confirm `1g` is an acceptable backend memory limit on the self-hosted Debian server given the
      in-memory dump buffering; if not, streaming `pg_dump` to a file must be pulled forward instead
      of deferred.

## Key Learnings

1. `docker-compose.yml` declares no `deploy.replicas`, so an in-process `SemaphoreSlim` is a valid
   single-flight guard for this deployment.
2. `PgDumpBackupService` returns plain SQL captured through `ProcessResult.StdOut` as a string, which
   rules out `pg_restore` and makes `psql -f` the correct restore tool.
3. `EntityBase.DateCreated` already supplies the catalog's `Fecha`, so `BackupRecord` needs no second
   timestamp column.
4. The Debian bookworm stock `postgresql-client` is PG 15 and will refuse to dump from a newer
   Supabase server; production is confirmed PG 17.6, so `postgresql-client-17` must be pinned from
   the PGDG repository.
5. A middleware gate placed after `UseCors` but before `UseAuthentication` returns 503 for every
   request shape while keeping the Admin-only escape hatch reachable through a path allow-list.
