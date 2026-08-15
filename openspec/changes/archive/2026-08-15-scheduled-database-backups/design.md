# Design: Scheduled Database Backups

## Technical Approach

A single `DatabaseBackupHostedService : BackgroundService` (API layer) drives a
`PeriodicTimer` loop. Each tick runs one backup through three injected ports so
the env-bound parts (`pg_dump`, Supabase) are mockable and the pure logic
(retention) is unit-testable: `IDatabaseBackupService` (create dump),
`IBackupStorage` (persist + list + delete), `IBackupRetentionPolicy`
(keep-last-N). Ports live in Application; adapters that touch the OS/network live
in Infrastructure (Clean Architecture). Fully additive and opt-in
(`Backup:Enabled=false` default) — no existing code path is modified in behavior.

## Architecture Decisions

### Decision: Hosted service in API, adapters in Infrastructure, ports in Application

**Choice**: Hosted service in `API/BackgroundServices`; ports in
`Application/Interfaces/Backup`; `pg_dump`/process/storage adapters in
`Infrastructure/Backup`; pure retention in `Application/Backup`.
**Alternatives**: Put everything in Application (matches the pre-existing
`SupabaseHelper` placement); use Hangfire/Quartz.
**Rationale**: Process execution + external storage are true Infrastructure
concerns; keeping ports in Application preserves testability without a scheduler
dependency. `SupabaseHelper`'s Application placement is an existing deviation, not
a pattern to extend for OS-level I/O.

### Decision: Dedicated `Application.Interfaces.Backup` namespace (not `...Interfaces.Services`)

**Choice**: New namespace for backup ports.
**Alternatives**: Reuse `Application.Interfaces.Services` with `*Service` suffix.
**Rationale**: `RegisterScoped()` reflection-scans exactly
`Application.Interfaces.Services` for `I*Service` and auto-binds as **Scoped**. A
`BackgroundService` is a singleton; scoped backup services would be a
lifetime mismatch. A separate namespace keeps them invisible to the scanner and
lets us register them explicitly as singletons via a new `AddBackupConfig`.

### Decision: `IProcessRunner` abstraction over `Process.Start`

**Choice**: Thin `IProcessRunner` port; `ProcessRunner` adapter in Infrastructure.
**Alternatives**: Call `Process.Start` directly inside `PgDumpBackupService`.
**Rationale**: The spec requires failure-handling unit tests (non-zero exit,
missing binary). A fake `IProcessRunner` makes `PgDumpBackupService` testable
without a real `pg_dump`. No such abstraction exists in the codebase today.

### Decision: Pure keep-last-N retention, separated from I/O

**Choice**: `KeepLastNRetentionPolicy.SelectForDeletion(files, retainCount)` is a
pure function over `BackupFile(Name, Timestamp)` records; listing/deleting is the
storage adapter's job.
**Rationale**: Deterministic, no I/O, trivially unit-tested at boundaries
(0, N, N+1, ties).

### Decision: Reuse existing Supabase client for off-host storage

**Choice**: `SupabaseBackupStorage` reuses the singleton `SupabaseHelper` via new
**additive** raw file methods (upload/list/remove under a dedicated `backups/`
path); existing `UploadImageAsync`/`DeleteImageAsync` untouched.
`LocalDirectoryBackupStorage` is the config-selected fallback.
**Alternatives**: Spin up a second Supabase `Client` in the adapter (duplicate
init); reuse image-shaped `UploadImageAsync<T>` (wrong shape/cache headers).
**Rationale**: Reuse credentials/client; keep existing paths behavior-frozen.

## Data Flow

    PeriodicTimer tick
       │  (single-flight guard: skip if a run is in progress)
       ▼
    IDatabaseBackupService.CreateDumpAsync()  ──uses──▶ IProcessRunner → pg_dump
       │  (reads ConnectionStrings:DbConnection; argument list, no shell string)
       ▼  dump stream/file
    IBackupStorage.StoreAsync(name, stream)   ──▶ Supabase backups/  | local dir
       │
       ▼
    IBackupStorage.ListAsync()  ──▶ IBackupRetentionPolicy.SelectForDeletion(list, N)
       │
       ▼
    IBackupStorage.DeleteAsync(each stale)     (failures logged, never crash host)

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Application/Interfaces/Backup/IDatabaseBackupService.cs` | Create | Create-dump port |
| `Application/Interfaces/Backup/IBackupStorage.cs` | Create | Store/list/delete port |
| `Application/Interfaces/Backup/IBackupRetentionPolicy.cs` | Create | Pruning port |
| `Application/Interfaces/Backup/IProcessRunner.cs` | Create | Process abstraction |
| `Application/Interfaces/Backup/BackupModels.cs` | Create | `BackupFile`, `BackupOptions`, `ProcessResult` records |
| `Application/Backup/KeepLastNRetentionPolicy.cs` | Create | Pure keep-last-N logic |
| `Infrastructure/Backup/ProcessRunner.cs` | Create | `Process.Start` adapter |
| `Infrastructure/Backup/PgDumpBackupService.cs` | Create | `pg_dump` via `IProcessRunner` |
| `Infrastructure/Backup/SupabaseBackupStorage.cs` | Create | Off-host storage adapter |
| `Infrastructure/Backup/LocalDirectoryBackupStorage.cs` | Create | Local fallback |
| `API/BackgroundServices/DatabaseBackupHostedService.cs` | Create | `PeriodicTimer` loop + single-flight |
| `Application/Utils/Helper/SupabaseHelper/SupabaseHelper.cs` | Modify | Add raw upload/list/remove (additive; existing methods unchanged) |
| `API/Utils/StartupExtensions.cs` | Modify | New `AddBackupConfig` (options bind + singleton adapters, storage selected by `Backup:StorageTarget`) |
| `API/Program.cs` | Modify | `AddBackupConfig` + `AddHostedService` (guarded by `Backup:Enabled`) |
| `API/appsettings.json` | Modify | Non-secret `Backup` defaults |
| `API.Tests/Backup/*` | Create | Retention, pg_dump failure, storage-fallback, hosted-service smoke |

## Interfaces / Contracts

```csharp
public sealed record BackupFile(string Name, DateTimeOffset Timestamp);
public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

public interface IProcessRunner {
    Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> args, CancellationToken ct);
}
public interface IDatabaseBackupService { Task<Stream> CreateDumpAsync(CancellationToken ct); }
public interface IBackupStorage {
    Task StoreAsync(string name, Stream content, CancellationToken ct);
    Task<IReadOnlyList<BackupFile>> ListAsync(CancellationToken ct);
    Task DeleteAsync(string name, CancellationToken ct);
}
public interface IBackupRetentionPolicy {
    IReadOnlyList<BackupFile> SelectForDeletion(IReadOnlyList<BackupFile> existing, int retainCount);
}
```

Config (`appsettings.json`, non-secret; secrets reuse `ConnectionStrings:DbConnection` + `SupaBase`):

```json
"Backup": { "Enabled": false, "IntervalHours": 24, "RetentionCount": 7,
            "StorageTarget": "Supabase", "LocalStoragePath": "backups", "PgDumpPath": "pg_dump" }
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Retention at 0 / N / N+1 / timestamp ties | Pure function, no I/O |
| Unit | pg_dump non-zero exit + missing-binary → logged failure, no throw | Fake `IProcessRunner` |
| Unit | Storage target selection Supabase vs Local | Config-driven factory |
| Integration | Host boots with `Backup:Enabled=false` → no service, no regression | `CustomWebApplicationFactory` smoke |
| Manual/Staging | Real `pg_dump` dump+upload | CI uses SQLite; verify in staging |

## Threat Matrix

Subprocess boundary is **Applicable** (spawns `pg_dump`). Git/PR/doc rows are N/A.

| Boundary | Applicability | Design response | Planned RED test |
|---|---|---|---|
| Documentation-like paths | N/A: no file classification | — | — |
| Git repository selection | N/A: no git invocation | — | — |
| Commit / Push / PR commands | N/A: no VCS automation | — | — |
| Subprocess argument injection | Applicable | Pass args via `ArgumentList` (no shell string); connection string + `PgDumpPath` never string-interpolated into a shell | Fake runner asserts arg vector, no shell metachar expansion |
| Subprocess missing/failing binary | Applicable | Non-zero exit / `Win32Exception` → logged, host survives, run skipped | Fake runner returns non-zero + throws → no host crash |
| Storage path traversal | Applicable | Backup names are server-generated (timestamp+guid), confined to `backups/` prefix / configured dir | Reject/normalize names outside prefix |

## Migration / Rollout

No schema or data migration. Ships disabled (`Backup:Enabled=false`); enable per
environment after staging validates `pg_dump` availability. Rollback = remove the
`AddHostedService`/`AddBackupConfig` lines; no data effects.

## Open Questions

- [ ] Confirm prod Postgres is self-managed (not Supabase-managed auto-backups) — implement regardless; keep toggleable.
- [ ] Confirm `pg_dump` binary/version present on the deploy host/container (document as deploy dependency).
