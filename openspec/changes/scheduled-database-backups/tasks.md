# Tasks: Scheduled Database Backups

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~1,400 (prod ~720, tests ~710) |
| 400-line budget risk | High |
| 800-line budget (real) | Exceeded even 2-way split; needs 3 units |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 backup-foundations → PR 2 backup-hosted-service → PR 3 backup-storage |
| Delivery strategy | single-pr |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

`single-pr` requires either an explicit `size:exception` for one oversized PR (not recommended at ~1,400 lines) or user approval to run PR 1→2→3 as stacked-to-main or feature-branch-chain.

### Suggested Work Units

| Unit | Goal | PR | Focused test | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 backup-foundations | Retention + pg_dump ports/logic, ~600 lines | PR 1 | `dotnet test --filter Backup.Retention\|Backup.PgDump` | N/A — pure fn + faked `IProcessRunner`, no real binary | Delete `Application/Interfaces/Backup`, `Application/Backup`, `Infrastructure/Backup/{ProcessRunner,PgDumpBackupService}.cs` |
| 2 backup-hosted-service | Scheduler + local storage + DI wiring, ~570 lines | PR 2 | `dotnet test --filter Backup.HostedService\|Backup.LocalStorage` | `dotnet run` with default `Backup:Enabled=false` boot smoke | Remove `AddBackupConfig`/`AddHostedService` from `Program.cs`; delete hosted service + local storage files |
| 3 backup-storage | Supabase adapter reusing existing client, ~250 lines | PR 3 | `dotnet test --filter Backup.SupabaseStorage` | Manual staging only: `Backup:StorageTarget=Supabase`+`Enabled=true` once (spec Non-Goal) | Revert `SupabaseHelper.cs` additive methods + `SupabaseBackupStorage.cs`; `StorageTarget` stays `Local` |

## Phase 1 — Ports & Models (PR 1)

- [x] 1.1 Create `Application/Interfaces/Backup/{IProcessRunner,IDatabaseBackupService,IBackupRetentionPolicy,IBackupStorage,BackupModels}.cs` — new namespace (not `...Interfaces.Services`, avoids reflection Scoped auto-bind)

## Phase 2 — RED: Retention & pg_dump tests (PR 1)

- [x] 2.1 `KeepLastNRetentionPolicyTests.cs`: count ≤ N → none removed; count > N → oldest `(count-N)` removed; timestamp ties → deterministic stable order
- [x] 2.2 `PgDumpBackupServiceTests.cs` w/ `FakeProcessRunner`: non-zero exit → logged, no throw; missing binary → logged, actionable message; asserted arg vector has no shell metachars (subprocess injection)

## Phase 3 — GREEN: Retention & pg_dump (PR 1)

- [x] 3.1 Implement `Application/Backup/KeepLastNRetentionPolicy.cs`
- [x] 3.2 Implement `Infrastructure/Backup/ProcessRunner.cs` (Process.Start wrapper; Win32Exception → failed `ProcessResult`)
- [x] 3.3 Implement `Infrastructure/Backup/PgDumpBackupService.cs` (`ArgumentList`, no shell-interpolated connection string)

## Phase 4 — RED: Hosted service & local storage tests (PR 2)

- [x] 4.1 `DatabaseBackupHostedServiceTests.cs`: interval elapsed → 1 attempt; not elapsed → 0; `Backup:Enabled=false` → zero scheduling/dump/storage calls; dump failure logged, host + other services survive; single-flight skips overlapping tick
- [x] 4.2 `LocalDirectoryBackupStorageTests.cs`: store/list/delete round-trip; reject names outside configured dir (path traversal)

## Phase 5 — GREEN: Hosted service, local storage, wiring (PR 2)

- [x] 5.1 Implement `Infrastructure/Backup/LocalDirectoryBackupStorage.cs`
- [x] 5.2 Implement `API/BackgroundServices/DatabaseBackupHostedService.cs` (PeriodicTimer loop, single-flight guard, per-tick try/catch, calls 3 ports + retention + delete)
- [x] 5.3 Add `AddBackupConfig` to `StartupExtensions.cs`: bind `Backup` options; register singletons explicitly (not scanner-visible)
- [x] 5.4 Wire `Program.cs`: `AddBackupConfig()` + `AddHostedService<DatabaseBackupHostedService>()` guarded by `Backup:Enabled`
- [x] 5.5 Add `Backup` section to `appsettings.json` (Enabled=false, IntervalHours, RetentionCount, StorageTarget=Local, LocalStoragePath, PgDumpPath)
- [x] 5.6 Integration smoke: host boots with `Backup:Enabled=false`, no side effects, other services unaffected (proven by `SmokeTests.GetDivisions_ReturnsOk` — full host boot via `Program.cs`, which now calls `AddBackupConfig`, still passes)

## Phase 6 — RED: Supabase storage tests (PR 3)

- [ ] 6.1 `SupabaseBackupStorageTests.cs`: object-path builder confines names to `backups/` prefix, rejects/normalizes traversal input; storage-target factory selects Supabase vs Local from config

## Phase 7 — GREEN: Supabase storage adapter (PR 3)

- [ ] 7.1 Add additive raw upload/list/remove methods to `SupabaseHelper.cs` (existing `UploadImageAsync`/`DeleteImageAsync` behavior-frozen)
- [ ] 7.2 Implement `Infrastructure/Backup/SupabaseBackupStorage.cs` using new raw methods under `backups/` prefix
- [ ] 7.3 Extend `AddBackupConfig` Supabase branch of the `IBackupStorage` factory
