# Verification Report: scheduled-database-backups - unit 3 (backup-storage)

**Change**: scheduled-database-backups
**Unit**: 3 of 3 (backup-storage) - FINAL unit; feature-complete verification gate
**Mode**: Strict TDD
**Verifier**: sdd-verify (independent re-execution, not delegated to sdd-apply's own report)
**Note**: Per orchestrator instruction, this pass does not use gentle-ai sdd-attempt/sdd-verify-validate native ledger tooling (unused for all 8 prior changes in this session). Report persisted directly via Bash write + Engram mem_save.

## Completeness

| Metric | Value |
|--------|-------|
| Tasks total (tasks.md) | 21 |
| Tasks complete | 21 |
| Tasks incomplete | 0 |
| Unit-3 scope (Phase 6-7) | 6.1, 7.1, 7.2, 7.3 - all [x], verified against actual code (not just checkbox) |

## Build & Tests Execution (independently re-run, not trusted from apply-progress)

**Build**: PASSED
```
$ dotnet build Club12-Backend/Solution/Club12.sln
Build succeeded.
    0 Warning(s)   <- incremental build, warnings suppressed because no recompilation occurred
    0 Error(s)

$ dotnet build Club12-Backend/Solution/Club12.sln --no-incremental
...
    439 Warning(s)
    0 Error(s)
```

**Tests - full suite**: PASSED
```
$ dotnet test Club12-Backend/Solution/Club12.sln
Passed!  - Failed: 0, Passed: 64, Skipped: 0, Total: 64, Duration: 1 s - API.Tests.dll (net8.0)
```
Real 64/64 pass count confirmed - matches apply-progress claim exactly.

**Tests - unit-3 focused filter**: PASSED
```
$ dotnet test Club12-Backend/Solution/Club12.sln --filter "FullyQualifiedName~SupabaseBackupStorage|FullyQualifiedName~AddBackupConfigStorageSelection"
Passed!  - Failed: 0, Passed: 18, Skipped: 0, Total: 18, Duration: 21 ms
```
18/18 matches claim (11 SupabaseBackupStorageTests + 7 AddBackupConfigStorageSelectionTests).

**Coverage**: Not available - no coverage tool detected in this project (informational only, not a failure per skill rules).

## Item-by-Item Verification (task instructions 1-8)

### 1. Build/test re-run
Done above. Build 0 errors. Tests 64/64 (full) and 18/18 (focused). Confirms apply-progress's headline numbers.

### 2. SupabaseHelper.cs diff - additive-only claim
git diff HEAD for SupabaseHelper.cs inspected in full (73 insertions / 1 deletion by numstat). The single deletion is the class-declaration line (public class SupabaseHelper -> public class SupabaseHelper : ISupabaseRawStorage), which is a strict superset change, not a behavior change. UploadImageAsync<T> (lines 49-70) and DeleteImageAsync<T> (lines 80-90) bodies show zero minus-lines in the diff - confirmed byte-for-byte unchanged. Three new methods (UploadRawAsync, ListRawAsync, RemoveRawAsync, lines 103-160) are inserted between the existing methods, purely additive. Claim CONFIRMED.

### 3. SupabaseBackupStorage.cs - prefix confinement and exception wrapping
Read in full. ToObjectPath(name):
- Rejects null/whitespace names.
- Rejects Path.IsPathRooted(name) (catches C:\evil, /etc/evil, and C:relative - .NET's IsPathRooted treats X: prefixes as rooted even without a separator).
- Splits on / (after normalizing backslash to /) and rejects any segment equal to .. or . - catches leading, trailing, and embedded traversal (e.g. nested/../../escape.sql, which is exactly what the test theory exercises).
- Only after passing validation does it prepend the backups/ prefix and forward to rawStorage.
Traced all 3 public methods (StoreAsync, ListAsync, DeleteAsync): StoreAsync/DeleteAsync call ToObjectPath before any raw call - a rejected name never reaches ISupabaseRawStorage. ListAsync always queries with the hardcoded Prefix constant, never a caller-supplied path - no escape vector there either. No caller-reachable path exists to touch storage outside backups/.
All raw-storage exceptions (from UploadRawAsync/ListRawAsync/RemoveRawAsync) are caught and re-thrown as BackupExecutionException, cross-checked against PgDumpBackupService.cs (throws BackupExecutionException at lines 32 and 64) and DatabaseBackupHostedService.cs (explicit catch of BackupExecutionException at line 123, with generic catch Exception as defense-in-depth below it). Contract is consistent. Claim CONFIRMED.

### 4. ISupabaseRawStorage.cs - seam justification
Read SupabaseHelper's actual constructor (lines 25-40): it builds a real Supabase Client and calls _client.InitializeAsync().Wait() at line 39 - synchronous-blocking real network I/O. A real SupabaseHelper genuinely cannot be constructed in a unit test without either live Supabase credentials/network or a hang/exception. Claim independently verified against actual constructor code, not trusted. CONFIRMED - the seam is necessary, not decorative.

### 5. SupabaseStorageEntry shape vs. installed Supabase.Storage package
Club12-Backend/Application/Application.csproj pins Supabase.Storage Version 2.4.1. Confirmed 2.4.1 is the installed package in the local NuGet cache. Loaded the actual installed Supabase.Storage.dll via .NET reflection (PowerShell Assembly.LoadFrom + GetProperties/GetMethods, not documentation or assumption):
- FileObject.Name : System.String - matches SupabaseHelper.ListRawAsync's file.Name usage.
- FileObject.UpdatedAt : System.Nullable of DateTime - matches the file.UpdatedAt.HasValue / .Value handling and the DateTime? to DateTimeOffset? conversion in ListRawAsync.
- StorageFileApi.List(string path = null, SearchOptions options = null) - both optional - confirms List(prefix) single-arg call site compiles.
- StorageFileApi.Upload(byte[] data, string supabasePath, FileOptions options = null, ...) - matches UploadRawAsync's Upload call shape (byte[] overload).
- StorageFileApi.Remove(string path) - matches RemoveRawAsync.
Claim independently re-verified via reflection against the real installed DLL - accurate. CONFIRMED.

### 6. AddBackupConfig branching - regression check
Read the full method (StartupExtensions.cs, AddBackupConfig). git diff shows the previous unconditional Log.Warning fallback-and-ignore block replaced with a proper if/else. Critically, the local-storage registration moved from unconditional fall-through into the else branch - before this change there was a latent double-registration risk (Supabase branch logged a warning but the Local registration ran unconditionally afterward anyway). Now:
- StorageTarget=Supabase (case-insensitive via StringComparison.OrdinalIgnoreCase, confirmed) registers ONLY ISupabaseRawStorage to SupabaseHelper singleton plus IBackupStorage to SupabaseBackupStorage (type-based).
- Any other value (including default Local, empty string, arbitrary string) registers ONLY the unit-2 LocalDirectoryBackupStorage factory registration, unchanged from unit 2's original implementation.
SupabaseHelper is confirmed already registered as itself elsewhere in StartupExtensions.cs (services.AddSingleton of SupabaseHelper at line 266), so sp.GetRequiredService of SupabaseHelper resolves the same existing singleton - no second Supabase client is spun up, matching the design decision. appsettings.json/appsettings.Development.json both default StorageTarget to Local - the safe, currently-active default. No regression found; local-storage default path is intact. Claim CONFIRMED.

### 7. git status --short - leakage check
```
 M Club12-Backend/API/Utils/StartupExtensions.cs
 M Club12-Backend/Application/Utils/Helper/SupabaseHelper/SupabaseHelper.cs
 M openspec/changes/scheduled-database-backups/tasks.md
?? .codegraph/.gitignore
?? Club12-Backend/API.Tests/Backup/AddBackupConfigStorageSelectionTests.cs
?? Club12-Backend/API.Tests/Backup/Fakes/FakeSupabaseRawStorage.cs
?? Club12-Backend/API.Tests/Backup/SupabaseBackupStorageTests.cs
?? Club12-Backend/Application/Utils/Helper/SupabaseHelper/ISupabaseRawStorage.cs
?? Club12-Backend/Infrastructure/Backup/SupabaseBackupStorage.cs
```
Exactly 3 modified + 5 new files, all matching apply-progress's declared unit-3 file list. The only extra untracked entry is .codegraph/.gitignore (local CodeGraph tooling artifact from this verification session, unrelated to the feature, not a repo source file). No leakage into units 1/2 files or unrelated areas. CONFIRMED.

### 8. tasks.md checkbox reality check
All 21 checkboxes in tasks.md are checked. Cross-referenced Phase 6-7 (unit-3 scope) against actual files: 6.1 maps to SupabaseBackupStorageTests.cs plus AddBackupConfigStorageSelectionTests.cs which exist and pass; 7.1 maps to SupabaseHelper.cs's 3 additive methods which exist and are exercised by tests; 7.2 maps to SupabaseBackupStorage.cs which exists and implements IBackupStorage; 7.3 is verified in item 6 above. Checkboxes match reality. CONFIRMED.

## Spec Compliance Matrix

The spec's 4 requirements / 9 scenarios (Interval-Based Backup Trigger x2, Backup Enabled Gate x2, Keep-Last-N Retention Pruning x3, Backup Failure Isolation x2) are unit-1/2 scoped and were verified in prior units' reports; re-confirmed still green here since the full 64/64 suite (which includes all unit-1/2 tests) passes unmodified. The spec's explicit Non-Goal - actual upload to the Supabase storage bucket, including credentials/network behavior is NOT covered by automated tests - is respected by this unit: SupabaseBackupStorageTests.cs and AddBackupConfigStorageSelectionTests.cs test the confinement/wrapping/selection logic against a fake, with zero real network calls or BuildServiceProvider resolution, exactly matching the spec's stated automated-test boundary. Real Supabase upload/list/remove remains a documented manual/staging verification item, consistent with the spec's Non-Goals section - not a gap introduced by this unit.

| Requirement | Scenario | Test | Result |
|-------------|----------|------|--------|
| Interval-Based Backup Trigger | elapses / not elapsed | DatabaseBackupHostedServiceTests | COMPLIANT (unit 2, re-confirmed passing) |
| Backup Enabled Gate | disabled no-op / enabled schedules | DatabaseBackupHostedServiceTests | COMPLIANT (unit 2, re-confirmed passing) |
| Keep-Last-N Retention Pruning | within/exceeds/ties | KeepLastNRetentionPolicyTests | COMPLIANT (unit 1, re-confirmed passing) |
| Backup Failure Isolation | dump fails / binary missing | PgDumpBackupServiceTests | COMPLIANT (unit 1, re-confirmed passing) |
| (Non-Goal, N/A to automated tests) | real Supabase network I/O | manual/staging only | Correctly out of scope, not claimed as tested |

**Compliance summary**: 9/9 in-scope scenarios compliant (0 regressions from unit 3's changes).

## Correctness (Static Evidence)

| Requirement/Claim | Status | Notes |
|------------|--------|-------|
| SupabaseHelper additive-only | Confirmed | Diff-verified, 0 lines changed in existing method bodies |
| backups/ prefix confinement, no escape | Confirmed | Traced all 3 public methods; traversal/rooted-path rejection verified logically and by passing tests |
| BackupExecutionException wrapping consistency | Confirmed | Matches PgDumpBackupService/DatabaseBackupHostedService's existing catch contract |
| ISupabaseRawStorage seam necessity | Confirmed | Constructor genuinely blocks on real network I/O |
| SupabaseStorageEntry shape accuracy | Confirmed | Verified via reflection against installed Supabase.Storage.dll 2.4.1, not assumed |
| AddBackupConfig Supabase/Local branching | Confirmed | Case-insensitive Supabase branch verified; Local default path unaffected (regression check passed) |
| Task/code alignment | Confirmed | All 21 tasks map to real, working code |

## Coherence (Design)

| Decision | Followed? | Notes |
|----------|-----------|-------|
| Reuse existing SupabaseHelper singleton, no second client | Yes | sp.GetRequiredService of SupabaseHelper reuses the singleton registered at line 266 |
| SupabaseBackupStorage under backups/ prefix | Yes | ToObjectPath enforces this |
| Existing UploadImageAsync/DeleteImageAsync behavior-frozen | Yes | Byte-for-byte unchanged per diff |
| ISupabaseRawStorage not in design.md's literal file list | Documented deviation | Design said reuses via new additive raw methods without naming an interface; the interface is the minimal seam needed for testability given the constructor's real I/O - sound, low-risk deviation, does not break any spec requirement |

## Issues Found

**CRITICAL**: None.

**WARNING**:
1. Apply-progress's build-warning claim is inaccurate. apply-progress states 432 pre-existing warnings only, zero warnings on any unit-3 file. An independent dotnet build --no-incremental (forced full recompile, since the default incremental build reports 0 warnings by skipping recompilation of up-to-date projects) shows 439 total warnings, of which 7 are in unit-3-authored files:
   - SupabaseHelper.cs lines 128-131: CS8600/CS8604 (x2) - nullable-reference warnings from mapping Supabase.Storage.FileObject.Name (compiler infers possibly-null) into the non-nullable SupabaseStorageEntry.Name.
   - SupabaseBackupStorage.cs: 3x CS1591 (missing XML doc comments on StoreAsync/ListAsync/DeleteAsync, inconsistent with the file's own XML-documented style elsewhere) plus 1x CS1574 (XML cref of rawStorage on the primary-constructor parameter doesn't resolve - primary-constructor parameters aren't valid cref targets).
   This is not a functional regression - build still succeeds with 0 errors, all 64 tests pass, and the warnings are cosmetic (nullable annotations, missing docs) - but the specific zero-warnings-on-unit-3-files claim in apply-progress does not hold up under a genuinely clean rebuild and should not be repeated as fact in the archived record.

**SUGGESTION**:
1. Add XML doc comments to SupabaseBackupStorage's 3 public methods and fix/remove the unresolved cref of rawStorage to match the codebase's existing documentation convention (and silence CS1591/CS1574).
2. Consider a null-coalescing fallback in SupabaseHelper.ListRawAsync to silence the CS8600/CS8604 nullable warnings, since the 3rd-party SDK's nullable annotations suggest Name could theoretically be null even though it isn't expected to be in practice.

## Verdict

**PASS WITH WARNINGS**

All 21/21 tasks are complete and verified against real code, not just checkboxes. Build succeeds (0 errors), full test suite is genuinely 64/64 passing (independently re-run, not trusted from the apply report), SupabaseHelper.cs's existing methods are confirmed byte-for-byte unchanged, the backups/ prefix confinement has no discoverable escape path, failure wrapping is consistent with the established BackupExecutionException contract, the ISupabaseRawStorage test seam is justified by a real blocking-I/O constructor (verified, not trusted), the SupabaseStorageEntry/FileObject shape claim was independently re-verified via reflection against the actual installed 2.4.1 DLL, the AddBackupConfig Supabase/Local branch regression check passed, and git status shows no leakage beyond the declared unit-3 file set. The sole WARNING is a documentation-accuracy issue in apply-progress's build-warning count (not a code defect) - non-blocking for archive, but should not be repeated as verified fact going forward.

This is unit 3 of 3 - with this PASS, the scheduled-database-backups feature (all 3 units: backup-foundations, backup-hosted-service, backup-storage) is feature-complete and ready for sdd-archive.
