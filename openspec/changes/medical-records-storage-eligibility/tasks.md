# Tasks: Private Medical-Records Bucket + File-Backed Habilitación + Seed Backfill

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | Part 1 ~270 authored; Part 2 ~365 authored; Part 3 ~290 authored; total ~925 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR #1 = Part 1 → PR #2 (base PR #1) = Part 2 → PR #3 (base PR #2) = Part 3 |
| Delivery strategy | ask-on-risk (default; orchestrator must confirm chain vs `size:exception` before apply) |
| Chain strategy | feature-branch-chain |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Part 1 — bucket-parameterized raw storage + medical bucket relocation | PR #1 (base = tracker branch off `develop`) | `dotnet test Club12-Backend/Solution/Club12.sln --filter MedicalRecordStorageTests\|MedicalRecordDownloadTests` | Manual: upload a PDF via `POST /api/medical-records`, confirm object lands in `medical-records` bucket via Supabase dashboard | Revert storage/config commits; no schema/data change; orphaned private objects are harmless |
| 2 | Part 2 — file-backed `IsHabilitado` on all 5 read sites + approve-time write guard + FE gate | PR #2 (base = PR #1 branch) | `dotnet test Club12-Backend/Solution/Club12.sln --filter MedicalRecordEligibilityTests` | N/A — fully covered by xUnit/WebApplicationFactory; no external dependency to exercise manually | Revert domain/application/FE commits; no data written; previously-approved rows immediately read habilitado again |
| 3 | Part 3 — gated, idempotent seed backfill + sample-builder fake-ref removal | PR #3 (base = PR #2 branch) | `dotnet test Club12-Backend/Solution/Club12.sln --filter MedicalRecordSeedTests` | Manual dev-DB procedure (design.md "Manual dev-DB verification procedure") — not CI-automatable, needs a real Supabase bucket | Revert seed/builder commits **and** reseed dev DB with `--Seed:Reset` (sharp edge — see proposal Rollback Plan) |

---

## PART 1 — Storage Relocation (PR #1)

### Phase 1: RED — raw storage boundary & key-shape tests (Req: Bucket-Parameterized Raw Storage Boundary, Object Key Scheme)

- [x] 1.1 Create `API.Tests/MedicalRecordStorageTests.cs`: `StoreAsync` key is `{teamId}/{playerId}/{guid}.pdf`, no `medical-records/` prefix, `teamId` is the first segment.
- [x] 1.2 Same file: upload and download both target the configured medical bucket; fallback is `"medical-records"` when the config key is absent.
- [x] 1.3 Same file: key-shape assertion — the two leading path segments parse as the expected Guids (path-traversal threat-matrix RED).
- [x] 1.4 Extend `API.Tests/Backup/Fakes/FakeSupabaseRawStorage.cs` to 4-arg signatures (`bucket` optional) and record the bucket passed per call.
- [x] 1.5 Extend backup/image regression tests: `SupabaseBackupStorage` and image uploads still pass `bucket: null` (default `public-images`).
- [x] 1.6 `API.Tests/MedicalRecordDownloadTests.cs:39-50,137-164`: update `InMemoryRawStorage`/fake signatures; invert the `:49` prefix assertion to expect `{teamId}/`.

### Phase 2: GREEN — bucket-parameterized boundary (Req: Private Medical-Records Bucket, Object Key Scheme, Bucket-Parameterized Raw Storage Boundary)

- [x] 2.1 `Application/Utils/Helper/SupabaseHelper/ISupabaseRawStorage.cs`: add `string? bucket = null` to all 4 methods + doc comments.
- [x] 2.2 `Infrastructure/Storage/SupabaseHelper.cs:115-192`: `bucket ?? _bucketName` in `UploadRawAsync`, `ListRawAsync`, `RemoveRawAsync`, `DownloadRawAsync`.
- [x] 2.3 `Application/Utils/Constants/Configuration/ConfigurationKeys.cs:53-59`: add `MedicalRecordsBucketName` const.
- [x] 2.4 `Infrastructure/Storage/SupabaseMedicalRecordStorage.cs`: ctor takes `IConfiguration`; `DefaultBucketName = "medical-records"`; resolve `_bucketName` from config; `StoreAsync`/`DownloadAsync` build `{teamId}/{playerId}/{guid}{ext}` and pass the bucket explicitly.
- [x] 2.5 `Application/Interfaces/Storage/IMedicalRecordStorage.cs`: rename `tournamentId` param to `teamId` + doc.
- [x] 2.6 `API/Controllers/MedicalRecordController.cs:69`: pass `request.TeamId` instead of `request.TournamentId`.
- [x] 2.7 (Optional) `API/appsettings.Franco.json`: add `"MedicalRecordsBucketName": "medical-records"` under `SupaBase`.
- [x] 2.8 Verify 1.1–1.6 green; `dotnet build Club12-Backend/Solution/Club12.sln` 0 warnings.

### Phase 3: Part 1 verification

- [x] 3.1 `dotnet test Club12-Backend/Solution/Club12.sln --filter MedicalRecordStorageTests|MedicalRecordDownloadTests`.
- [x] 3.2 Full backend regression: `dotnet test Club12-Backend/Solution/Club12.sln` green.

---

## PART 2 — File-Backed Eligibility (PR #2, base = PR #1 branch)

### Phase 4: Domain RED (Req: File-Backed Habilitación Rule)

- [x] 4.1 Pure xUnit `PlayerTeamRegistration.IsStoredReference`: null / "" / whitespace / `medical-records/x` → false; `{guid}/{guid}/x.pdf` → true.
- [x] 4.2 Pure xUnit truth table for `IsHabilitado` on `PlayerTeamRegistration` and `Player` (Approved+file, Approved+legacy, Approved+null, Pending+file).
- [x] 4.3 Pure xUnit `MedicalRecordResponse.FromRegistration`: Approved + legacy ref → `IsHabilitado` false.

### Phase 5: Domain/Application GREEN (Req: File-Backed Habilitación Rule, Rule Applies at Every Read Surface)

- [x] 5.1 `Domain/Entities/Models/PlayerTeamRegistration.cs`: add `LegacyReferencePrefix` const, `IsStoredReference(string?)`, `[NotMapped] IsHabilitado`.
- [x] 5.2 `Domain/Entities/Models/Player.cs:82-91`: add `[NotMapped] HasMedicalRecordFile`; rewrite `IsHabilitado` body.
- [x] 5.3 `Application/Services/TeamService.cs:267`: `r.Player!.HasMedicalRecordFile = PlayerTeamRegistration.IsStoredReference(r.MedicalRecordFileUrl)`.
- [x] 5.4 `Application/DTOs/MedicalRecord/Response/MedicalRecordResponse.cs:49`: `IsHabilitado = registration.IsHabilitado`.
- [x] 5.5 Verify 4.1–4.3 green.

### Phase 6: Approve-time write guard (Req: Approve-Time Write Guard)

- [x] 6.1 RED `MedicalRecordEligibilityTests.cs`: approve with no stored file → `InvalidOperationException` containing "no hay un archivo cargado"; row stays Pending.
- [x] 6.2 RED same file: approve with a legacy `medical-records/` ref → rejected.
- [x] 6.3 RED same file: reject with no file → still allowed.
- [x] 6.4 GREEN `Application/Utils/Constants/ErrorMessages.cs:333-352`: add `MedicalRecord.NoStoredFile` Spanish constant.
- [x] 6.5 GREEN `Application/Services/MedicalRecordService.cs:54-67`: guard at the top of the approve branch using `IsStoredReference`.
- [x] 6.6 Verify 6.1–6.3 green.

### Phase 7: Read-site integration (Req: Rule Applies at Every Read Surface, Match-Sheet Gate Rejects Approved-Without-File)

- [x] 7.1 RED `PlayerStatisticService` match-sheet test: Approved+null-ref registration → `PlayerNotEligible` (mirror `LoadTeamMatchSheetAsync_NotApprovedPlayer_IsRejected`).
- [x] 7.2 GREEN `Application/Services/PlayerStatisticService.cs:185`: gate condition uses `!registration.IsHabilitado`.
- [x] 7.3 RED `TeamService.AttachSeasonRostersAsync` test: Approved+legacy-ref player reads `isHabilitado false` on roster surfacing.
- [x] 7.4 Verify 7.1 and 7.3 green (5.3 supplies the fix).

### Phase 8: Fix tests broken by the new rule (Req: Effective Immediately, No Data Migration)

- [x] 8.1 `API.Tests/MedicalRecordEligibilityTests.cs:117-133` (`Approve_MakesHabilitado`): insert a `RecordUploadAsync` with a `{teamId}/{playerId}/{guid}.pdf` ref before `ReviewAsync`.
- [x] 8.2 `:176-196` (`LoadTeamMatchSheetAsync_AfterApproval_Succeeds`): same one-line upload before `ReviewAsync`.
- [x] 8.3 `:200-244` (`ApprovalInOneSeason_DoesNotHabilitateAnother`): same upload for season A only; season B stays file-less.
- [x] 8.4 `:84-113` (`RecordUpload_AfterApproval_IsRejected`): replace `"medical-records/some/object/path.pdf"` literals with a `{teamId}/{playerId}/...` shaped ref.
- [x] 8.5 `:53-70` (`RecordUpload_StoresReference_ButStaysPending`): update the literal ref for consistency (no assertion change).
- [x] 8.6 Verify `dotnet test Club12-Backend/Solution/Club12.sln` green. (Also fixed an out-of-scope pre-existing helper, `MatchResultAndSheetTests.SeedRosterPlayerAsync`, which defaulted to Approved+no-file and broke under the new rule — not listed in design.md's "existing tests to fix" table; see Deviations.)

### Phase 9: Frontend approve gate (Req: Frontend Approve Action Disabled Without a File)

- [x] 9.1 RED `Club12-WebClient/src/views/medicalRecord/PlayerMedicalRecordDialog.test.tsx` (already existed — extended, not created): "Aprobar" disabled + tooltip when `fileUrl` null or legacy; enabled for a `{teamId}/…` ref; "Rechazar" never disabled. Pure-function RED also added to `medicalRecordDisplay.test.ts`.
- [x] 9.2 GREEN `src/modules/medicalRecord/utils/medicalRecordDisplay.ts`: add `LEGACY_MEDICAL_RECORD_PREFIX` and `isStoredMedicalRecordFile(fileUrl)`.
- [x] 9.3 GREEN `PlayerMedicalRecordDialog.tsx:311-318`: `disabled={submitting || !canApprove}` with `canApprove = isStoredMedicalRecordFile(record?.fileUrl)`; MUI `Tooltip` (Spanish: "Subí la ficha médica antes de aprobarla."), `<span>`-wrapped. Also fixed the pre-existing "approves the record via the review endpoint" test, which approved with no file and broke under the new gate — see Deviations.
- [x] 9.4 Verify 9.1 green; `npm run test --prefix Club12-WebClient`.

### Phase 10: Part 2 verification

- [x] 10.1 Full backend suite: `dotnet test Club12-Backend/Solution/Club12.sln`.
- [x] 10.2 Full frontend suite: `npm run test --prefix Club12-WebClient`.

---

## PART 3 — Seed Backfill (PR #3, base = PR #2 branch)

### Phase 11: Seed decision-logic RED (Req: Backfill Target Selection, Idempotent, Failure-Tolerant, Whole-Step Skip Guard)

- [x] 11.1 Create `API.Tests/MedicalRecordSeedTests.cs`: skip-vs-upload — null ref → upload, `medical-records/…` ref → upload, `{teamId}/…` ref → skip; second run uploads 0 (SQLite + fake `IMedicalRecordStorage`).
- [x] 11.2 Same file: a throwing fake storage does not fail the seed and leaves the ref null.
- [x] 11.3 Same file: missing PDF path → warn + skip, 0 uploads.
- [x] 11.4 Extend `SampleTournamentBuilder` tests: seeded approved registrations carry `MedicalRecordFileUrl == null` after `Build()` (Req: Sample Builder Stops Assigning Fake File References).

### Phase 12: Sample builder + config wiring GREEN

- [x] 12.1 `Infrastructure/Persistance/SampleTournamentBuilder.cs:47`: delete `SampleMedicalRecordFileUrl`.
- [x] 12.2 `:361`: `MedicalRecordFileUrl = null`; rewrite the `:342-346` comment to note the seed fills the ref.
- [x] 12.3 `API/Utils/ConfigurationKeys.cs:44-61`: add `Seed.MedicalRecordPath = "Seed:MedicalRecordPath"`, `Seed.MedicalRecords = "Seed:MedicalRecords"`.
- [x] 12.4 Verify 11.4 green.

### Phase 13: DataSeeder GREEN (Req: Configurable Seed PDF Path, Seed:MedicalRecords Bypass Flag)

- [x] 13.1 `Infrastructure/Persistance/DataSeeder.cs`: ctor gains `IMedicalRecordStorage`; add `DefaultMedicalRecordPath` const and `MedicalRecordSaveBatchSize = 50`.
- [x] 13.2 `SeedAsync` gains `medicalRecordPath`/`forceMedicalRecords` params; add the bypass branch ahead of the skip-if-teams-exist short-circuit.
- [x] 13.3 Add `SeedMedicalRecordsAsync(string? medicalRecordPath)`: buffer the PDF once, query `Approved` rows with null/empty/legacy ref, per-row try/catch upload with a fresh `MemoryStream`, batch-save every 50 rows, log summary.
- [x] 13.4 Call `SeedMedicalRecordsAsync` after `db.SaveChangesAsync()` in the reset path (store-generated keys, ADR #6).
- [x] 13.5 `API/Utils/StartupExtensions.cs:166-172`: read both config keys and pass them into `dataSeeder.SeedAsync(...)`.
- [x] 13.6 Verify 11.1–11.3 green.

### Phase 14: Part 3 verification

- [x] 14.1 `dotnet test Club12-Backend/Solution/Club12.sln --filter MedicalRecordSeedTests|SampleTournamentBuilder`.
- [x] 14.2 Full backend suite green.

### Phase 15: Manual dev-DB verification (documented, not CI — Req: Effective Immediately, No Data Migration)

- [ ] 15.1 Run the 9-step manual procedure in `design.md` ("Manual dev-DB verification procedure") after deploying P1→P2→P3 in order; record results in the PR description. **Not run in this apply session** — it requires a real Supabase bucket/dev DB and manual `dotnet run` with real credentials; out of scope for an automated agent session. See Deviations/risks in the final report.

---

## Phase 16: Final full-suite regression (all parts merged)

- [x] 16.1 `dotnet test Club12-Backend/Solution/Club12.sln` green, 0 build warnings. (722 tests passing.)
- [x] 16.2 `npm run test --prefix Club12-WebClient` green; `npx tsc --noEmit` exit 0; `npm run lint` exit 0. (482 tests passing.)
