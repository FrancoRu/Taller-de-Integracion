```yaml
schema: gentle-ai.verify-result/v1
evidence_revision: sha256:cfa5452e0c40a18bdef7d07d512942d3fa6eb9f68385dcb32e7a7bc8992c6f8c
verdict: fail
blockers: 5
critical_findings: 5
requirements: 14/18
scenarios: 23/28
test_command: dotnet test Club12-Backend/Solution/Club12.sln --logger "console;verbosity=minimal" && npm run test --prefix Club12-WebClient -- --run
test_exit_code: 0
test_output_hash: sha256:cfa5452e0c40a18bdef7d07d512942d3fa6eb9f68385dcb32e7a7bc8992c6f8c
build_command: dotnet build Club12-Backend/Solution/Club12.sln
build_exit_code: 0
build_output_hash: sha256:432a2a7bbb7cd90e0c9aa5a950ea7e3cdd350d545208e3b6d3ae793be2360467
```

## Verification Report

**Change**: medical-records-storage-eligibility
**Version**: N/A (new capabilities, no prior spec)
**Mode**: Strict TDD

### Completeness
| Metric | Value |
|--------|-------|
| Tasks total | 65 |
| Tasks complete | 64 |
| Tasks incomplete | 1 (15.1 - manual dev-DB verification, explicitly deferred, needs real Supabase credentials; not a CI-automatable task) |

### Build and Tests Execution

**Build**: Passed (independently re-run, not just accepted from apply-progress)
```text
dotnet build Club12-Backend/Solution/Club12.sln
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**Tests**: Passed (independently re-run)
```text
dotnet test Club12-Backend/Solution/Club12.sln --logger "console;verbosity=minimal"
Passed! - Failed: 0, Passed: 722, Skipped: 0, Total: 722, Duration: 9 s - API.Tests.dll (net8.0)

npm run test --prefix Club12-WebClient -- --run
Test Files  105 passed (105)
     Tests  482 passed (482)
```
These numbers match the apply agent self-report exactly (722/0 backend, 482/0 frontend). Baseline deltas (695 to 722 backend, 477 to 482 frontend) were not independently re-verified against a pre-change checkout, but are consistent with new test methods added across MedicalRecordStorageTests.cs (new, 7 facts), MedicalRecordSeedTests.cs (new, 6 facts), MedicalRecordHabilitacionTests.cs (new, 11 facts/theories), 5 new facts in MedicalRecordEligibilityTests.cs, and 1 new fact in SampleTournamentBuilderCategoryTests.cs.

Also independently re-run: npx tsc --noEmit (via local node_modules tsc binary) - exit 0. npm run lint (eslint . --report-unused-disable-directives --max-warnings 0) - exit 0, no output.

**Coverage**: Not available - no coverage tool configured for either suite. Not treated as a failure per Strict TDD rules.

### Spec Compliance Matrix

#### medical-record-storage
| Requirement | Scenario | Test | Result |
|---|---|---|---|
| Private Medical-Records Bucket | Upload lands in private bucket, nothing in public-images | MedicalRecordStorageTests.StoreAsync_NoConfiguredBucket_FallsBackToDefaultMedicalRecordsBucket / StoreAsync_ConfiguredBucket_IsPassedExplicitly | COMPLIANT |
| Object Key Scheme | Key format teamId/playerId/guid.pdf | StoreAsync_KeyShape_IsTeamPlayerGuidExtension_NoLegacyPrefix, StoreAsync_DifferentTeamAndPlayer_TeamIdIsAlwaysFirstSegment | COMPLIANT |
| Bucket-Parameterized Raw Storage Boundary | Default bucket preserved for existing callers | BackupStorage_StillPassesNullBucket_DefaultBucketPreserved | COMPLIANT |
| Bucket-Parameterized Raw Storage Boundary | Medical storage overrides the bucket | StoreAsync_ConfiguredBucket_IsPassedExplicitly, DownloadAsync_TargetsTheConfiguredMedicalBucket | COMPLIANT |
| Authenticated Streaming Is the Only Read Path | Owner downloads via streaming endpoint | MedicalRecordDownloadTests.Download_WithStoredFile_StreamsPdfWithOriginalName | COMPLIANT |
| Authenticated Streaming Is the Only Read Path | No public or signed URL is emitted | none found | UNTESTED (CRITICAL) |
| Authenticated Streaming Is the Only Read Path | Unauthorized caller rejected (401/403) | none found | UNTESTED (CRITICAL) |
| Upload Resets Status and Preserves Reupload Guard | Upload forces Pending / Reupload while Approved blocked | MedicalRecordEligibilityTests.RecordUpload_StoresReference_ButStaysPending, RecordUpload_AfterApproval_IsRejected (both updated for new-scheme refs, unchanged assertions) | COMPLIANT |

#### medical-record-eligibility
| Requirement | Scenario | Test | Result |
|---|---|---|---|
| File-Backed Habilitacion Rule | Approved with a stored file / Approved without a stored file | MedicalRecordHabilitacionTests.Registration_ApprovedWithStoredFile_IsHabilitado, Registration_ApprovedWithNullReference_IsNotHabilitado, Registration_ApprovedWithLegacyReference_IsNotHabilitado | COMPLIANT |
| Rule Applies at Every Read Surface | Public player DTO / season roster load | MedicalRecordHabilitacionTests.FromRegistration tests, MedicalRecordEligibilityTests.GetTeamByIdAsync_ApprovedPlayerWithLegacyReference_ReadsAsNotHabilitado | COMPLIANT |
| Match-Sheet Gate Rejects Approved-Without-File | Approved-without-file cannot be added to a match sheet | MedicalRecordEligibilityTests.LoadTeamMatchSheetAsync_ApprovedWithNoStoredFile_IsRejected | COMPLIANT |
| Approve-Time Write Guard | Approve without a file rejected / Reject without a file allowed | Approve_WithNoStoredFile_IsRejected_AndRowStaysPending, Approve_WithLegacyReference_IsRejected, Reject_WithNoStoredFile_IsStillAllowed | COMPLIANT |
| Effective Immediately, No Data Migration | Legacy approved row with no file after deploy | Same truth-table tests; no migration code exists anywhere in the diff (confirmed: 0 EF migrations added) | COMPLIANT |
| Frontend Approve Action Disabled Without a File | Aprobar disabled/enabled | PlayerMedicalRecordDialog.test.tsx: disables Aprobar with a tooltip test, disables Aprobar for legacy ref test, approves the record once a file is stored test | COMPLIANT |

#### medical-record-seed-backfill
| Requirement | Scenario | Test | Result |
|---|---|---|---|
| Configurable Seed PDF Path | Default path used when key unset | none found - all MedicalRecordSeedTests pass an explicit temp-file path, none pass medicalRecordPath: null | UNTESTED (CRITICAL) |
| Backfill Target Selection | Approved row without new-scheme file backfilled | MedicalRecordSeedTests.SeedMedicalRecords_NullReference_IsUploaded, SeedMedicalRecords_LegacyReference_IsUploaded | COMPLIANT |
| Backfill Target Selection | Non-approved rows skipped | none found | UNTESTED (CRITICAL) |
| Idempotent | Second run is a no-op | SeedMedicalRecords_SecondRun_UploadsZero | COMPLIANT |
| Failure-Tolerant | One upload fails, seed continues | SeedMedicalRecords_UploadThrows_DoesNotFailTheSeed_AndLeavesRefNull | COMPLIANT |
| Whole-Step Skip Guard | Missing PDF file | SeedMedicalRecords_MissingPdfPath_WarnsAndSkips_ZeroUploads | COMPLIANT |
| Seed:MedicalRecords Bypass Flag | Runs during a normal reset seed | none found | UNTESTED (CRITICAL) |
| Seed:MedicalRecords Bypass Flag | Standalone backfill on a seeded database | All MedicalRecordSeedTests (exercised via forceMedicalRecords: true) | COMPLIANT |
| Sample Builder Stops Assigning Fake File References | Seeded approved row has no file before backfill | SampleTournamentBuilderCategoryTests.Build_ApprovedRegistrations_HaveNullMedicalRecordFileUrl | COMPLIANT |

**Compliance summary**: 23/28 scenarios compliant, 5 UNTESTED (no CRITICAL functional/behavioral regression found in any implemented code path - all 5 gaps are missing tests, not observed defects). 14/18 requirements fully compliant.

### Correctness (Static Evidence)
| Requirement | Status | Notes |
|---|---|---|
| ISupabaseRawStorage bucket parameterization | Implemented | Trailing optional string bucket = null on all 4 methods, exactly per design ADR 1; SupabaseHelper resolves bucket ?? _bucketName in all 4 method bodies |
| SupabaseMedicalRecordStorage ctor/config | Implemented | Matches design full code block verbatim (ctor signature, DefaultBucketName, config resolution) |
| IMedicalRecordStorage.StoreAsync param rename | Implemented | tournamentId to teamId across interface, impl, controller (request.TeamId), and all test fakes |
| PlayerTeamRegistration.IsStoredReference / IsHabilitado | Implemented | Verbatim match to design Domain code block, including LegacyReferencePrefix |
| Player.HasMedicalRecordFile + IsHabilitado | Implemented | Transient bool carrier, not the file path, matches ADR 5 disclosure-surface rationale; PublicPlayerResponse confirmed to have zero ForMember override for IsHabilitado (grep across Application/Utils/Mappers returned no hits), so AutoMapper convention correctly propagates the new rule with no DTO code change, exactly as ADR 5 claims |
| TeamService.AttachSeasonRostersAsync | Implemented | One added line populating HasMedicalRecordFile |
| MedicalRecordService.ReviewAsync write guard | Implemented | Guard placed before mutation, exact Spanish message, InvalidOperationException maps to 409 (unchanged mapping) |
| PlayerStatisticService match-sheet gate | Implemented | !registration.IsHabilitado replaces the old != Approved check |
| DataSeeder.SeedMedicalRecordsAsync | Implemented | Batch size 50, fresh MemoryStream per upload (ADR 7), runs after SaveChangesAsync (ADR 6), per-row try/catch, superset EF-translatable filter plus authoritative IsStoredReference re-check |
| Seed:MedicalRecords bypass semantics | Implemented | Bypass branch precedes the skip-if-teams-exist short-circuit, exactly per ADR 8 |
| SampleTournamentBuilder fake-ref removal | Implemented | SampleMedicalRecordFileUrl const deleted, MedicalRecordFileUrl = null unconditionally |
| Frontend Aprobar gate | Implemented | isStoredMedicalRecordFile, LEGACY_MEDICAL_RECORD_PREFIX, MUI Tooltip plus span wrap, Spanish copy matches design exactly |
| appsettings config key (optional) | Implemented | SupaBase:MedicalRecordsBucketName present in gitignored appsettings.Franco.json |

### Coherence (Design)
| Decision | Followed? | Notes |
|---|---|---|
| ADR 1 (optional param, not overloads/new class) | Yes | |
| ADR 2 (ctor takes IConfiguration, in-code default) | Yes | |
| ADR 3 (drop medical-records/ prefix from the key) | Yes | |
| ADR 4 (IsStoredReference single predicate, prefix-aware) | Yes | Reused identically by read sites, write guard, and seed |
| ADR 5 (Player carries a bool, not the path) | Yes | |
| ADR 6 (seed step runs after SaveChangesAsync) | Yes | |
| ADR 7 (buffer PDF once, fresh MemoryStream per upload) | Yes | |
| ADR 8 (Seed:MedicalRecords is a bypass, not an extra gate) | Yes | |
| ADR 9 (ship as chained PRs P1 to P2 to P3) | No | See WARNING findings below - delivered as a single combined diff instead |

### TDD Compliance
| Check | Result | Details |
|-------|--------|---------|
| TDD Evidence reported | Yes | tasks.md documents RED/GREEN phase structure per part, with explicit deviation notes on tasks 8.6 and 9.1/9.3. No separate apply-progress artifact was retrievable in this session (no MCP/Engram tool exposed, no file at the change root) - see Issues. |
| All tasks have tests | Yes | 64/65 checked tasks map to a test file or a verification step; 15.1 is explicitly manual |
| RED confirmed (tests exist) | Yes | All referenced test files confirmed present on disk: MedicalRecordStorageTests.cs, MedicalRecordSeedTests.cs, MedicalRecordHabilitacionTests.cs, extended MedicalRecordEligibilityTests.cs, MatchResultAndSheetTests.cs, SampleTournamentBuilderCategoryTests.cs, PlayerMedicalRecordDialog.test.tsx, medicalRecordDisplay.test.ts |
| GREEN confirmed (tests pass) | Yes | 722/722 backend, 482/482 frontend, independently re-run |
| Triangulation adequate | Yes | IsStoredReference/IsHabilitado truth tables cover null/whitespace/legacy/new-scheme times Approved/Pending; seed decision logic covers null/legacy/new-scheme/second-run/throw/missing-file |
| Safety Net for modified files | Yes | Full backend and frontend suites re-run green after all modifications |

**TDD Compliance**: 6/6 checks passed

### Test Layer Distribution
| Layer | Tests | Files | Tools |
|-------|-------|-------|-------|
| Unit | ~22 | MedicalRecordStorageTests.cs, MedicalRecordHabilitacionTests.cs, medicalRecordDisplay.test.ts | xUnit, Vitest |
| Integration | ~14 | MedicalRecordSeedTests.cs (SQLite/WebApplicationFactory), MedicalRecordEligibilityTests.cs plus SampleTournamentBuilderCategoryTests, MatchResultAndSheetTests, PlayerMedicalRecordDialog.test.tsx (RTL) | xUnit plus CustomWebApplicationFactory, Testing Library |
| E2E | 0 | none | Not installed |
| Total | ~36 new/modified test methods | 11 files | |

### Assertion Quality
No tautologies, ghost loops, or assertion-without-production-code-call patterns found in the new/modified test files. MedicalRecordSeedTests explicitly documents why its assertions are scoped per-tuple rather than global (storage.StoredCalls per-test, not a shared counter) - this is a deliberate, sound response to IClassFixture shared-DB state, not a trivial assertion. The frontend tooltip test correctly hovers the MUI span wrapper (not the disabled button) and asserts the actual rendered Spanish text via screen.findByText, not just element presence.

**Assertion quality**: All assertions verify real behavior

### Quality Metrics
**Linter**: No errors (eslint . --report-unused-disable-directives --max-warnings 0, exit 0)
**Type Checker**: No errors (tsc --noEmit exit 0; dotnet build 0 warnings/0 errors)

### Issues Found

**CRITICAL** (per the strict rule: a required spec scenario with no passing covering test is CRITICAL/UNTESTED, regardless of how low-risk or pre-existing the gap appears):
1. medical-record-storage / Authenticated Streaming Is the Only Read Path - No public or signed URL is emitted: no automated test asserts this. Static evidence only (zero SignedUrl/CreateSignedUrl call sites anywhere in the backend). This behavior is unmodified by this change.
2. medical-record-storage / Authenticated Streaming Is the Only Read Path - Unauthorized caller rejected: no HTTP-pipeline test exercises the AdminOrOwner authorize attribute on the medical-record endpoints. The attribute itself is unchanged by this diff; design.md own Verification gap section and the proposal Non-Goals (Stubbing Supabase in CustomWebApplicationFactory, pre-existing test gap, still open) explicitly acknowledge this class of gap predates the change.
3. medical-record-seed-backfill / Configurable Seed PDF Path - Default path used when key unset: no test passes medicalRecordPath: null to exercise the DefaultMedicalRecordPath fallback; every MedicalRecordSeedTests case passes an explicit temp-file path.
4. medical-record-seed-backfill / Backfill Target Selection - Non-approved rows are skipped: no test seeds a Pending or Rejected registration and asserts it is left untouched by the seed step. Correctness currently relies on reading the WHERE MedicalRecordStatus == Approved SQL filter, not on an executed assertion.
5. medical-record-seed-backfill / Seed:MedicalRecords Bypass Flag - Runs during a normal reset seed: no automated test drives SeedAsync(reset: true, ...) through the full SampleTournamentBuilder reseed with SeedMedicalRecordsAsync wired in afterward. Only the standalone-backfill bypass path is exercised. This matches task 15.1 being explicitly left unchecked (manual dev-DB verification, needs real Supabase credentials) - the gap is disclosed in tasks.md, not hidden - but the actual full-reset production code path still has no automated end-to-end check.

None of these 5 findings correspond to an observed defect, regression, or incorrect implementation - every piece of production code involved was read and matches design.md verbatim (see Correctness table). They are strictly missing-test findings, surfaced because the strict-TDD verification rule treats an untested required scenario as a blocker independent of implementation confidence.

**WARNING**:
1. Delivery diverged from the tasks.md/design.md forecast. tasks.md Review Workload Forecast explicitly states Chained PRs recommended: Yes, 400-line budget risk: High, and a P1 to P2 to P3 chain (tracker plus 2 child PRs); design.md ADR 9 and Review Budget section make the same call (about 925 authored lines, roughly double the 800-line budget named for this change). The orchestrator/apply context for this verify run states single-PR delivery, all 3 internal parts implemented in P1 to P2 to P3 order - i.e. the change was NOT split into chained PRs as its own planning artifacts called for. No size:exception acceptance or superseding delivery-strategy decision is visible in the artifacts available to this verify session. This is a process/reviewability risk, not a functional defect - the diff is about 1340 raw lines across 26 modified files (647 insertions, 88 deletions) plus about 605 lines across 3 new test files, a large single review unit against the stated 400-line default budget.
2. This verify session could not retrieve a separate apply-progress artifact. No apply-progress.md or similar file exists under the change root, and no Engram/mem tool was exposed to this execution context to search for one. Verification was performed instead by independently re-running the full backend and frontend test/build/lint/typecheck commands from a clean invocation, and by diffing every changed/new file against design.md code blocks and tasks.md task descriptions line by line. This is a more direct form of evidence than trusting a self-reported progress artifact, but it means the TDD Cycle Evidence table was reconstructed from tasks.md inline deviation notes and git diff rather than read verbatim from a dedicated apply-progress document.

**SUGGESTION**:
1. MedicalRecordHabilitacionTests.cs was created as a new file for the Phase 4 domain truth-table tests; design.md File Changes table did not name this specific file (it only listed MedicalRecordEligibilityTests.cs as modified for Part 2 tests). This is a benign, additive naming choice - the tests correctly cover their required scenarios - not a functional gap.

### Verdict
**FAIL** (on strict spec-scenario test-coverage grounds only; no functional defect, regression, or design deviation was found)

722/722 backend tests, 482/482 frontend tests, 0 build warnings, 0 lint errors, 0 type errors - all independently reproduced from a clean re-run, matching the apply agent self-report exactly. Every file in the diff was checked line by line against design.md prescribed code blocks and matches verbatim (Domain predicate, storage boundary, seed step, frontend gate), and 23 of 28 spec scenarios have a passing covering test. The FAIL verdict is driven entirely by 5 required scenarios with no automated covering test (3 of which are pre-existing/disclosed gaps predating this change, and 1 of which - the full-reset-seed integration path - is explicitly deferred to the documented manual dev-DB procedure in task 15.1). Recommended next step: either add the 5 missing tests (all low-effort - a null-path seed test, a Pending/Rejected seed-skip test, a reset-path integration test, and, for the 2 pre-existing Supabase-authorization gaps, accept them as an explicitly scoped, already-disclosed limitation before archiving) or have the human/orchestrator explicitly accept this as a known-risk exception before proceeding to sdd-archive.
