# Proposal: Private Medical-Records Bucket + File-Backed Habilitación + Seed Backfill

**Touches**: Part 1 = backend only. Part 2 = backend (+ optional tiny frontend UX). Part 3 = backend (seed) only. **No EF migration** — no schema change.

## Intent

Three connected problems with medical records (`PlayerTeamRegistration.MedicalRecordStatus` / `MedicalRecordFileUrl`):

1. **Health documents sit in a public bucket.** Every medical PDF is uploaded to `public-images` (`SupaBase:BucketName`), the same bucket serving team logos and blog images. `SupabaseHelper` hardcodes that single bucket on every method. The owner has already created a **private** `medical-records` bucket; nothing points at it.
2. **"Habilitado" can be true with no medical record at all.** `MedicalRecordService.ReviewAsync` approves without checking that a file was ever stored, and all five read sites compute eligibility as `status == Approved` only. A player can be cleared to play a match with zero documentation — the exact thing the feature exists to prevent.
3. **Seeded data is fiction.** `SampleTournamentBuilder:47` stamps every approved row with `medical-records/sample/ficha-medica.pdf`, a path with no object behind it. Download fails, and the dev DB cannot exercise the real flow.

## Scope

### In Scope — Part 1: storage relocation (backend)

- New config key `SupaBase:MedicalRecordsBucketName`, default `"medical-records"` (private bucket, already provisioned manually by the owner).
- `ISupabaseRawStorage` becomes **bucket-parameterized**: optional `bucket` argument / overloads defaulting to the configured `SupaBase:BucketName`, so `SupabaseBackupStorage` and image uploads are byte-for-byte unaffected.
- `SupabaseMedicalRecordStorage` targets the medical bucket and builds key **`{teamId}/{playerId}/{guid}{ext}`** — the `medical-records/` prefix is dropped (the bucket already carries that name) and `tournamentId` is dropped from the path.
- `IMedicalRecordStorage.StoreAsync` parameter `tournamentId` → `teamId`; ripples to `MedicalRecordController`, test fakes, and the `MedicalRecordDownloadTests` path assertion.
- Files keep being served by the existing **authenticated backend streaming endpoint** (service-role download → `File(...)`). No public URL, no signed URL. Frontend unaffected (`fileUrl` is opaque).
- **Data policy: go-forward only + dev reseed.** No object-migration tool. `main` is untouched and real production volume is ≈ 0; existing `public-images` refs (fake seed refs plus any manual uploads) are abandoned and rewritten by the Part 3 seed.

### In Scope — Part 2: file-backed eligibility, effective immediately (backend, optional FE)

- **Read side** — `habilitado` becomes `status == Approved && a real file reference is present`, at all five current sites:
  `Domain/Entities/Models/Player.cs:91` (needs the file ref surfaced onto the transient `Player` alongside the transient `MedicalRecordStatus`), `Application/DTOs/MedicalRecord/Response/MedicalRecordResponse.cs:49`, `Application/Services/PlayerStatisticService.cs:185` (match-sheet enforcement), `Application/Services/TeamService.cs:267` (roster surfacing), `Application/DTOs/Player/Response/PublicPlayerResponse.cs:55` (AutoMapper-projected).
- **Write side** — `MedicalRecordService.ReviewAsync` rejects an approve when `MedicalRecordFileUrl` is null/whitespace, with a Spanish message under `ErrorMessages.MedicalRecord.*`. Keeps the invariant "Approved ⟺ file present" clean going forward.
- **Accepted consequence**: the ~320 seeded "approved with fake ref" players become **not habilitado the moment this ships**, until the Part 3 seed gives them real files.
- 3 existing tests approve without a file and MUST be updated in this change (`API.Tests/MedicalRecordEligibilityTests.cs:117-133, 177-196, 200-244`). Strict TDD.
- Optional/nice-to-have frontend UX only: disable "Aprobar" when there is no stored file (`PlayerMedicalRecordDialog.tsx`).

### In Scope — Part 3: medical-records seed (backend)

- New config key `Seed:MedicalRecordPath` (mirrors `Seed:LogosPath`) with a sensible `DataSeeder` default const, pointing at `ficha-medica-club12.pdf`.
- New step in `DataSeeder.SeedAsync`, piggybacking the existing flow exactly like `UploadTeamLogosAsync`: for every `PlayerTeamRegistration` that should be habilitado and has no real file under the new scheme, upload the PDF via `IMedicalRecordStorage.StoreAsync` and set `MedicalRecordFileUrl` / `MedicalRecordFileName` (~320 uploads).
- **Idempotent, failure-tolerant, resumable**: skip registrations whose ref already resolves in the new bucket scheme; per-upload `try/catch` → warn and continue, **never fail the seed**; if `Seed:MedicalRecordPath` is unset or missing, warn and skip the whole step.
- Extra gate `Seed:MedicalRecords=true` so it can run as a **targeted backfill against an already-seeded DB** without a full `--Seed:Reset` wipe.
- `SampleTournamentBuilder:47,317-365` stops assigning `SampleMedicalRecordFileUrl`; approved rows are left with a null file ref so the seed fills them (and so, pre-seed, they correctly read as not habilitado under Part 2).
- `DataSeeder` gains `IMedicalRecordStorage` in its constructor (it already receives `SupabaseHelper`).

### Out of Scope (Non-Goals)

- Making the `medical-records` bucket public, or introducing signed/public URLs.
- Any object-migration tool for production data (go-forward only, by decision).
- DB `CHECK` constraint for "Approved ⇒ file", and therefore **any EF migration**.
- `RosterCopyService` copying medical fields across seasons (deliberate HU-59 behavior, unchanged).
- Any change to the Pending / Rejected review flow, the 409-on-reupload-when-approved guards, or the `MedicalRecordFileUrl` column name.
- Stubbing Supabase in `CustomWebApplicationFactory` (pre-existing test gap, still open).

## Capabilities

### New Capabilities

- `medical-record-storage`: which bucket medical PDFs live in, the bucket-parameterized raw-storage boundary, the `{teamId}/{playerId}/{guid}{ext}` object key, and authenticated streaming as the only read path.
- `medical-record-eligibility`: habilitación semantics — `Approved` **and** a stored file — across the API read surfaces and the match-sheet gate, plus the approve-time write guard.
- `medical-record-seed-backfill`: the gated, idempotent, failure-tolerant seed step that gives seeded registrations real medical PDFs.

### Modified Capabilities

- None. No existing `openspec/specs/` capability covers medical records; the behavior change is captured by the three new specs above.

## Approach

Exploration **Storage option 1** + **Eligibility A and B together** + **Seed 2 + 3**.

Parameterizing the existing raw boundary (rather than standing up a second `SupabaseHelper`) avoids a second blocking `InitializeAsync().Wait()` in the startup singleton and keeps the backup adapter untouched via a defaulted argument. Eligibility is enforced on **both** sides because the write guard alone cannot fix rows that already carry a non-null fake ref, and the read rule alone would leave the approve action able to mint invalid states. The seed reuses `IMedicalRecordStorage.StoreAsync` — the same boundary the controller uses — so the seed exercises the production path rather than a parallel one.

**Legacy-ref discriminator**: a ref is "old scheme" when it starts with `medical-records/` (or is null); new-scheme refs start with `{teamId}/`. The seed uses this to decide skip-vs-upload.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Application/Utils/Helper/SupabaseHelper/ISupabaseRawStorage.cs` | Modified (P1) | Optional `bucket` argument / overloads |
| `Infrastructure/Storage/SupabaseHelper.cs:115-127` | Modified (P1) | Honor per-call bucket, default configured |
| `Infrastructure/Storage/SupabaseMedicalRecordStorage.cs:27-41` | Modified (P1) | Medical bucket + `{teamId}/{playerId}/{guid}{ext}` |
| `Application/Interfaces/Storage/IMedicalRecordStorage.cs` | Modified (P1) | `tournamentId` → `teamId` |
| `API/Controllers/MedicalRecordController.cs:68-72` | Modified (P1) | Pass `request.TeamId` |
| `Application/Utils/Constants/Configuration/ConfigurationKeys.cs:53-58` + `appsettings*.json` | Modified (P1) | `SupaBase:MedicalRecordsBucketName` |
| `Domain/Entities/Models/Player.cs:82,91` | Modified (P2) | Transient file ref + file-backed `IsHabilitado` |
| `Application/Services/TeamService.cs:267` | Modified (P2) | Surface file ref onto roster players |
| `Application/Services/PlayerStatisticService.cs:185` | Modified (P2) | Match-sheet gate requires a file |
| `Application/DTOs/MedicalRecord/Response/MedicalRecordResponse.cs:49`, `Player/Response/PublicPlayerResponse.cs:55` | Modified (P2) | File-backed `IsHabilitado` |
| `Application/Services/MedicalRecordService.cs:54-67` + `ErrorMessages.cs` | Modified (P2) | Approve guard + Spanish message |
| `Infrastructure/Persistance/DataSeeder.cs` | Modified (P3) | Inject `IMedicalRecordStorage`, new seed step, default path const |
| `Infrastructure/Persistance/SampleTournamentBuilder.cs:47,317-365` | Modified (P3) | Drop fake `SampleMedicalRecordFileUrl` |
| `API/Utils/ConfigurationKeys.cs:44-61` + `StartupExtensions.cs:144-173` | Modified (P3) | `Seed:MedicalRecordPath`, `Seed:MedicalRecords` |
| `API.Tests/MedicalRecordEligibilityTests.cs:117-133,177-196,200-244` | Modified | 3 approve-without-file tests + new guard tests |
| `API.Tests/MedicalRecordDownloadTests.cs:49`, `Backup/Fakes/FakeSupabaseRawStorage.cs` | Modified | Key/signature ripple, fake bucket arg |
| `Club12-WebClient/.../PlayerMedicalRecordDialog.tsx` | Modified (optional) | Disable "Aprobar" with no stored file |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| `medical-records` bucket missing/not private in an environment → every upload+download fails (client never creates buckets) | Med | Owner already created it private; document as a mandatory per-environment manual step with an RLS check (service-role key bypasses RLS, so the private bucket MUST NOT be exposed via anon policies) |
| ~320 sequential network uploads make the seed slow or flaky | High | Idempotent skip + per-upload try/catch + resumable `Seed:MedicalRecords` gate; the seed never fails |
| Every seeded "approved" player is not habilitado between the Part 2 merge and the Part 3 seed run | High | Accepted and documented; run the seed/backfill immediately after deploy in dev |
| Existing `public-images` medical objects become unreachable | Med | Accepted (go-forward only); prod volume ≈ 0, `main` untouched, dev is reseeded |
| The seed and `SupabaseHelper` are not integration-testable through `CustomWebApplicationFactory` (Supabase not stubbed) | High | Pre-existing gap; cover the seed step's decision logic through unit-level fakes and record a manual dev-DB verification step |
| Dropping `tournamentId` from the object key removes the season dimension | Low | Accepted; a team belongs to exactly one tournament, so `{teamId}/{playerId}/{guid}` stays unique — note it in the spec |
| Bucket-parameterizing a shared boundary regresses backups or image uploads | Low | Default argument = configured `SupaBase:BucketName`; backup/image call sites unchanged and covered by existing tests |
| Three parts together exceed the 400-line review budget | Med | Ship as chained PRs (P1 → P2 → P3) if the tasks-phase forecast is Medium/High |

## Rollback Plan

- **Part 1**: revert the storage/config commits. No schema and no persisted state changes; the boundary returns to the single configured bucket. Objects already written to `medical-records` are simply orphaned (private, harmless). Note that any record uploaded while Part 1 was live keeps a `{teamId}/...` ref that will not resolve in `public-images` — reseed dev after reverting.
- **Part 2**: revert the domain/application commits. `IsHabilitado` returns to `status == Approved`; no data was written, so previously-approved rows immediately read as habilitado again.
- **Part 3**: **the fake-ref removal is the sharp edge.** Reverting Part 3 code without reseeding leaves approved rows with a null `MedicalRecordFileUrl`; combined with Part 2 still deployed they read as not habilitado, and combined with a Part 2 revert they read as habilitado with no file (the original bug). Correct rollback of Part 3 = revert the commits **and** reseed the dev DB with `--Seed:Reset`.
- **Manual step**: the private `medical-records` bucket is already created and is not deleted by any rollback; leaving it in place is safe.

## Dependencies

- Private `medical-records` Supabase bucket — **already created by the owner** (manual, per environment).
- A readable `ficha-medica-club12.pdf` at `Seed:MedicalRecordPath` on the machine running the seed.
- `UploadTeamLogosAsync` as the reference pattern for degrade-on-failure seed uploads.
- Integration branch is `develop`, not `main`. Strict TDD active.

## Success Criteria

- [ ] A medical PDF uploaded through `POST` lands in the private `medical-records` bucket at `{teamId}/{playerId}/{guid}.pdf`; nothing new is written to `public-images`.
- [ ] Download still streams through the authenticated endpoint; no public or signed URL is produced anywhere.
- [ ] Team logos, blog images, and database backups still upload and download unchanged.
- [ ] Approving a record with no stored file is rejected with a Spanish `ErrorMessages.MedicalRecord.*` message.
- [ ] `habilitado` is false for `Approved`-with-no-file at all five read sites, including the match-sheet gate (`PlayerNotEligible`).
- [ ] The seed uploads a real PDF for every should-be-habilitado registration, is a no-op on a second run, survives an upload failure without failing the seed, and skips cleanly when `Seed:MedicalRecordPath` is unset.
- [ ] `Seed:MedicalRecords=true` backfills an already-seeded DB without a reset.
- [ ] Backend and frontend suites green; `dotnet test Club12-Backend/Solution/Club12.sln` and `npm run test --prefix Club12-WebClient` pass.

## Proposal question round

Scope was decided by the user before this phase and is not re-litigated here. Two items still need a user answer or an explicit design decision:

1. **Which registrations does the seed treat as "should be habilitado"?** Once `SampleTournamentBuilder` stops writing a file ref, the only remaining marker is `MedicalRecordStatus == Approved` (still set by the builder). Confirm the seed keys off `Approved` rather than re-deriving the `p < 7` rule.
2. **`Seed:MedicalRecords` semantics** — is it (a) an additional gate that must be true for the step to run at all, or (b) a bypass that lets the step run even when the skip-if-teams-exist guard short-circuits a normal `SeedAsync`? This proposal assumes **(b)**: the step runs during a normal reset seed, and the flag additionally enables it as a standalone backfill against an already-seeded DB.
3. **Frontend "Aprobar" disable** is marked optional. Confirm whether it ships in this change or is deferred.
