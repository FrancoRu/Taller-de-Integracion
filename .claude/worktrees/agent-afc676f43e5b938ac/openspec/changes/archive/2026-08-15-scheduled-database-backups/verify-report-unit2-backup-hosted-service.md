# Verify Report: scheduled-database-backups - Unit 2 (backup-hosted-service)

**Verdict**: PASS (0 CRITICAL, 1 WARNING, 1 SUGGESTION)

Independently re-verified, not trusted from the apply report. Covers Phase 4-5 of tasks.md (DatabaseBackupHostedService, LocalDirectoryBackupStorage, AddBackupConfig, Program.cs/appsettings wiring). Unit 1 (foundations) was verified separately and is not re-litigated here beyond confirming it is still green in the full suite.

## Evidence

- dotnet build Club12-Backend/Solution/Club12.sln --no-incremental -> 0 errors, 432 warnings (all pre-existing CS1591/CS1573 missing-XML-doc patterns on unrelated existing controllers/helpers; consistent with unit 1's documented convention of sparse XML docs).
- dotnet test Club12-Backend/Solution/Club12.sln -> 46 passed, 0 failed, 0 skipped (35 pre-existing/unit-1 + 11 new unit-2).
- Flakiness stress test (explicit ask #1): ran filter FullyQualifiedName~API.Tests.Backup 3 times in a row -> 25/25 passed every time, ~585ms each run, no variance.
- Ran filter FullyQualifiedName~DatabaseBackupHostedServiceTests (the timing-sensitive suite specifically) 5 times in a row -> 5/5 passed every time, ~570-580ms each run. The apply report's claim of rerunning 3x after the fix with no further flakes is now independently reproduced with a wider margin (5 runs) - genuinely stable, not just claimed.

## Code correctness (verified by direct source read)

### DatabaseBackupHostedService.cs (Club12-Backend/API/BackgroundServices/DatabaseBackupHostedService.cs)
- Enabled gate: ExecuteAsync checks the enabled flag as the very first statement (log then return) - before the PeriodicTimer is even constructed and before any port (IDatabaseBackupService/IBackupStorage) is touched. Confirmed genuine short-circuit, not just a skip-inside-the-loop.
- Single-flight guard: TryStartBackupAttempt uses Interlocked.CompareExchange on an internal running flag - an overlapping tick that finds the flag already set returns a completed task immediately (logged as a warning) instead of starting a second concurrent attempt; the flag is reset in a finally block inside RunBackupAttemptAsync. This is a real atomic guard, not a cosmetic flag - confirmed via the SingleFlight test, which gates the fake backup service with a TaskCompletionSource across several elapsed intervals and asserts call count stays at 1 until released, then reaches 2+ after release/reset.
- Failure isolation: RunBackupAttemptAsync wraps the whole dump/store/prune sequence in a catch for BackupExecutionException plus a defense-in-depth generic catch (excluding OperationCanceledException), both logged via logger.LogError and swallowed - never rethrown. Since a tick is dispatched fire-and-forget (not awaited inline in the loop), and the returned Task never carries an unhandled exception, ExecuteAsync's own while loop cannot be crashed by a failed backup attempt. Confirmed genuine - not merely claimed - by direct read of the catch/finally structure and independently by the BackupFails test, which triggers a real failure on the first call and asserts a real error-level log entry plus a successful store on a later tick.

### LocalDirectoryBackupStorage.cs (Club12-Backend/Infrastructure/Backup/LocalDirectoryBackupStorage.cs)
- Path-traversal guard (ResolveSafePath) rejects any rooted/absolute name outright (Path.IsPathRooted), then resolves Path.GetFullPath(Path.Combine(directoryPath, name)) and requires the result to start with the configured directory plus separator, throwing ArgumentException otherwise. Conceptually verified this cannot be bypassed by a crafted filename: Path.GetFullPath normalizes ".." segments before the prefix check runs, so a path-traversal-style name resolves outside the directory and is rejected; a nested traversal name is likewise normalized and caught. This is a real guard, not just an existence check - confirmed both by source read and by the LocalDirectoryBackupStorageTests.cs traversal-reject cases (relative traversal and rooted-path variants both covered).

### AddBackupConfig in StartupExtensions.cs
- Confirmed the reflection-based RegisterScoped/HelperRegisterScoped scanner only walks three fixed namespaces derived from the service, repository, and mapper marker-interface namespaces (i.e. Application.Interfaces.Services, the repository namespace, and the mapper namespace). The new backup ports live in Application.Interfaces.Backup / Application.Backup / Infrastructure.Backup, which are never in that scan's namespace set, so they are genuinely invisible to the scoped auto-binder.
- AddBackupConfig registers every backup type explicitly via AddSingleton calls - IBackupRetentionPolicy, IProcessRunner, IDatabaseBackupService, IBackupStorage (factory lambda), and DatabaseBackupHostedService itself. All five registrations use AddSingleton, none use AddScoped. Confirmed matches the claimed explicit-singletons behavior, not accidentally caught by the reflection scan.
- StorageTarget=Supabase currently logs a warning and falls back to LocalDirectoryBackupStorage (Supabase adapter doesn't exist until unit 3) - matches the documented unit-2 deviation #5 in apply-progress.

### Program.cs wiring
- AddBackupConfig always runs (binds options and registers the singleton DatabaseBackupHostedService in the DI container regardless of the flag).
- The AddHostedService call is wrapped in a configuration check reading the Enabled flag - the IHostedService registration itself is conditional, not just a no-op inside the service. When the flag is false, the singleton exists in the container but is never added to the hosted-services collection, so the ASP.NET Core host never calls StartAsync on it. This matches the apply report's claim and is defense-in-depth alongside the service's own internal enabled check (belt-and-suspenders: even if something else constructed and started it directly, ExecuteAsync still no-ops).

### appsettings.json / appsettings.Development.json
Both confirmed to contain a Backup section with Enabled false, IntervalHours 24, RetentionCount 7, StorageTarget Local, LocalStoragePath backups, PgDumpPath pg_dump. Safety-critical default (Enabled false) confirmed present in both files - does not ship enabled.

### Diff scope (existing production code paths)
- git diff Program.cs: purely additive 3-line insertion (new using, AddBackupConfig call, conditional AddHostedService) plus two pre-existing lines had trailing whitespace stripped (cosmetic, zero behavioral change).
- git diff StartupExtensions.cs: purely additive - new using directives and one new AddBackupConfig method appended at the end of the class; zero lines changed inside any pre-existing method.
- git diff appsettings files: purely additive Backup section, no existing keys touched.
- No existing controller, existing service, existing repository, or any other pre-existing production file outside these three was touched by this unit.

## Scope / leakage check

git status --short at repo root shows, for the Backup-related surface:
- Modified: Club12-Backend/API/Program.cs, Club12-Backend/API/Utils/StartupExtensions.cs, Club12-Backend/API/appsettings.json, Club12-Backend/API/appsettings.Development.json, openspec/changes/scheduled-database-backups/tasks.md (checkbox updates).
- New: Club12-Backend/API/BackgroundServices/ (hosted service), Club12-Backend/Infrastructure/Backup/LocalDirectoryBackupStorage.cs, Club12-Backend/API.Tests/Backup/DatabaseBackupHostedServiceTests.cs, Club12-Backend/API.Tests/Backup/LocalDirectoryBackupStorageTests.cs, Club12-Backend/API.Tests/Backup/Fakes (CapturingLogger, FakeBackupStorage, FakeDatabaseBackupService, TestTiming).
- No SupabaseBackupStorage.cs or any Supabase-Backup-named file exists anywhere in Club12-Backend (confirmed by find) - unit 3 scope genuinely untouched.
- Club12-Backend/Infrastructure/Backup/ contains exactly 3 files: LocalDirectoryBackupStorage.cs (unit 2), PgDumpBackupService.cs and ProcessRunner.cs (unit 1, unchanged) - no unit-3 leakage.
- Working tree also shows unrelated concurrent-change files (Club12-WebClient/src/views/team/TeamsPage.tsx, openspec/changes/refactor-teamspage-decomposition tasks.md and verify-report.md) - these belong to the separate refactor-teamspage-decomposition change per the orchestrator's explicit scope note and are correctly excluded from this unit's evaluation, not flagged as leakage.

## Task completeness (tasks.md)

All unit-2 tasks (Phase 4 and Phase 5, 8 items) are checked complete on disk and match the code state verified above. Phase 6-7 (unit 3, Supabase adapter) remain unchecked, correctly reflecting they are out of scope for this unit.

## Non-blocking findings

- WARNING: AddBackupConfig always registers DatabaseBackupHostedService as a DI singleton even when Backup:Enabled=false (only the AddHostedService call is gated). This is harmless - the singleton is inert until something starts it, and nothing in the current wiring does when disabled - but it does mean a future accidental direct StartAsync call elsewhere in the codebase would bypass the Program.cs-level gate. The service's own internal enabled check in ExecuteAsync is the actual safety net for that scenario, and it is real and effective - this is a defense-in-depth observation, not a live bug.
- SUGGESTION: IntervalOverride is public (not internal) solely because the codebase has no InternalsVisibleTo wiring between API and API.Tests, per the documented deviation. This is a minor test-seam leak into the production type's public surface; low priority, already documented and justified in apply-progress.

## Process note

Per the orchestrator's explicit instruction, the native gentle-ai sdd-attempt/sdd-verify-validate runtime ledger was not used for this pass (consistent with all prior changes shipped this session, including unit 1 - see verify-report-unit1-backup-foundations.md's process note). This report is persisted manually via direct file write and mem_save, with independently re-run build/test commands and direct source reads as the evidence base, not trust in the apply report's claims.
